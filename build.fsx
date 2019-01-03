
#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif

#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget FSharp.Core
nuget Fake.Core.Target 
//"

open System

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO

let serverPath = "./src/Server" |> Path.getFullName
let clientPath = "./src/Client" |> Path.getFullName
let deployDir = "./deploy" |> Path.getFullName

let platformTool tool winTool =
  let tool = if Environment.isUnix then tool else winTool
  tool
  |> ProcessUtils.tryFindFileOnPath
  |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"

type JsPackageManager = 
  | NPM
  | YARN
  member this.Tool =
    match this with
    | NPM -> platformTool "npm" "npm.cmd"
    | YARN -> platformTool "yarn" "yarn.cmd"
   member this.ArgsInstall =
    match this with
    | NPM -> "install"
    | YARN -> "install --frozen-lockfile"

let jsPackageManager = 
  match Environment.environVarOrDefault "jsPackageManager" "" with
  | "npm" -> NPM
  | "yarn" | _ -> YARN

let mutable dotnetCli = "dotnet"

let run cmd args workingDir =
  let result = Shell.Exec (cmd, args, workingDir)
  if result <> 0 then failwithf "'%s %s' failed" cmd args

Target.create "Clean" (fun _ -> 
  Shell.cleanDirs [deployDir]
)

Target.create "InstallClient" (fun _ ->
  printfn "Node version:"
  run nodeTool "--version" __SOURCE_DIRECTORY__
  run jsPackageManager.Tool jsPackageManager.ArgsInstall  __SOURCE_DIRECTORY__
  run dotnetCli "restore" clientPath
)

let openInBrowser url = 
  async {
    do! Async.Sleep 5000
    let pinfo = Diagnostics.ProcessStartInfo()
    pinfo.UseShellExecute <- true
    pinfo.FileName <- url 
    Diagnostics.Process.Start(pinfo) |> ignore
  }

Target.create "Run" (fun _ ->
  let server = async { run dotnetCli "watch run" serverPath }
  let client = async { run dotnetCli "fable webpack-dev-server" clientPath }
  let browser = async {
    do! Async.Sleep 5000
    do! openInBrowser "http://localhost:8080"
  }

  [ server; client; browser]
  |> Async.Parallel
  |> Async.RunSynchronously
  |> ignore
)

"Clean"
  ==> "InstallClient"
  ==> "Run"

Target.runOrDefaultWithArguments "Run"