module FsCli.Du
open System
open System.ComponentModel
open Microsoft.FSharp.Reflection

type CaseMap = {
    name: string
    tp: Type
    caseinfo: UnionCaseInfo
    description: string
  }

let MakeUnionCase<'X> caseinfo caseValue =
  FSharpValue.MakeUnion(caseinfo, [|caseValue|]) :?> 'X

let caseMap<'X> : CaseMap list = 
  FSharpType.GetUnionCases(typeof<'X>)
  |> Seq.map (
      fun c -> 
        let desc = 
          c.GetCustomAttributes(typeof<DescriptionAttribute>)
          |> Array.map (fun d -> (d :?> DescriptionAttribute).Description)
          |> fun x-> String.Join("", x)
        let tp = 
          c.GetFields()
          |> Array.head
          |> fun p -> p.PropertyType

        {name=c.Name.ToLowerInvariant(); tp=tp; caseinfo=c; description=desc}
    )
  |> Seq.toList
