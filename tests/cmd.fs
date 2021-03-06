module Tests.Cmd

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
  colour: Colour
  [<Description("The size of the thing")>]
  size: Size option
}

type Verbs =
  | [<Description("RUN DESC")>]  Run of RunCmd
  | [<Description("SUB DESC")>]  Subjective of SubjectedCmd
  | [<Description("ENUM DESC")>] Enumerated of EnumCmd

module helpers =
  let asLines (response:string) =
    response.Split(Environment.NewLine)

  let getCmd<'V> cmdline =
    match Commandline.parse<'V> cmdline with 
    | Error msg -> failwith msg
    | Ok x -> x

  let getFailure<'V> cmdline =
    match Commandline.parse<'V> cmdline with 
    | Error msg -> msg
    | Ok x -> failwith "didn't fail"

open helpers


[<Fact>]
let ``Empty command show help`` () =
  getFailure<Verbs> [| |] 
  |> asLines
  |> Array.head
  |> should startWith "USAGE"

[<Fact>]
let ``Parsed from command line`` () =
  match getCmd<Verbs> [| "run"; "--name"; "Fred"; "--data"; "69"|] with 
  | Subjective _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Run run -> run |> should equal { name="Fred"; data=69 }

[<Fact>]
let ``Int options must be number`` () =
  getFailure<Verbs> [| "run"; "--name"; "Fred"; "--data"; "not-a-number"|] 
  |> asLines
  |> Array.head
  |> should startWith "Type Int32 failed to parse \"not-a-number\""

[<Fact>]
let ``Int options must be a whole number`` () =
  getFailure<Verbs> [| "run"; "--name"; "Fred"; "--data"; "420.69"|] 
  |> asLines
  |> Array.head
  |> should startWith "Type Int32 failed to parse \"420.69\""

[<Fact>]
let ``Mandatory options must be supplied`` () =
  getFailure<Verbs> [| "run"; "--name"; "Fred"|] 
  |> asLines
  |> Array.head
  |> should startWith "Required field \"data\" is missing"

[<Fact>]
let ``Subjected commands don't include subject arg name`` () =
  match getCmd<Verbs> [| "subjective"; "My subject"; "--name"; "Fred"; "--value"; "69"|] with 
  | Run _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Subjective sub -> sub |> should equal { subject="My subject"; name="Fred"; value=Some 69 }

[<Fact>]
let ``Optional commands may be ommitted and are given a value of None`` () =
  match getCmd<Verbs> [| "subjective"; "My subject"; "--name"; "Fred";|] with 
  | Run _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Subjective sub -> sub |> should equal { subject="My subject"; name="Fred"; value=None }

[<Fact>]
let ``Command names are case insensitive`` () =
  match getCmd<Verbs> [| "sUBjectIve"; "My subject"; "--name"; "Fred"; "--value"; "69"|] with 
  | Run _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Subjective sub -> sub |> should equal { subject="My subject"; name="Fred"; value=Some 69 }

[<Fact>]
let ``Option names are case insensitive`` () =
  match getCmd<Verbs> [| "subjective"; "My subject"; "--nAMe"; "Fred"; "--VALUE"; "69"|] with 
  | Run _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Subjective sub -> sub |> should equal { subject="My subject"; name="Fred"; value=Some 69 }
  
[<Fact>]
let ``Options can be in any order`` () =
  match getCmd<Verbs> [| "subjective"; "My subject"; "--value"; "69"; "--name"; "Fred"|] with 
  | Run _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Subjective sub -> sub |> should equal { subject="My subject"; name="Fred"; value=Some 69 }

[<Fact>]
let ``Subject must be first`` () =
  getFailure<Verbs> [| "subjective"; "--value"; "69"; "--name"; "Fred"; "My subject"|] 
  |> asLines
  |> Array.head
  |> should startWith "Unknown parameters provided"

[<Fact>]
let ``Extra parameters cannot be added to command line`` () =
  getFailure<Verbs> [| "subjective"; "My subject"; "--value"; "69"; "--name"; "Fred"; "--something"; "else"|] 
  |> asLines
  |> Array.head
  |> should startWith "Unknown parameters provided"

[<Fact>]
let ``Last instance of option wins`` () =
  match getCmd<Verbs> [| "run"; "--name"; "Fred"; "--data"; "69"; "--data"; "420"|] with 
  | Subjective _ -> failwith "Wrong command"
  | Enumerated _ -> failwith "Wrong command"
  | Run run -> run |> should equal { name="Fred"; data=420 }

[<Fact>]
let ``Enumerated Options are parsed`` () =
  match getCmd<Verbs> [| "enumerated"; "--colour"; "Red"; "--size"; "Medium"|] with 
  | Run _ -> failwith "Wrong command"
  | Subjective _ -> failwith "Wrong command"
  | Enumerated x -> x |> should equal { colour=Colour.Red; size=Some Medium }

[<Fact>]
let ``Enumerated Options are case insensitive`` () =
  match getCmd<Verbs> [| "enumerated"; "--colour"; "yELLow"; "--size"; "smaLL"|] with 
  | Run _ -> failwith "Wrong command"
  | Subjective _ -> failwith "Wrong command"
  | Enumerated x -> x |> should equal { colour=Colour.Yellow; size=Some Small }

[<Fact>]
let ``Enumerated Options may be optional and return None`` () =
  match getCmd<Verbs> [| "enumerated"; "--colour"; "yELLow"|] with 
  | Run _ -> failwith "Wrong command"
  | Subjective _ -> failwith "Wrong command"
  | Enumerated x -> x |> should equal { colour=Colour.Yellow; size=None }

[<Fact>]
let ``Enum parameters must be spelled correctly`` () =
  getFailure<Verbs> [| "enumerated"; "--colour"; "Pourple";|]
  |> asLines
  |> Array.head
  |> should startWith "Type Colour failed to parse \"Pourple\""

[<Fact>]
let ``DU parameters must be spelled correctly`` () =
  getFailure<Verbs> [| "enumerated"; "--colour"; "red"; "--size"; "XXL"|]
  |> asLines
  |> Array.head
  |> should startWith "Type Size failed to parse \"XXL\""