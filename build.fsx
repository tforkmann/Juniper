#r "paket:
nuget FSharp.Core
nuget Fake.Core.ReleaseNotes
nuget Fake.Core.Process
nuget Fake.IO.FileSystem
nuget Fake.BuildServer.TeamFoundation
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.Core.Environment
nuget Fake.Installer.Wix
nuget Newtonsoft.Json
nuget System.ServiceProcess.ServiceController 
nuget Fake.Core.Trace
nuget Fake.IO.Zip
nuget Fake.Tools.Git
nuget Fake.DotNet.Testing.Expecto
//"

#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
#endif 
open System
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Tools
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators
open Microsoft.Azure.Management.ResourceManager.Fluent.Core
open Fake.DotNet.Testing

//-----------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let project = "OctoBus"
let authors = ["Tim Forkmann";"Alexander But"]
let configuration = "Release"

let servicePath = Path.getFullName "./src/AzureUpload"  

let serverPath = Path.getFullName "./src/Server"
let clientPath = Path.getFullName "./src/Client"
let deployDir = Path.getFullName "./deploy"
let clientDeployPath = Path.combine clientPath "deploy"
let release = ReleaseNotes.load "RELEASE_NOTES.md"
let serverTestsPath = Path.getFullName "./test/ServerTests"
let clientTestsPath = Path.getFullName "./test/UITests"
let clientTestExecutables = "test/UITests/**/bin/**/*Tests*.exe"

let buildDir = "./bin/"


// --------------------------------------------------------------------------------------
// PlatformTools
// --------------------------------------------------------------------------------------
let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg


let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"
let npmTool = platformTool "npm" "npm.cmd"

// --------------------------------------------------------------------------------------
// Standard DotNet Build Steps
// --------------------------------------------------------------------------------------
let install = lazy DotNet.install DotNet.Versions.FromGlobalJson
let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

let mutable dotnetExePath = "dotnet"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir
// --------------------------------------------------------------------------------------
// Clean Build Results
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    !!"src/**/bin"
    |> Shell.cleanDirs
    !! "src/**/obj/*.nuspec"
    |> Shell.cleanDirs

    Shell.cleanDirs [buildDir; "temp"; "docs/output"; deployDir;]
)

// --------------------------------------------------------------------------------------
// Publish AzureFunctions
// --------------------------------------------------------------------------------------

let functionsPath =  "./src/AzureFunctions"

let runFunc cmd args workingDir  =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore
let getFunctionApp projectName =
    match projectName with 
    | "AzurereportingUtils.fsproj" -> "azurereportingutils"   
    | _ ->
        "unmatched"

let azureFunctionsfilter = Environment.environVarOrDefault "FunctionApp" ""

let functionApps = 
    ["azurereportingutils" ]

Target.create "PublishAzureFunctions" (fun _ ->
    let funcTool = platformTool "npm" "func.cmd"
    let deployDir = deployDir + "/functions"
    Shell.cleanDir deployDir

    for functionApp in functionApps do
        Trace.tracefn "FunctionAppName %s" functionApp
        if azureFunctionsfilter <> "" && functionApp <> azureFunctionsfilter then () else

        let deployDir = deployDir + "/" + functionApp
        Shell.cleanDir deployDir
        
        !! (functionsPath + "/*.json")
        |> Shell.copyFiles deployDir
        Trace.tracefn "Copied JsonFiles"

        let functionsToDeploy = 
            !! (functionsPath + "/**/*.fsproj")
            |> Seq.filter (fun proj ->
                let fi = FileInfo proj
                getFunctionApp fi.Name = functionApp)
            |> Seq.toList
        
        let targetBinDir = deployDir + "/bin"
        Shell.cleanDir targetBinDir

        functionsToDeploy
        |> Seq.iter (fun proj -> 
            let fi = FileInfo proj
            let publishCmd = sprintf "publish -c Release %s" fi.Name
            Trace.tracefn "PublishCmd: %s" publishCmd
            runDotNet publishCmd fi.Directory.FullName
            let targetPath = deployDir + "/" + fi.Name.Replace(fi.Extension,"") + "/"
            Shell.cleanDir targetPath
            Trace.tracefn "Target: %s" targetPath
            
            let mutable found = false
            let allFiles = (fun _ -> true)
            let allFiles x = 
                found <- true
                allFiles x

            let publishDir = Path.Combine(fi.Directory.FullName,"bin/Release/netstandard2.0/publish")
            let binDir = Path.Combine(publishDir,"bin")
            if Directory.Exists binDir then
                Shell.copyDir targetBinDir binDir allFiles
                !! (publishDir + "/*.deps.json")
                |> Shell.copyFiles targetBinDir
            else
                Shell.copyDir targetBinDir publishDir allFiles

            let functionJson = publishDir + "/**/function.json"
            !! functionJson
            |> Seq.iter (fun fileName ->
                let fi = FileInfo fileName
                let target = Path.Combine(targetPath,"..",fi.Directory.Name) 
                Shell.cleanDir target
                fileName |> Shell.copyFile target)

            if not found then failwithf "No files found for function %s" fi.Name

            !! (fi.Directory.FullName + "/function.json")
            |> Shell.copyFiles targetPath
        )

        runFunc funcTool ("azure functionapp publish " + functionApp) deployDir
)