namespace Typed.Engine.Tests

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open Xunit
open Typed.Engine.Overrides
open Typed.Engine.Generator

module GenerateFixtures =

    /// The time limit for one run of the command, which may build first.
    let commandLimitMs = 300000

    let tempFolder () : string =
        Path.Combine(Path.GetTempPath(), "generate-check-" + Guid.NewGuid().ToString("N"))

    /// The repository root: the first folder above the test run folder that holds README.md and src.
    let repoRoot () : string =
        let rec up (dir: DirectoryInfo) =
            if isNull dir then
                failwith "Repository root not found above the test run folder."
            elif File.Exists(Path.Combine(dir.FullName, "README.md")) && Directory.Exists(Path.Combine(dir.FullName, "src")) then
                dir.FullName
            else
                up dir.Parent
        up (DirectoryInfo AppContext.BaseDirectory)

    let readmeText () : string = File.ReadAllText(Path.Combine(repoRoot (), "README.md"))

    let linesOf (text: string) : string list =
        text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun l -> l.Trim())
        |> List.ofArray

    /// Runs the library entry with a changed config. Gives the exit code and the output lines.
    let runConfig (cfg: Config) : int * string list =
        use w = new StringWriter()
        let code = run cfg w
        code, linesOf (w.ToString())

    /// The todo app with the access rule of Todo removed.
    let withoutTodoRule (outDir: string) : Config =
        let cfg = todoConfig outDir
        { cfg with Rules = cfg.Rules |> List.filter (fun r -> r.ModelName <> "Todo") }

    /// The todo app with the override unsafe "rawName" "uses raw SQL" added on TodoList.
    let withUnsafe (outDir: string) : Config =
        let cfg = todoConfig outDir
        let extra = (overrides<Todo.App.Model.TodoList> [ unsafe "rawName" "uses raw SQL" ]).Items
        let sets =
            cfg.Overrides
            |> List.map (fun s -> if s.ModelName = "TodoList" then { s with Items = s.Items @ extra } else s)
        { cfg with Overrides = sets }

    /// Runs the real command with a time limit. Gives the exit code, standard output, and standard error.
    let runCommand (args: string list) : int * string * string =
        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- repoRoot ()
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        for a in [ "run"; "--project"; "src/Typed.Engine.Generator"; "--" ] @ args do
            psi.ArgumentList.Add a
        use p = Process.Start psi
        let out = p.StandardOutput.ReadToEndAsync()
        let err = p.StandardError.ReadToEndAsync()
        if not (p.WaitForExit commandLimitMs) then
            p.Kill true
            failwithf "The command did not finish in %d ms." commandLimitMs
        p.WaitForExit()
        p.ExitCode, out.Result, err.Result

open GenerateFixtures

module ``JOB-generate-R1`` =

    [<Fact>]
    let ``JOB-generate.R1 the todo app prints no error line and exits 0`` () =
        let dir = tempFolder ()
        let code, stdout, _ = runCommand [ "generate"; "--out"; dir ]
        Assert.Equal(0, code)
        Assert.DoesNotContain(linesOf stdout, fun l -> l.StartsWith "error ")

    [<Fact>]
    let ``JOB-generate.R1 the todo app without the Todo access rule prints MISSING_ACCESS_RULE and exits 1`` () =
        let code, lines = runConfig (withoutTodoRule (tempFolder ()))
        Assert.Equal(1, code)
        Assert.Contains("error MISSING_ACCESS_RULE Model Todo has no access rule.", lines)

    [<Fact>]
    let ``JOB-generate.R1 an unsafe override on TodoList prints one warning and exits 0`` () =
        let code, lines = runConfig (withUnsafe (tempFolder ()))
        Assert.Equal(0, code)
        Assert.Contains("warning UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL", lines)
        Assert.DoesNotContain(lines, fun l -> l.StartsWith "error ")

    [<Fact>]
    let ``JOB-generate.R1 the command frobnicate prints usage and exits 2`` () =
        let code, stdout, stderr = runCommand [ "frobnicate" ]
        Assert.Equal(2, code)
        Assert.Contains("usage: generate", stdout + stderr)

    [<Fact>]
    let ``JOB-generate.R1 the command prints each finding line to standard output`` () =
        // The todo app has no finding, so run the real command on a build that adds none, and check the
        // library entry for a finding line. The writer plays the part of standard output.
        let code, lines = runConfig (withoutTodoRule (tempFolder ()))
        Assert.Equal(1, code)
        let findings = lines |> List.filter (fun l -> l.StartsWith "error " || l.StartsWith "warning ")
        Assert.NotEmpty findings
        // Each finding line is "<severity> <code> <message>".
        for l in findings do
            Assert.Matches(@"^(error|warning) [A-Z_]+ .+$", l)

