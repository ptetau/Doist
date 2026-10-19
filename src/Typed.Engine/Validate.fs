module Typed.Engine.Validate

open System
open System.Globalization
open Microsoft.FSharp.Reflection
open Typed.Engine.Model
open Typed.Engine.Overrides

type FieldError = { Field: string; Message: string }

let private required = "is required"

/// The value inside an `option`, or None for the empty option.
let private unwrapOption (value: obj) : obj option =
    if isNull value then None else Some(value.GetType().GetProperty("Value").GetValue value)

/// The text inside a semantic type such as `Title of string`.
let private textOf (value: obj) : string =
    match value with
    | :? string as s -> s
    | _ -> string (FSharpValue.GetUnionFields(value, value.GetType(), true) |> snd |> Array.head)

/// The id inside a `Ref`.
let private idOf (value: obj) : Guid =
    FSharpValue.GetUnionFields(value, value.GetType(), true) |> snd |> Array.head :?> Guid

/// Wide text keeps its spaces. Other text is trimmed.
let private textRules (field: Field) (maxLength: int) (raw: string) : string list =
    let text = if isWide field.Kind then raw else raw.Trim()
    if text = "" then [ required ]
    else
        [ if text.Length > maxLength then sprintf "must be %d characters or fewer" maxLength
          yield! field.Check text ]

let private kindErrors (field: Field) (value: obj) : string list =
    match innerKind field.Kind with
    | Text n -> textRules field n (textOf value)
    | Reference _ when idOf value = Guid.Empty -> [ required ]
    | _ -> []

let private fieldErrors (items: OverrideItem list) (record: obj) (field: Field) : FieldError list =
    let raw = field.Prop.GetValue record
    let core = if isOptional field.Kind then unwrapOption raw else Some raw
    let messages =
        match core with
        | None -> []
        | Some value ->
            kindErrors field value
            @ [ for item in items do
                    match item with
                    | Rule(f, _, predicate, message) when f = field.Name && not (predicate raw) -> message
                    | _ -> () ]
    messages |> List.map (fun m -> { Field = field.Name; Message = m })

/// Check a typed record. An empty list is a pass.
let validate (model: Model) (items: OverrideItem list) (record: obj) : FieldError list =
    model.Fields |> List.collect (fieldErrors items record)

let private parseCore (field: Field) (kind: Kind) (raw: string) : Result<obj, string> =
    let invariant = CultureInfo.InvariantCulture
    match kind with
    | Text _ ->
        let text = if isWide field.Kind then raw else raw.Trim()
        if text = "" then Result.Error required
        elif field.Core = typeof<string> then Ok(box text)
        else Ok(FSharpValue.MakeUnion(FSharpType.GetUnionCases(field.Core, true)[0], [| box text |], true))
    | WholeNumber ->
        if field.Core = typeof<int64> then
            match Int64.TryParse(raw.Trim(), NumberStyles.Integer, invariant) with
            | true, n -> Ok(box n)
            | _ -> Result.Error "must be a whole number"
        else
            match Int32.TryParse(raw.Trim(), NumberStyles.Integer, invariant) with
            | true, n -> Ok(box n)
            | _ -> Result.Error "must be a whole number"
    | Date ->
        match DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", invariant, DateTimeStyles.None) with
        | true, d -> Ok(box d)
        | _ -> Result.Error "must be a date in the form YYYY-MM-DD"
    | Boolean ->
        match raw.Trim() with
        | "on" | "true" -> Ok(box true)
        | "false" -> Ok(box false)
        | _ -> Result.Error "must be true or false"
    | Enum cases ->
        match FSharpType.GetUnionCases(field.Core, true) |> Array.tryFind (fun c -> c.Name = raw.Trim()) with
        | Some c -> Ok(FSharpValue.MakeUnion(c, [||], true))
        | None -> Result.Error("must be one of " + String.Join(", ", cases))
    | _ -> Result.Error "cannot be read from text"

/// Parse one raw value by kind. An optional field gives a boxed `option`. A missing boolean is false.
let parseField (field: Field) (raw: string option) : Result<obj, FieldError> =
    let fail message = Result.Error { Field = field.Name; Message = message }
    let kind = innerKind field.Kind
    let text = raw |> Option.defaultValue ""
    if kind = Boolean && raw.IsNone then
        Ok(box false)
    elif isOptional field.Kind && (not (isWide field.Kind) && text.Trim() = "" || text = "") then
        Ok null
    else
        match parseCore field kind text with
        | Result.Error message -> fail message
        | Ok value when isOptional field.Kind ->
            let some = FSharpType.GetUnionCases(field.Prop.PropertyType) |> Array.find (fun c -> c.Name = "Some")
            Ok(FSharpValue.MakeUnion(some, [| value |]))
        | Ok value -> Ok value
