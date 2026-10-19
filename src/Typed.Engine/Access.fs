module Typed.Engine.Access

open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Typed.Engine.Model

type Access =
    | Owned
    | OwnedVia of reference: string * parent: string * readOnlyWhen: (string * string) option
    | Self
    | SystemOnly

type AccessRule = { ModelName: string; Access: Access }

type ScopeKind =
    | OwnerFilter of Guid
    | SelfFilter of Guid
    | Engine

/// A scope. Only this module makes one.
type Scope =
    private
        { scopeKind: ScopeKind }

    member this.Kind = this.scopeKind

type Action =
    | Create
    | Change
    | Delete

let owned: Access = Owned
let self: Access = Self
let system: Access = SystemOnly

/// The property that a quotation `fun x -> x.Prop` reads.
let private propertyOf (expr: Expr) : Reflection.PropertyInfo =
    match expr with
    | Lambda(_, PropertyGet(Some _, prop, [])) -> prop
    | _ -> invalidArg "expr" "Expected a quotation of the form fun x -> x.Property"

let ownedVia (expr: Expr<'T -> Ref<'P>>) : Access =
    let prop = propertyOf expr
    OwnedVia(prop.Name, typeof<'P>.Name, None)

let readOnlyWhenParent (expr: Expr<'P -> bool>) (access: Access) : Access =
    match access with
    | OwnedVia(reference, parent, _) -> OwnedVia(reference, parent, Some(typeof<'P>.Name, (propertyOf expr).Name))
    | _ -> raise (ArgumentException "readOnlyWhenParent only applies to ownedVia")

let rule<'T> (access: Access) : AccessRule =
    { ModelName = typeof<'T>.Name
      Access = access }

let engineScope: Scope = { scopeKind = Engine }

let private notFound (model: string) (message: string) : Finding =
    { Severity = Error
      Code = NOT_FOUND
      Model = model
      Field = None
      Message = message }

let private accessOf (rules: AccessRule list) (model: string) : Access option =
    rules |> List.tryFind (fun r -> r.ModelName = model) |> Option.map (fun r -> r.Access)

let scopeFor (rules: AccessRule list) (user: Guid) (model: string) : Result<Scope, Finding> =
    match accessOf rules model with
    | Some Owned
    | Some(OwnedVia _) -> Ok { scopeKind = OwnerFilter user }
    | Some Self -> Ok { scopeKind = SelfFilter user }
    | Some SystemOnly
    | None -> Result.Error(notFound model (sprintf "%s was not found." model))

/// The reason text, such as "This list is archived".
let private reasonOf (parent: string) (field: string) : string =
    let word = parent |> Seq.fold (fun (w: string) c -> if Char.IsUpper c then string c else w + string c) ""
    sprintf "This %s is %s" (word.ToLowerInvariant()) (field.ToLowerInvariant())

let checkChange
    (rules: AccessRule list)
    (_action: Action)
    (model: string)
    (parent: Record option)
    : Result<unit, Finding> =
    match accessOf rules model with
    | Some(OwnedVia(_, parentModel, readOnlyWhen)) ->
        match parent with
        | None -> Result.Error(notFound model (sprintf "The %s of this %s was not found." parentModel model))
        | Some p ->
            match readOnlyWhen with
            | Some(roModel, field) when p.Values.TryFind field = Some(box true) ->
                Result.Error
                    { Severity = Error
                      Code = READ_ONLY_RECORD
                      Model = model
                      Field = None
                      Message = reasonOf roModel field }
            | _ -> Ok()
    | _ -> Ok()

let ownerFor
    (rules: AccessRule list)
    (user: Guid)
    (model: string)
    (parent: Record option)
    : Result<Guid, Finding> =
    match accessOf rules model with
    | Some Owned
    | Some Self -> Ok user
    | Some(OwnedVia _) ->
        match parent with
        | Some p when p.Owner = Some user -> Ok user
        | _ -> Result.Error(notFound model (sprintf "%s was not found." model))
    | Some SystemOnly
    | None -> Result.Error(notFound model (sprintf "%s was not found." model))