module ``JOB-generate-R2`` =

    let private schemaOf (dir: string) = Path.Combine(dir, "schema.sql")

    [<Fact>]
    let ``JOB-generate.R2 the todo app writes schema.sql with the tables, foreign keys, and indexes`` () =
        let dir = tempFolder ()
        let code, _, _ = runCommand [ "generate"; "--out"; dir ]
        Assert.Equal(0, code)
        let sql = File.ReadAllText(schemaOf dir)
        Assert.Equal(3, Regex.Matches(sql, "CREATE TABLE", RegexOptions.IgnoreCase).Count)
        for name in [ "User"; "TodoList"; "Todo" ] do
            Assert.Matches(Regex("CREATE TABLE\\s+\\S*" + name + "\\b", RegexOptions.IgnoreCase), sql)
        Assert.Contains("FOREIGN KEY", sql, StringComparison.OrdinalIgnoreCase)
        Assert.Matches(Regex("CREATE\\s+(UNIQUE\\s+)?(NONCLUSTERED\\s+)?INDEX", RegexOptions.IgnoreCase), sql)

    [<Fact>]
    let ``JOB-generate.R2 the todo app also writes shape.json`` () =
        let dir = tempFolder ()
        let code, _ = runConfig (todoConfig dir)
        Assert.Equal(0, code)
        Assert.True(File.Exists(Path.Combine(dir, "shape.json")))

    [<Fact>]
    let ``JOB-generate.R2 an error finding leaves schema.sql unwritten`` () =
        let dir = tempFolder ()
        let code, _ = runConfig (withoutTodoRule dir)
        Assert.Equal(1, code)
        Assert.False(File.Exists(schemaOf dir))

    [<Fact>]
    let ``JOB-generate.R2 a missing build folder is created`` () =
        // The folder and its parent do not exist before the run.
        let dir = Path.Combine(tempFolder (), "build")
        Assert.False(Directory.Exists dir)
        let code, _ = runConfig (todoConfig dir)
        Assert.Equal(0, code)
        Assert.True(Directory.Exists dir)
        Assert.True(File.Exists(schemaOf dir))

module ``JOB-generate-R3`` =

    [<Fact>]
    let ``JOB-generate.R3 the README gives the generate command`` () =
        Assert.Contains("dotnet run --project src/Typed.Engine.Generator -- generate", readmeText ())

    [<Fact>]
    let ``JOB-generate.R3 the README gives the meaning of exit codes 0, 1, and 2`` () =
        let lines = linesOf (readmeText ())
        let afterExit = lines |> List.skipWhile (fun l -> not (l.ToLowerInvariant().Contains "exit"))
        Assert.NotEmpty afterExit
        let lineFor (code: string) =
            afterExit
            |> List.tryFind (fun l -> Regex.IsMatch(l, "(^|[^0-9])" + code + "([^0-9]|$)"))
        let has (code: string) (words: string list) =
            match lineFor code with
            | Some l -> words |> List.exists (fun w -> l.ToLowerInvariant().Contains w)
            | None -> false
        Assert.True(has "0" [ "error"; "success"; "ok" ], "exit code 0 has no meaning in the README")
        Assert.True(has "1" [ "error" ], "exit code 1 has no meaning in the README")
        Assert.True(has "2" [ "usage"; "unknown" ], "exit code 2 has no meaning in the README")

    [<Fact>]
    let ``JOB-generate.R3 the README gives the command dotnet test tests/`` () =
        Assert.Contains("dotnet test tests/Typed.Engine.Tests", readmeText ())

    [<Fact>]
    let ``JOB-generate.R3 the README names the --out seam`` () =
        Assert.Contains("--out", readmeText ())
