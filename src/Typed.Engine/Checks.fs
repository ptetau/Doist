module Typed.Engine.Checks

open System
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access

let private make (severity: Severity) (code: ErrorCode) (model: string) (field: string option) (message: string) : Finding =
    { Severity = severity
      Code = code
      Model = model
      Field = field
      Message = message }

let private error code model field message = make Severity.Error code model field message
let private warning code model field message = make Severity.Warning code model field message

/// A finding with its place in the field order of its model.
type private Placed = int * Finding

/// The name of the kind of an item that may appear once for each field.
let private singleKind (item: OverrideItem) : (string * string) option =
    match item with
    | Label(f, _) -> Some("label", f)
    | HiddenInForm f -> Some("hiddenInForm", f)
    | ReadOnly f -> Some("readOnly", f)
    | ListEdit f -> Some("listEdit", f)
    | Sensitive f -> Some("sensitive", f)
    | Column(f, _) -> Some("column", f)
    | _ -> None

let private accessChecks (models: Model list) (rules: AccessRule list) (model: Model) : Placed list =
    let own = rules |> List.filter (fun r -> r.ModelName = model.Name)
    let count =
        match own with
        | [] -> [ -1, error MISSING_ACCESS_RULE model.Name None (sprintf "Model %s has no access rule." model.Name) ]
        | [ _ ] -> []
        | _ -> [ -1, error MISSING_ACCESS_RULE model.Name None (sprintf "Model %s has more than one access rule." model.Name) ]
    let wrong (field: string option) message = -1, error WRONG_CONFIG_TYPE model.Name field message
    let ofRule (r: AccessRule) : Placed list =
        match r.Access with
        | OwnedVia(reference, parent, readOnlyWhen) ->
            let isRef =
                model.Fields
                |> List.exists (fun f -> f.Name = reference && refTarget f.Kind = Some parent)
            let refFinding =
                if isRef then
                    []
                else
                    [ wrong (Some reference) (sprintf "ownedVia %s.%s must be a reference to %s." model.Name reference parent) ]
            let roFinding =
                match readOnlyWhen with
                | Some(roModel, roField) ->
                    let isBool =
                        models
                        |> List.tryFind (fun m -> m.Name = roModel)
                        |> Option.bind (fun m -> m.Fields |> List.tryFind (fun f -> f.Name = roField))
                        |> Option.exists (fun f -> innerKind f.Kind = Boolean)
                    if isBool then
                        []
                    else
                        [ wrong None (sprintf "readOnlyWhenParent names %s.%s, which is not a bool field." roModel roField) ]
                | None -> []
            refFinding @ roFinding
        | _ -> []
    count @ (own |> List.collect ofRule)

let private referenceChecks
    (names: string list)
    (models: Model list)
    (setsOf: string -> OverrideItem list)
    (model: Model)
    : Placed list =
    model.Fields
    |> List.mapi (fun i f -> i, f)
    |> List.collect (fun (i, f) ->
        match refTarget f.Kind with
        | None -> []
        | Some target when not (List.contains target names) ->
            [ i,
              error
                  DANGLING_REFERENCE
                  model.Name
                  (Some f.Name)
                  (sprintf "Field %s.%s refers to %s, which is not a model." model.Name f.Name target) ]
        | Some target ->
            match models |> List.tryFind (fun m -> m.Name = target) with
            | Some t when (titleFieldName t.Fields (setsOf target)).IsNone ->
                [ i,
                  error
                      NO_TITLE_FIELD
                      model.Name
                      (Some f.Name)
                      (sprintf "%s has no title field, but %s.%s refers to it." target model.Name f.Name) ]
            | _ -> [])

