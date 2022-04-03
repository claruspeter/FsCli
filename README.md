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

The parser returns either a populated variable of a DU value, or an helpful error message for your users.

```fsharp
open FsCli

[<EntryPoint>]
let Main argv =
  match Commandline.parse<Verbs> argv with 
  | Error msg-> 
      printfn "%s" msg
  | Ok cmd -> 
      match cmd with 
      | Run runcmd -> DoSomething* runcmd
      | List listcmd -> ListSomething* listcmd
```
\* _DoSomething_ and _ListSomething_ are your functions for acting on the user's commands

A help message for the above run command would look something like:
```
USAGE: tkt run --name --data [--help]
A description for the Run verb

OPTIONS:

    --name            The name of the thing (String)
    --data            The value of the thing (Int32)
    --help            display this list of options

Run the thing
```

## Some extra options 

There are a few extras and pointers that have helped me:

### Optional Arguments

Use an option type to make arguments optional.  If you do this then the help with show the argument in square brackets
```fsharp
type ListCmd = {
  user: string option 
}

// USAGE: tkt list [--user] [--help]
```

### Argument options' types

Option values can be any _simple_ type (e.g. string, int, double) and the parsing with accept anything that works for the types `...Parse(x)` method.
```fsharp
type ListCmd = {
  user: string option 
  max: int option
}

// USAGE: myprog list [--user] [--max] [--help]
// ...
//    --name    (String)
//    --max     (Int)
```

### Commands with a subject

Sometime the commands I make need a subject. For instance:
```bash
myprog create admin --name Peter --title programmer
```

I can nominate a command subject with the `Key` attribute:
```fsharp
open System.ComponentModel
type CreateCmd ={
  [<DataAnnotations.Key>]
  usertype: string
  name: string
  title: string option
}

// USAGE: myprog create usertype --name [--title] [--help]
```
