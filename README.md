# FsCli

A simple way of dealing with command line arguments for console apps in .NET, designed around F# DUs and Records.

## Quickstart for using FsCli

### Define your commands

1. Create a DU that lists your allowed set of commands:

```fsharp
type Verbs =
  | Run of RunCmd
  | List of ListCmd

// e.g > myprog list --user "Peter"
// or  > myprog run --name "Sophie" --data 69

```

2. Create a record type for each command specifying the options for the command.  
  You can optionally use description attributes to help generate useful help messages.


```fsharp
open System.ComponentModel

type ListCmd = {
  user: string
}

[<Description("Run the thing")>]
type RunCmd = {
  [<Description("The name of the thing")>] name: string
  [<Description("The value of the thing")>] data: int
}

```

### Parse the commandline

1. Pass the commandline args into the `parse` function

The parser returns either a populated DU value (e.g. `Verbs.Run of RunCmd`), or an helpful error message for your users.

```fsharp
open FsCli

[<EntryPoint>]
let Main argv =
  let userCommand = Commandline.parse<Verbs> argv
  match userCommand with 
  | Ok cmd -> 
      // cmd has the value Run, or List, from our Verbs DU definition
      match cmd with 
      | Run runcmd -> DoSomething* runcmd
      | List listcmd -> ListSomething* listcmd
  | Error msg-> 
      // msg is an help message that we can just print out to the user
      printfn "%s" msg
```
\* _DoSomething_ and _ListSomething_ are your functions for acting on the user's commands

A help message for the above run command would look something like:
```
USAGE: myprog run --name --data [--help]
A description for the Run verb

OPTIONS:

    --name            The name of the thing (String)
    --data            The value of the thing (Int32)
    --help            display this list of options

Run the thing
```

## Advanced  

### Optional Arguments

Use an option type to make arguments optional.  If you do this then the help message will show the argument in square brackets
```fsharp
type ListCmd = {
  user: string option 
}

// USAGE: tkt list [--user] [--help]
```

### Argument options' types

Option values can be any _simple_ type (e.g. string, int, double) and the parsing with accept anything that works for the types `...Parse(x)` method.
```fsharp
open System.ComponentModel // for the description attribute

type ListCmd = {
  [<Description("The user to list for")>]          
  user: string option

  [<Description("The maximum number of results")>]
  max: int option
}

// USAGE: myprog list [--user] [--max] [--help]
// ...
//    --name    The user to list for (String)
//    --max     The maximum number of results (Int)
```

### Commands with a subject

Sometime we need a subject to apply the command to.
For instance, creating a thing that needs to be named, like a user, or maybe an address object.

```bash
myprog create user --name Peter --value 42
myprog create address --name Home --value 69
```

I can nominate a command subject with the `Key` attribute:
```fsharp
open System.ComponentModel
type CreateCmd ={
  [<DataAnnotations.Key>]
  itemType: string
  name: string
  value: int option
}

// USAGE: myprog create itemType --name [--value] [--help]
```
