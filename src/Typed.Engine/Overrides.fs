module Typed.Engine.Overrides

open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Typed.Engine.Model

type Direction =
    | Ascending
    | Descending

type OverrideItem =
    | Label of field: string * text: string
    | HiddenInForm of string
    | ReadOnly of string
    | ListColumns of string list
    | ListEdit of string
    | ListSort of string * Direction
    | ListView of name: string * filter: Expr
    | TitleFieldOf of string
    | Rule of field: string * fieldType: Type * predicate: (obj -> bool) * message: string
    | Sensitive of string
    | Column of field: string * name: string
    | Unsafe of name: string * reason: string

/// One override of the model 'T.
type Override<'T> = Override of OverrideItem

/// One column of a list, named by a field selector of the model 'T.
type ColumnRef<'T> = ColumnRef of string

type ModelOverrides =
    { ModelName: string
      Items: OverrideItem list }

/// The property name in `<@ fun t -> t.Due @>`.
let rec fieldName (selector: Expr) : string =
    match selector with
    | Lambda(_, body) -> fieldName body
    | Coerce(e, _) -> fieldName e
    | PropertyGet(Some _, prop, _) -> prop.Name
    | _ -> failwithf "Not a field selector: %A" selector

let label (sel: Expr<'T -> 'F>) (text: string) : Override<'T> = Override(Label(fieldName sel, text))
let hiddenInForm (sel: Expr<'T -> 'F>) : Override<'T> = Override(HiddenInForm(fieldName sel))
let readOnly (sel: Expr<'T -> 'F>) : Override<'T> = Override(ReadOnly(fieldName sel))
let listEdit (sel: Expr<'T -> 'F>) : Override<'T> = Override(ListEdit(fieldName sel))

let listSort (sel: Expr<'T -> 'F>) (direction: Direction) : Override<'T> =
    Override(ListSort(fieldName sel, direction))

let col (sel: Expr<'T -> 'F>) : ColumnRef<'T> = ColumnRef(fieldName sel)

let listColumns (cols: ColumnRef<'T> list) : Override<'T> =
    Override(ListColumns(cols |> List.map (fun (ColumnRef f) -> f)))

let listView (name: string) (filter: Expr<'T -> bool>) : Override<'T> = Override(ListView(name, filter))
let titleField (sel: Expr<'T -> 'F>) : Override<'T> = Override(TitleFieldOf(fieldName sel))

let rule (sel: Expr<'T -> 'F>) (predicate: 'F -> bool) (message: string) : Override<'T> =
    Override(Rule(fieldName sel, typeof<'F>, (fun o -> predicate (unbox<'F> o)), message))

let sensitive (sel: Expr<'T -> 'F>) : Override<'T> = Override(Sensitive(fieldName sel))
let column (sel: Expr<'T -> 'F>) (name: string) : Override<'T> = Override(Column(fieldName sel, name))
let unsafe (name: string) (reason: string) : Override<'T> = Override(Unsafe(name, reason))

let overrides<'T> (items: Override<'T> list) : ModelOverrides =
    { ModelName = typeof<'T>.Name
      Items = items |> List.map (fun (Override i) -> i) }

/// The names of the properties that an expression reads, in order, with no repeats.
let referencedFields (expr: Expr) : string list =
    let rec walk (e: Expr) : string list =
        match e with
        | PropertyGet(Some inst, prop, _) -> walk inst @ [ prop.Name ]
        | ExprShape.ShapeVar _ -> []
        | ExprShape.ShapeLambda(_, body) -> walk body
        | ExprShape.ShapeCombination(_, args) -> args |> List.collect walk
    walk expr |> List.distinct

let fieldsOf (item: OverrideItem) : string list =
    match item with
    | Label(f, _)
    | HiddenInForm f
    | ReadOnly f
    | ListEdit f
    | ListSort(f, _)
    | TitleFieldOf f
    | Rule(f, _, _, _)
    | Sensitive f
    | Column(f, _) -> [ f ]
    | ListColumns fs -> fs
    | ListView(_, filter) -> referencedFields filter
    | Unsafe _ -> []

/// The title field: an override wins, else the field of a title type.
let titleFieldName (fields: Field list) (items: OverrideItem list) : string option =
    match items |> List.tryPick (function TitleFieldOf f -> Some f | _ -> None) with
    | Some f -> Some f
    | None -> fields |> List.tryFind (fun f -> f.IsTitle) |> Option.map (fun f -> f.Name)
