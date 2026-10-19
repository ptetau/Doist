namespace Typed.Engine.Tests

open System
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Validate
open Typed.Engine.Tests.ModelsFixtures

/// Records and helpers of the validation checks. The checks use only these.
module ValidateFixtures =

    let oneLine (s: string) = if s.IndexOfAny([| '\r'; '\n' |]) >= 0 then [ "must be on one line" ] else []

    let knownZone (s: string) =
        if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then []
        else [ "must be a known time zone, such as Pacific/Auckland" ]

    /// The defaults of the todo app, with the rule of each type, as the spec writes them.
    let defaults : TypeDefault list =
        [ { Clr = typeof<Title>;      Kind = Text 200;  Check = oneLine;        Hidden = false; Unique = false }
          { Clr = typeof<Notes>;      Kind = Text 5000; Check = (fun _ -> []); Hidden = false; Unique = false }
          { Clr = typeof<TimeZoneId>; Kind = Text 64;   Check = knownZone;      Hidden = false; Unique = false }
          { Clr = typeof<ExternalId>; Kind = Text 255;  Check = (fun _ -> []); Hidden = true;  Unique = true } ]

    /// Shadows `ModelsFixtures.describeOk`. It uses the defaults above.
    let describeOk (t: Type) : Model =
        match describe defaults t with
        | Ok m -> m
        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs

    let spaces (n: int) = String(' ', n)
    let chars (n: int) = String('a', n)

    let validTodo: Todo =
        { List = Ref(Guid.Parse "33333333-3333-3333-3333-333333333333")
          Title = Title "Pay rent"
          Notes = None
          Priority = Normal
          Due = None
          Done = false
          CompletedAt = None }

    let validUser: User =
        { Subject = ExternalId "sub-1"
          DisplayName = Title "Ana"
          TimeZone = TimeZoneId "Pacific/Auckland" }

    let run (t: Type) (items: OverrideItem list) (record: obj) : FieldError list =
        validate (describeOk t) items record

    let todoErrors (t: Todo) = run typeof<Todo> [] (box t)
    let listErrors (l: TodoList) = run typeof<TodoList> [] (box l)
    let userErrors (u: User) = run typeof<User> [] (box u)

    let onField (name: string) (errs: FieldError list) =
        errs |> List.filter (fun e -> e.Field = name)

    let listNamed (name: string) : TodoList = { Name = Title name; Archived = false }

    let parseOk (f: Field) (raw: string option) : obj =
        match parseField f raw with
        | Ok v -> v
        | Result.Error e -> failwithf "Expected a value, got: %A" e

    let parseErr (f: Field) (raw: string option) : FieldError =
        match parseField f raw with
        | Ok v -> failwithf "Expected an error, got: %A" v
        | Result.Error e -> e

    let todoField (name: string) = field (describeOk typeof<Todo>) name

open ValidateFixtures

module ``CAP-validate-R1`` =

    [<Fact>]
    let ``CAP-validate.R1 a valid list gives an empty list`` () =
        Assert.Empty(listErrors { Name = Title "Groceries"; Archived = false })

    [<Fact>]
    let ``CAP-validate.R1 two broken fields give one error for each with its own field name`` () =
        let errs =
            todoErrors
                { validTodo with
                    Title = Title "   "
                    Notes = Some(Notes(chars 5001)) }
        Assert.Equal(2, errs.Length)
        let sorted = errs |> List.sortBy (fun e -> e.Field)
        Assert.Equal<FieldError list>(
            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" }
              { Field = "Title"; Message = "is required" } ],
            sorted
        )