let private itemChecks (model: Model) (items: OverrideItem list) : Placed list =
    let indexOf (name: string) =
        model.Fields |> List.tryFindIndex (fun f -> f.Name = name)
    let find (name: string) = model.Fields |> List.tryFind (fun f -> f.Name = name)
    let last = Int32.MaxValue
    let fieldChecks (item: OverrideItem) : Placed list =
        let placed (name: string) = defaultArg (indexOf name) last
        let named =
            match item with
            | ListView _ -> []
            | _ -> fieldsOf item
        let system, other = named |> List.partition (fun f -> List.contains f systemFields)
        let systemFindings =
            system
            |> List.map (fun f ->
                last,
                error SYSTEM_FIELD_TARGET model.Name (Some f) (sprintf "Override targets system field %s.%s." model.Name f))
        let missing =
            other
            |> List.filter (fun f -> (find f).IsNone)
            |> List.map (fun f ->
                last, error MISSING_FIELD model.Name (Some f) (sprintf "Override names missing field %s.%s." model.Name f))
        let typed =
            match item with
            | Rule(f, predicateType, _, _) ->
                match find f with
                | Some field when field.Core <> predicateType ->
                    [ placed f,
                      error
                          WRONG_CONFIG_TYPE
                          model.Name
                          (Some f)
                          (sprintf
                              "Rule %s.%s expects a predicate of type %s, but the predicate takes %s."
                              model.Name
                              f
                              (displayType field.Core)
                              (displayType predicateType)) ]
                | _ -> []
            | ListSort(f, _) ->
                match find f with
                | Some field when isWide field.Kind ->
                    [ placed f,
                      error
                          WRONG_CONFIG_TYPE
                          model.Name
                          (Some f)
                          (sprintf "Cannot sort %s by %s: wide text is never indexed." model.Name f) ]
                | _ -> []
            | ListView(_, filter) ->
                match Persist.viewToSql items filter with
                | Result.Error reason ->
                    [ last, error WRONG_CONFIG_TYPE model.Name None (sprintf "Unsupported list view for %s: %s" model.Name reason) ]
                | Ok _ -> []
            | Label(f, text) ->
                match find f with
                | Some field when field.Label = text ->
                    [ placed f,
                      warning
                          REDUNDANT_OVERRIDE
                          model.Name
                          (Some f)
                          (sprintf "Override %s.%s label matches the default." model.Name f) ]
                | _ -> []
            | Unsafe(name, reason) ->
                [ last,
                  warning
                      UNSAFE_ESCAPE_HATCH
                      model.Name
                      (Some name)
                      (sprintf "Unsafe escape hatch %s.%s: %s" model.Name name reason) ]
            | _ -> []
        systemFindings @ missing @ typed
    let duplicates =
        items
        |> List.choose singleKind
        |> List.countBy id
        |> List.filter (fun (_, n) -> n > 1)
        |> List.map (fun ((kind, f), _) ->
            defaultArg (indexOf f) last,
            error WRONG_CONFIG_TYPE model.Name (Some f) (sprintf "%s.%s has two %s overrides." model.Name f kind))
    (items |> List.collect fieldChecks) @ duplicates

/// Every finding, errors and warnings, in model order then field order.
let run
    (defaults: TypeDefault list)
    (types: Type list)
    (rules: AccessRule list)
    (sets: ModelOverrides list)
    : Finding list =
    let described = types |> List.map (describe defaults)
    let models = described |> List.choose (function Ok m -> Some m | Result.Error _ -> None)
    let names = types |> List.map (fun t -> t.Name)
    let setsOf (name: string) =
        sets |> List.filter (fun s -> s.ModelName = name) |> List.collect (fun s -> s.Items)
    let ofModel (model: Model) : Finding list =
        accessChecks models rules model @ referenceChecks names models setsOf model @ itemChecks model (setsOf model.Name)
        |> List.sortBy fst
        |> List.map snd
    let perType =
        described
        |> List.collect (function
            | Ok m -> ofModel m
            | Result.Error fs -> fs)
    let ghosts =
        sets
        |> List.filter (fun s -> not (List.contains s.ModelName names))
        |> List.map (fun s ->
            error MISSING_FIELD s.ModelName None (sprintf "Overrides name %s, which is not a model." s.ModelName))
    perType @ ghosts

let errors (findings: Finding list) : Finding list =
    findings |> List.filter (fun f -> f.Severity = Severity.Error)

let warnings (findings: Finding list) : Finding list =
    findings |> List.filter (fun f -> f.Severity = Severity.Warning)
