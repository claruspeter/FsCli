module Tests.Help

open System
open System.ComponentModel
open Xunit
open FsUnit.Xunit
open FsCli

[<Description("Run the thing")>]
type RunCmd = {
  [<Description("The name of the thing")>] name: string
  [<Description("The value of the thing")>] data: int
}

type ListCmd = {
  user: string
}

type OptionalCmd = {
  name: string
  title: string option
  another: int option
}

type SubjectedCmd = {
  [<DataAnnotations.KeyAttribute>]
  [<Description("The subject")>]
  subject: string
  [<Description("The name")>]
  name: string
  [<Description("The value")>]
  value: int option
}

type Colour = Red=0 | Yellow=1 | Blue=2
type Size = Small | Medium | Large

type EnumCmd = {
  [<Description("The colour of the thing")>]
  colour: Colour option
  [<Description("The size of the thing")>]
  size: Size option
}

type Verbs =
  | [<Description("RUN DESC")>]  Run of RunCmd
  | [<Description("LIST DESC")>] List of ListCmd
  | [<Description("OPT DESC")>]  Optional of OptionalCmd
  | [<Description("SUB DESC")>]  Subjective of SubjectedCmd
  | [<Description("ENUM DESC")>] Enumerated of EnumCmd



type PartiallyDescribedVerbs =
  | [<Description("A description")>] Described of ListCmd
  | Undescribed of ListCmd

module helpers =
  let getHelp<'V> cmdline =
    match Commandline.parse<'V> cmdline with 
    | Error msg -> msg
    | _ -> Sdk.XunitException("No help message") |> raise

  let asLines (response:string) =
    response.Split(Environment.NewLine)

  let getCommands (response:string) =
    response
    |> asLines
    |> Array.skip 4
    |> Array.takeWhile (fun x -> x <> "OPTIONS:")
    |> Array.map (fun x -> x.Trim())

  let getOptions (response:string) =
    response
    |> asLines
    |> Array.skip 5
    |> Array.takeWhile (fun x -> x.StartsWith("  "))
    |> Array.map (fun x -> x.Trim())

  [<Literal>]
  let myprog = "testhost"

open helpers

[<Fact>]
let ``Commands are listed`` () =
  let commands = getHelp<Verbs> [| "--help" |] |> getCommands
  commands.[0] |> should startWith "run"
  commands.[1] |> should startWith "list"

[<Fact>]
let ``Commands have descriptions`` () =
  let commands = getHelp<Verbs> [| "--help" |] |> getCommands
  commands.[0] |> should endWith "RUN DESC"
  commands.[1] |> should endWith "LIST DESC"

[<Fact>]
let ``Commands may be undescribed`` () =
  let commands = getHelp<Verbs> [| "--help" |] |> getCommands
  commands.[0] |> should endWith "RUN DESC"
  commands.[1] |> should endWith "LIST DESC"

[<Fact>]
let ``Command line shows top-level exe in usage`` () =
  let lines = getHelp<Verbs> [| "--help" |] |> asLines
  lines.[0] |> should startWith $"USAGE: {myprog}"

[<Fact>]
let ``Command line shows optional help and options`` () =
  let lines = getHelp<Verbs> [| "--help" |] |> asLines
  lines.[0] |> should endWith $"[--help] command [<options>]"

module Commands =

  [<Fact>]
  let ``Shows options`` () =
    let options = getHelp<Verbs> [| "run"; "--help" |] |> getOptions
    options.[0] |> should startWith "--name"
    options.[1] |> should startWith "--data"
    options.[2] |> should startWith "--help"

  [<Fact>]
  let ``Options have descriptions with types`` () =
    let options = getHelp<Verbs> [| "run"; "--help" |] |> getOptions
    options.[0] |> should endWith "The name of the thing (String)"
    options.[1] |> should endWith "The value of the thing (Int32)"
    options.[2] |> should endWith "display this list of options"

  [<Fact>]
  let ``Command show description`` () =
    let lines = getHelp<Verbs> [| "run"; "--help" |] |> asLines
    lines.[1] |> should equal "RUN DESC"
    lines.[2] |> should equal ""

  [<Fact>]
  let ``Missing command description prints empty line`` () =
    let lines = getHelp<PartiallyDescribedVerbs> [| "undescribed"; "--help" |] |> asLines
    lines.[1] |> should equal ""
    lines.[2] |> should equal ""

  [<Fact>]
  let ``Command shows detailed description`` () =
    let lines = getHelp<Verbs> [| "run"; "--help" |] |> asLines
    lines |> Seq.last |> should equal "Run the thing"

  [<Fact>]
  let ``Missing detailed description prints empty line`` () =
    let lines = getHelp<PartiallyDescribedVerbs> [| "undescribed"; "--help" |] |> asLines
    lines |> Seq.last |> should equal ""

  [<Fact>]
  let ``Command line shows options`` () =
    let lines = getHelp<Verbs> [| "run"; "--help" |] |> asLines
    lines.[0] |> should equal $"USAGE: {myprog} run --name --data [--help]"

  [<Fact>]
  let ``Command line shows optional options in square brackets`` () =
    let lines = getHelp<Verbs> [| "optional"; "--help" |] |> asLines
    lines.[0] |> should equal $"USAGE: {myprog} optional --name [--title] [--another] [--help]"

  [<Fact>]
  let ``Command line shows command subject without dashes`` () =
    let lines = getHelp<Verbs> [| "subjective"; "--help" |] |> asLines
    lines.[0] |> should equal $"USAGE: {myprog} subjective subject --name [--value] [--help]"

  [<Fact>]
  let ``Options show subject without dashes`` () =
    let options = getHelp<Verbs> [| "subjective"; "--help" |] |> getOptions
    options.[0] |> should startWith "subject"
    options.[1] |> should startWith "--name"
    options.[2] |> should startWith "--value"

  [<Fact>]
  let ``Options description for subject lines up with other options`` () =
    let options = getHelp<Verbs> [| "subjective"; "--help" |] |> getOptions
    options.[0].IndexOf("The subject") |> should equal ( options.[1].IndexOf("The name") )

  [<Fact>]
  let ``Enum Options show possible values`` () =
    let help = getHelp<Verbs> [| "enumerated"; "--help" |] 
    let lines = help |> asLines
    lines.[5] |> should equal $"    --colour          The colour of the thing (Red|Yellow|Blue)"

  [<Fact>]
  let ``DU Options show possible values`` () =
    let help = getHelp<Verbs> [| "enumerated"; "--help" |] 
    let lines = help |> asLines
    lines.[6] |> should equal $"    --size            The size of the thing (Small|Medium|Large)"