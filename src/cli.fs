module FsCli.Commandline
open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Reflection
open FsCli.Records
open FsCli.Du
open FsCli.Help

let private argsLookup (args: string list) = 
  args
  |> Seq.chunkBySize 2
  |> Seq.map (fun pair -> (pair.[0].Trim([|' '; '-'|]).ToLowerInvariant(), pair.[1]))
  |> dict

let private argsWithSubjects fields args = 
  let subjects = fields |> Seq.filter (fun x -> x.isSubject) |> Seq.map ( fun x -> x.name) |> Seq.toList
  subjects @ (args |> Array.toList)

let private parseOption (field:RecordMap) value = 
  let optionize q = 
    if field.tp |> isOptional then 
      q |> Some  |> box
    else
      q |> box
  try
    match (field.tp |> FinalType).Name with 
    | "String" -> value |> optionize
    | "Int32" -> Int32.Parse(value) |> optionize
    | "Double" -> Double.Parse(value) |> optionize
    | _ -> 
      let parseMethod = (field.tp |> FinalType).GetMethod("Parse", BindingFlags.Static ||| BindingFlags.Public, [|typeof<string>|] )
      parseMethod.Invoke(null, [|value|]) |> optionize
  with 
  | _ -> 
    failwithf "Type %s failed to parse %A" (field.tp |> FinalType).Name value

let private constructRecordField (lookup:IDictionary<string,string>) (field: RecordMap) =
  if lookup.ContainsKey field.name then 
    lookup.[field.name] |> parseOption field
  else 
    if field.tp |> isOptional then 
      None :> obj
    else
      failwithf "Required field %A is missing" field.name

let private constructRecord cmd (fields:RecordMap[]) (lookup:IDictionary<string,string>) = 
  let values = fields |> Array.map (constructRecordField lookup)
  FSharpValue.MakeRecord(cmd.tp |> FinalType, values) // ugh! the order of the values matter
    |> MakeUnionCase cmd.caseinfo 
    |> Ok

let private checkForMissingKeys (fields: RecordMap seq) (lookup:IDictionary<string,string>) =
  match lookup.Keys |> Seq.except (fields |> Seq.map (fun f -> f.name)) |> Seq.toList with 
  | [] -> 
    lookup
  | keysNotInRecord -> 
    keysNotInRecord
    |> Seq.map (fun x -> "--" + x)
    |> Seq.toArray
    |> fun x -> String.Join(", ", x)
    |> failwithf "Unknown parameters provided: %s"

let private processCommand<'V> (cmd: CaseMap) args : Result<'V,string> =
  if args |> Array.contains "--help" then 
    helpForCommand (cmd.tp |> FinalType) cmd.name cmd.description |> sprintf "%s" |> Error
  else
    try
      let fields = recordMap (cmd.tp |> FinalType)
      let withsubjects = argsWithSubjects fields args
      match withsubjects.Length with 
      | 1 -> failwithf "Missing command subject"
      | n when n % 2 = 1 -> failwithf "Argument count mismatch: %A" withsubjects
      | _ -> 
        let lookup = withsubjects |> argsLookup |> checkForMissingKeys fields
        constructRecord cmd fields lookup
      with 
      | _ as exc -> 
        [|
          sprintf "%s" exc.Message
          helpForCommand (cmd.tp |> FinalType) cmd.name cmd.description |> sprintf "%s" 
        |]
        |> fun x -> String.Join(Environment.NewLine, x)
        |> Error

let parse<'V> argv : Result<'V,string> =
  match argv with 
  | [||]
  | [| "--help" |] ->
    help<'V> |> sprintf "%s" |> Error
  | _ -> 
    try
      let verbStr = argv[0].ToLowerInvariant()
      match caseMap<'V> |> List.tryFind (fun x -> x.name = verbStr ) with 
      | None -> 
        failwithf "The command %A was not found" verbStr
      | Some x -> 
        argv 
        |> Array.skip 1
        |> processCommand x
    with 
    | _ as exc -> 
        sprintf "%s%s%s" exc.Message Environment.NewLine help<'V> |> Error