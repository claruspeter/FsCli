module FsCli.Help
open System
open System.ComponentModel
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open FsCli.Du
open FsCli.Records

let private thingDescription (thingType:Type) =
  thingType.GetCustomAttributes true
  |> Array.filter (fun a -> a.GetType() = typeof<DescriptionAttribute>)
  |> Array.map (fun d -> (d :?> DescriptionAttribute).Description)
  |> fun x-> String.Join("", x)
  
let helpForCommand cmdType name description =
  let options = recordMap cmdType
  let line = 
    options
    |> Array.map (fun x -> 
        if x.tp |> isOptional then 
          sprintf "[%s%s]" x.cmdPrefix x.name
        else
          sprintf "%s%s" x.cmdPrefix x.name
    )
    |> fun x -> String.Join(" ", x)

  [
    sprintf "USAGE: tkt %s %s [--help]" name line
    description
    ""
    "OPTIONS:"
    ""
  ]
  @ (
    options
    |> Array.map (
        fun x -> 
          sprintf "    %s%-*s%s (%s)" 
            x.cmdPrefix 
            (if x.isSubject then 18 else 16)
            x.name 
            x.description 
            (x.tp |> FinalType).Name
    )
    |> Array.toList
  )
  @ [
    "    --help            display this list of options"
    ""
    cmdType |> thingDescription
  ]
  |> List.toArray
  |> fun x -> String.Join(Environment.NewLine, x)


let help<'V> =
  [
    sprintf "USAGE: tkt [--help] command [<options>]"
    ""
    "COMMANDS:"
    ""
  ]
  @ (
    caseMap<'V> 
    |> List.map (fun x -> sprintf "    %-16s%s" x.name x.description)
  )
  @ [
    ""
    "OPTIONS:"
    ""
    "    --help            display this list of options"
  ]
  |> List.toArray
  |> fun x -> String.Join(Environment.NewLine, x)
