module FsCli.Records
open System
open System.ComponentModel
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Reflection

type RecordMap = {
  name: string
  tp: Type
  description: string
  isSubject: bool
}
with
  member this.cmdPrefix = if this.isSubject then "" else "--"

let isOptional (tp:Type) = tp.Name.StartsWith("FSharpOption")

let FinalType (tp:Type) = 
    match isOptional tp with 
    | false -> tp
    | true -> tp.GenericTypeArguments.[0]

let recordMap recordType =
  let fields = Reflection.FSharpType.GetRecordFields(recordType) 
  fields
  |> Array.map (fun f -> 
      let desc = 
        f.GetCustomAttributes true
        |> Array.filter (fun a -> a.GetType() = typeof<DescriptionAttribute>)
        |> Array.map (fun d -> (d :?> DescriptionAttribute).Description)
        |> fun x-> String.Join("", x)
      let sub =
        f.GetCustomAttributes true
        |> Array.exists (fun a -> a.GetType() = typeof<DataAnnotations.KeyAttribute>)
      {name=f.Name.ToLowerInvariant(); tp=f.PropertyType; description=desc; isSubject=sub}
  )