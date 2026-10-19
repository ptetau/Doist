module Typed.Engine.Model

open System
open System.Reflection
open Microsoft.FSharp.Reflection

/// Marks a semantic type whose fields are the title of a record.
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type TitleTypeAttribute() =
    inherit Attribute()

/// A reference to a record of the model 'T.
type Ref<'T> = Ref of Guid

type Kind =
    | Text of maxLength: int
    | WholeNumber
    | Decimal
    | Boolean
    | Enum of cases: string list
    | Date
    | Timestamp
    | Id
    | Reference of target: string
    | Optional of Kind

type ErrorCode =
    | NOT_FOUND
    | STALE_VERSION
    | READ_ONLY_RECORD
    | VALIDATION_FAILED
    | MISSING_DEFAULT
    | MISSING_ACCESS_RULE
    | MISSING_FIELD
    | WRONG_CONFIG_TYPE
    | SYSTEM_FIELD_TARGET
    | DANGLING_REFERENCE
    | NO_TITLE_FIELD
    | UNPLANNED_DROP
    | UNSAFE_ESCAPE_HATCH
    | REDUNDANT_OVERRIDE
    | UNSAFE_MIGRATION
    | SHAPE_UNREADABLE

type Severity =
    | Error
    | Warning

type Finding =
    { Severity: Severity
      Code: ErrorCode
      Model: string
      Field: string option
      Message: string }

type TypeDefault =
    { Clr: Type
      Kind: Kind
      Check: string -> string list
      Hidden: bool
      Unique: bool }

type Field =
    { Name: string
      Label: string
      Kind: Kind
      Prop: PropertyInfo
      Core: Type
      Check: string -> string list
      Hidden: bool
      Unique: bool
      IsTitle: bool }

type Model =
    { Name: string
      Clr: Type
      Fields: Field list }

/// A stored record as the store and the access rules see it.
type Record =
    { Model: string
      Id: Guid
      Owner: Guid option
      Version: int64
      Values: Map<string, obj> }

/// The context that hooks get.
type HookContext =
    { User: Guid
      TimeZone: TimeZoneInfo
      Now: DateTimeOffset }

let localToday (ctx: HookContext) : DateOnly =
    DateOnly.FromDateTime((TimeZoneInfo.ConvertTime(ctx.Now, ctx.TimeZone)).DateTime)

let engines: string list = [ "Persist"; "Validate"; "Display"; "API" ]

let systemFields: string list = [ "Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt" ]

/// The label of a field name: words split at capitals, only the first word capitalised.
let labelOf (name: string) : string =
    let spaced =
        name
        |> Seq.mapi (fun i c -> if i > 0 && Char.IsUpper c then " " + string (Char.ToLower c) else string (Char.ToLower c))
        |> String.concat ""
    if spaced = "" then spaced else string (Char.ToUpper spaced[0]) + spaced.Substring 1

let rec innerKind (kind: Kind) : Kind =
    match kind with
    | Optional k -> innerKind k
    | k -> k

let isOptional (kind: Kind) : bool =
    match kind with
    | Optional _ -> true
    | _ -> false

let isWide (kind: Kind) : bool =
    match innerKind kind with
    | Text n -> n > 400
    | _ -> false

let rec refTarget (kind: Kind) : string option =
    match kind with
    | Reference t -> Some t
    | Optional k -> refTarget k
    | _ -> None

/// The name of a type as a person reads it, such as `Title list`.
let rec displayType (t: Type) : string =
    if t.IsGenericType then
        let arg = displayType (t.GetGenericArguments()[0])
        match t.GetGenericTypeDefinition().Name with
        | "FSharpList`1" -> arg + " list"
        | "FSharpOption`1" -> arg + " option"
        | _ -> t.Name.Substring(0, t.Name.IndexOf '`') + "<" + arg + ">"
    else
        t.Name

let private isGeneric (def: Type) (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = def

let private isEnumUnion (t: Type) =
    FSharpType.IsUnion(t, true)
    && not (isGeneric typedefof<option<_>> t)
    && FSharpType.GetUnionCases(t, true) |> Array.forall (fun c -> c.GetFields().Length = 0)

let private builtinKind (t: Type) : Kind option =
    if t = typeof<bool> then Some Boolean
    elif t = typeof<int> || t = typeof<int64> then Some WholeNumber
    elif t = typeof<decimal> then Some Decimal
    elif t = typeof<DateOnly> then Some Date
    elif t = typeof<DateTimeOffset> then Some Timestamp
    elif t = typeof<Guid> then Some Id
    elif isGeneric typedefof<Ref<_>> t then Some(Reference((t.GetGenericArguments()[0]).Name))
    elif isEnumUnion t then Some(Enum(FSharpType.GetUnionCases(t, true) |> Array.map (fun c -> c.Name) |> List.ofArray))
    else None

let private isTitleType (t: Type) =
    t.GetCustomAttributes(typeof<TitleTypeAttribute>, false).Length > 0

let describe (defaults: TypeDefault list) (t: Type) : Result<Model, Finding list> =
    if not (FSharpType.IsRecord(t, true)) then
        Result.Error
            [ { Severity = Error
                Code = WRONG_CONFIG_TYPE
                Model = t.Name
                Field = None
                Message = sprintf "%s is not a record type." t.Name } ]
    else
        let describeField (prop: PropertyInfo) : Result<Field, Finding> =
            let isOpt = isGeneric typedefof<option<_>> prop.PropertyType
            let core = if isOpt then prop.PropertyType.GetGenericArguments()[0] else prop.PropertyType
            let found =
                match defaults |> List.tryFind (fun d -> d.Clr = core) with
                | Some d -> Some(d.Kind, d.Check, d.Hidden, d.Unique)
                | None -> builtinKind core |> Option.map (fun k -> k, (fun _ -> []), false, false)
            match found with
            | Some(kind, check, hidden, unique) ->
                Ok
                    { Name = prop.Name
                      Label = labelOf prop.Name
                      Kind = if isOpt then Optional kind else kind
                      Prop = prop
                      Core = core
                      Check = check
                      Hidden = hidden
                      Unique = unique
                      IsTitle = isTitleType core }
            | None ->
                Result.Error
                    { Severity = Error
                      Code = MISSING_DEFAULT
                      Model = t.Name
                      Field = Some prop.Name
                      Message =
                        sprintf
                            "%s.%s has type '%s', which has no default in %s."
                            t.Name
                            prop.Name
                            (displayType prop.PropertyType)
                            (String.Join(", ", engines)) }

        let results = FSharpType.GetRecordFields(t, true) |> List.ofArray |> List.map describeField
        let errors = results |> List.choose (function Result.Error f -> Some f | Ok _ -> None)
        if errors.IsEmpty then
            Ok
                { Name = t.Name
                  Clr = t
                  Fields = results |> List.choose (function Ok f -> Some f | Result.Error _ -> None) }
        else
            Result.Error errors