module ``CAP-validate-R2`` =

    [<Fact>]
    let ``CAP-validate.R2 a name with spaces at both ends is valid`` () =
        Assert.Empty(listErrors (listNamed "  Groceries  "))

    [<Fact>]
    let ``CAP-validate.R2 parseField trims the title`` () =
        let v = parseOk (todoField "Title") (Some "  Groceries  ")
        Assert.Equal(box (Title "Groceries"), v)

    [<Fact>]
    let ``CAP-validate.R2 a name of spaces only is required`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Name"; Message = "is required" } ],
            listErrors (listNamed "   ")
        )

    [<Fact>]
    let ``CAP-validate.R2 a name of 200 characters is valid`` () =
        Assert.Empty(listErrors (listNamed (chars 200)))

    [<Fact>]
    let ``CAP-validate.R2 a name of 201 characters is too long`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Name"; Message = "must be 200 characters or fewer" } ],
            listErrors (listNamed (chars 201))
        )

    [<Fact>]
    let ``CAP-validate.R2 a name with a line break must be on one line`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Name"; Message = "must be on one line" } ],
            listErrors (listNamed "Shop\nlater")
        )

    [<Fact>]
    let ``CAP-validate.R2 a name with a tab is valid`` () =
        Assert.Empty(listErrors (listNamed "Shop\tlater"))

    [<Fact>]
    let ``CAP-validate.R2 a name of 200 characters and three spaces at each end is valid`` () =
        Assert.Empty(listErrors (listNamed (spaces 3 + chars 200 + spaces 3)))

    [<Fact>]
    let ``CAP-validate.R2 a name with script tags is valid`` () =
        Assert.Empty(listErrors (listNamed "<script>alert(1)</script>"))

module ``CAP-validate-R3`` =

    let private withNotes n = todoErrors { validTodo with Notes = n }

    [<Fact>]
    let ``CAP-validate.R3 no notes gives no error on Notes`` () =
        Assert.Empty(onField "Notes" (withNotes None))

    [<Fact>]
    let ``CAP-validate.R3 notes of 5000 characters give no error on Notes`` () =
        Assert.Empty(onField "Notes" (withNotes (Some(Notes(chars 5000)))))

    [<Fact>]
    let ``CAP-validate.R3 notes of 5001 characters give one error on Notes`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" } ],
            withNotes (Some(Notes(chars 5001)))
        )

    [<Fact>]
    let ``CAP-validate.R3 notes of 5000 characters and one space give one error with no trim`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" } ],
            withNotes (Some(Notes(chars 5000 + " ")))
        )

module ``CAP-validate-R4`` =

    let private withZone z = userErrors { validUser with TimeZone = TimeZoneId z }

    [<Fact>]
    let ``CAP-validate.R4 Pacific/Auckland gives no error on TimeZone`` () =
        Assert.Empty(onField "TimeZone" (withZone "Pacific/Auckland"))

    [<Fact>]
    let ``CAP-validate.R4 Mars/Base gives one error on TimeZone`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "TimeZone"
                Message = "must be a known time zone, such as Pacific/Auckland" } ],
            withZone "Mars/Base"
        )

    [<Fact>]
    let ``CAP-validate.R4 a zone name with the wrong case gives one error on TimeZone`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "TimeZone"
                Message = "must be a known time zone, such as Pacific/Auckland" } ],
            withZone "pacific/auckland"
        )

module ``CAP-validate-R5`` =

    [<Fact>]
    let ``CAP-validate.R5 an empty reference gives one error on List`` () =
        let errs = todoErrors { validTodo with List = Ref Guid.Empty }
        Assert.Equal<FieldError list>([ { Field = "List"; Message = "is required" } ], errs)

    [<Fact>]
    let ``CAP-validate.R5 a reference with an id gives no error on List`` () =
        let errs = todoErrors { validTodo with List = Ref(Guid.NewGuid()) }
        Assert.Empty(onField "List" errs)

module ``CAP-validate-R6`` =

    let private reserved =
        (overrides<Todo>
            [ rule <@ fun (t: Todo) -> t.Title @> (fun (Title s) -> s <> "forbidden") "Title is reserved" ])
            .Items

    [<Fact>]
    let ``CAP-validate.R6 a failing predicate adds its message on Title`` () =
        let errs = run typeof<Todo> reserved (box { validTodo with Title = Title "forbidden" })
        Assert.Equal<FieldError list>([ { Field = "Title"; Message = "Title is reserved" } ], errs)

    [<Fact>]
    let ``CAP-validate.R6 a passing predicate adds no error on Title`` () =
        let errs = run typeof<Todo> reserved (box validTodo)
        Assert.Empty(onField "Title" errs)

module ``CAP-validate-R7`` =

    [<Fact>]
    let ``CAP-validate.R7 Urgent for Priority gives one error on Priority`` () =
        let e = parseErr (todoField "Priority") (Some "Urgent")
        Assert.Equal({ Field = "Priority"; Message = "must be one of Low, Normal, High" }, e)

    [<Fact>]
    let ``CAP-validate.R7 High for Priority gives the case High`` () =
        let v = parseOk (todoField "Priority") (Some "High")
        Assert.Equal(box High, v)

module ``CAP-validate-R8`` =

    [<Fact>]
    let ``CAP-validate.R8 abc for a whole number gives an error`` () =
        let f = field (describeOk typeof<NoTitle>) "Count"
        Assert.Equal("must be a whole number", (parseErr f (Some "abc")).Message)

    [<Fact>]
    let ``CAP-validate.R8 2026-02-30 for Due gives an error`` () =
        let e = parseErr (todoField "Due") (Some "2026-02-30")
        Assert.Equal("must be a date in the form YYYY-MM-DD", e.Message)

    [<Fact>]
    let ``CAP-validate.R8 2026-10-18 for Due gives Some date`` () =
        let v = parseOk (todoField "Due") (Some "2026-10-18")
        Assert.Equal(box (Some(DateOnly(2026, 10, 18))), v)

    [<Fact>]
    let ``CAP-validate.R8 an empty value for the optional Due gives None`` () =
        // A boxed None is null.
        Assert.Null(parseOk (todoField "Due") (Some ""))

    [<Fact>]
    let ``CAP-validate.R8 an empty value for the required Title gives an error`` () =
        let e = parseErr (todoField "Title") (Some "")
        Assert.Equal("is required", e.Message)

    [<Fact>]
    let ``CAP-validate.R8 on and true for Done give true`` () =
        Assert.Equal(box true, parseOk (todoField "Done") (Some "on"))
        Assert.Equal(box true, parseOk (todoField "Done") (Some "true"))

    [<Fact>]
    let ``CAP-validate.R8 false for Done gives false`` () =
        Assert.Equal(box false, parseOk (todoField "Done") (Some "false"))

    [<Fact>]
    let ``CAP-validate.R8 a missing value for Done gives false`` () =
        Assert.Equal(box false, parseOk (todoField "Done") None)

    [<Fact>]
    let ``CAP-validate.R8 yes for Done gives an error`` () =
        let e = parseErr (todoField "Done") (Some "yes")
        Assert.Equal("must be true or false", e.Message)
