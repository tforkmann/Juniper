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
nuget Fake.DotNet.Paket
nuget Fake.Core.UserInput
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
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators
open Fake.Tools

//-----------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let servicePath = Path.getFullName "./src/AzureUpload"  

let serverPath = Path.getFullName "./src/Server"
let clientPath = Path.getFullName "./src/Client"
let deployDir = Path.getFullName "./deploy"
let clientDeployPath = Path.combine clientPath "deploy"
let release = ReleaseNotes.load "RELEASE_NOTES.md"
let unitTestsPath = Path.getFullName "./src/Juniper.Tests/"
let templateName = "Juniper"

let buildDir  = "./build/"


// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitHome = "https://github.com/tforkmann"
// The name of the project on GitHub
let gitName = "juniper"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Juniper"

let projectUrl = sprintf "%s/%s" gitHome gitName

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Source code formatter for F#"

let copyright = "Copyright \169 2018"
let iconUrl = "https://raw.githubusercontent.com/fsprojects/Juniper/master/Juniper_logo.png"
let licenceUrl = "https://github.com/fsprojects/Juniper/blob/master/LICENSE.md"
let configuration = DotNet.BuildConfiguration.Release

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """This library Juniper contains Azure business reporting utils and uses an high level 
computation expression on top of the EPPlus excel package to create efficent excel reports."""
// List of author names (for NuGet package)
let authors = [ "Tim Forkmann"]
let owner = "Tim Forkmann"
// Tags for your project (for NuGet package)
let tags = "Azure business reporting utils"

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
    RawCommand (cmd, arguments)
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


open System.Text.RegularExpressions
module Util =

    let visitFile (visitor: string->string) (fileName: string) =
        File.ReadAllLines(fileName)
        |> Array.map (visitor)
        |> fun lines -> File.WriteAllLines(fileName, lines)

    let replaceLines (replacer: string->Match->string option) (reg: Regex) (fileName: string) =
        fileName |> visitFile (fun line ->
            let m = reg.Match(line)
            if not m.Success
            then line
            else
                match replacer line m with
                | None -> line
                | Some newLine -> newLine)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "Build" (fun _ ->
    !! "src/**/*.fsproj"
    |> Seq.iter (fun s ->
        let dir = Path.GetDirectoryName s
        DotNet.build id dir)
)

// Target.create "UnitTests" (fun _ ->
//     DotNet.test (fun p ->
//         { p with
//             Configuration = configuration
//             NoRestore = true
//             NoBuild = true
//             // TestAdapterPath = Some "."
//             // Logger = Some "nunit;LogFilePath=../../TestResults.xml"
//             // Current there is an issue with NUnit reporter, https://github.com/nunit/nunit3-vs-adapter/issues/589
//         }
//     ) "src/Juniper.Tests/Juniper.Tests.fsproj"
// )

Target.create "UnitTests" (fun _ ->
    runDotNet "run" unitTestsPath
)

Target.create "PrepareRelease" (fun _ ->
    Git.Branches.checkout "" false "master"
    Git.CommandHelper.directRunGitCommand "" "fetch origin" |> ignore
    Git.CommandHelper.directRunGitCommand "" "fetch origin --tags" |> ignore

    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bumping version to %O" release.NugetVersion)
    Git.Branches.pushBranch "" "origin" "master"

    let tagName = string release.NugetVersion
    Git.Branches.tag "" tagName
    Git.Branches.pushTag "" "origin" tagName
)

Target.create "Pack" (fun _ ->
    let nugetVersion = release.NugetVersion

    let pack project =
        let projectPath = sprintf "src/%s/%s.fsproj" project project
        let args =
            let defaultArgs = MSBuild.CliArguments.Create()
            { defaultArgs with
                      Properties = [
                          "Title", project
                          "PackageVersion", nugetVersion
                          "Authors", (String.Join(" ", authors))
                          "Owners", owner
                          "PackageRequireLicenseAcceptance", "false"
                          "Description", description
                          "Summary", summary
                          "PackageReleaseNotes", ((String.toLines release.Notes).Replace(",",""))
                          "Copyright", copyright
                          "PackageTags", tags
                          "PackageProjectUrl", projectUrl
                          "PackageIconUrl", iconUrl
                          "PackageLicenseUrl", licenceUrl
                      ] }
        
        DotNet.pack (fun p ->
            { p with
                  NoBuild = true
                  Configuration = configuration
                  OutputPath = Some "build"
                  MSBuildParams = args
              }) projectPath 

    pack "Juniper"
)

let getBuildParam = Environment.environVar
let isNullOrWhiteSpace = String.IsNullOrWhiteSpace

// Workaround for https://github.com/fsharp/FAKE/issues/2242
let pushPackage arguments =
    let nugetCmd fileName key = sprintf "nuget push %s -k %s -s nuget.org/" fileName key
    let key =
        match getBuildParam "nuget-key" with
        | s when not (isNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserPassword "NuGet Key: "
    let fileName = IO.Directory.GetFiles(buildDir, "*.nupkg", SearchOption.TopDirectoryOnly) |> Seq.head
    Trace.tracef "fileName %s" fileName
    let cmd = nugetCmd fileName key
    runDotNet cmd buildDir

Target.create "Push" (fun _ -> pushPackage [] )

// Build order
"Clean"
    ==> "Build"
    ==> "UnitTests"
    ==> "PrepareRelease"
    ==> "Pack"
    ==> "Push"

// start build
Target.runOrDefault "Build"