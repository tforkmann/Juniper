module FileWriter

open System
open System.IO
open Domain
open Domain.Logging
open Microsoft.ApplicationInsights
open FSharp.Control.Tasks.ContextInsensitive
open System.Threading.Tasks
let client = TelemetryClient ()

let logPath = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "logs"))
let logArchivPath = @".\..\..\logs\Archiv\"
let testPath = __SOURCE_DIRECTORY__ + @".\..\tests\"
let miniLogFile (dt:DateTime) = 
    let year = dt.Year
    let day = dt.Day
    let month = dt.Month
    let hour = dt.Hour
    let min = dt.Minute
    logPath
    + (sprintf "log_%i%i%i%i%i.txt" year day month hour min )



open Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
open Microsoft.ApplicationInsights.Extensibility
let startAI ()= 
    let config = TelemetryConfiguration.Active
    config.InstrumentationKey <- "bab38c2f-5ad0-465a-a2fb-a9641646ef44"
    let mutable processor:QuickPulseTelemetryProcessor = null
    config
        .TelemetryProcessorChainBuilder
        .Use(fun next ->
            processor <- QuickPulseTelemetryProcessor next
            processor :> _)
        .Build()
    let quickPulse = new QuickPulseTelemetryModule()
    quickPulse.Initialize config
    quickPulse.RegisterTelemetryProcessor processor

let activateTSL () =
    Net.ServicePointManager.SecurityProtocol <-
        Net.ServicePointManager.SecurityProtocol |||
          Net.SecurityProtocolType.Tls11 |||
          Net.SecurityProtocolType.Tls12

let writeLog (status:Result<_,exn>) (logTxt:string) =
    let date = DateTime.Now
    let file = miniLogFile date
    match status with
    | Error exn -> client.TrackException exn
    | Ok _ -> client.TrackEvent logTxt
    let logTxt =
        sprintf "%O: %s - %s"
            DateTime.Now
            (match status with
             | Ok _ -> "Ok"
             | Error _ -> "Error")
            (match status with
             | Ok _ -> logTxt
             | Error er -> er.ToString())
    printfn "Msg %s" logTxt
    let status =
        match status with
        | Error _ -> "Error"
        | Ok _ -> "Ok"    
    try 
        File.AppendAllText(file,date.Date.ToString() + ";" + status + ";" + logTxt + Environment.NewLine)
     with
    | exn -> 
        printfn "Couldn't write LogFile: %s" exn.Message
        failwithf "Couldn't write LogFile: %s" exn.Message

let moveOldLogFiles () = 
    printfn "moving old log files"
    let destinationDirectory = Path.GetFullPath(logArchivPath)
    let sourceDirectoryRoot = Path.GetFullPath(logPath)
    let searchPattern = @"*.txt";
    let getFileName sourceFile = FileInfo(sourceFile).Name
    let getLogFiles = Directory.GetFiles(sourceDirectoryRoot, searchPattern, 
                              SearchOption.TopDirectoryOnly)
    let getDestinationFileName sourceFile destinationDirectory = 
        let destinationPath = Path.Combine (destinationDirectory,getFileName sourceFile)
        printfn "DestPath %s" destinationPath
        destinationPath
    let copyLogFiles sourceFile destinationDirectory = 
        File.Copy(sourceFile, getDestinationFileName sourceFile destinationDirectory, true) 
        |> ignore 
    let deleteOldLogFiles sourceFile= 
        File.Delete sourceFile 
    getLogFiles
    |> Array.iter (fun logFile ->
        printfn "File: %s" logFile
        copyLogFiles logFile destinationDirectory
        deleteOldLogFiles logFile)       
let logOk devOption = 
    if devOption = Local then
        writeLog (Ok())
    else
        printfn "Running On Azure %s"
let logError exn devOption = 
    if devOption = Local then
        moveOldLogFiles ()
        writeLog (Error exn)
    else
        printfn "Running On Azure %s"        
let logWithTiming fnName fn =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let res = fn()
    logOk Local (sprintf "Time taken to run %s: %O" fnName sw.Elapsed)
    res
let logWithTimingTask fnName (fn:unit -> Task<'a>) = 
    task {
        printfn "Starting LogTiming %s" fnName
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let! res = fn()
        logOk Local (sprintf "Time taken to run %s: %O" fnName sw.Elapsed)
        return res
    }
let printLogFileTotalTime (stopWatch:Diagnostics.Stopwatch) name ()=
    printfn "Creating log file: %s" name 
    let duration = stopWatch.Elapsed.TotalSeconds
    stopWatch.Stop() 
    let path = logPath + (sprintf "log_%s_%s.txt" name (DateTime.Now.ToString("yyyyMMdd")))
    let log = sprintf "Total elapsed time: %fs\n\n\n" duration
    File.AppendAllText(path,log)
    printfn "Finished log file: %s" name 

let printArray name array ()=    
    let projDir = Path.Combine(Environment.CurrentDirectory, @"..\..\")
    if not(Directory.Exists(projDir + "logs")) then 
        Directory.CreateDirectory(projDir + "logs") |> ignore
    let path = logPath + (sprintf "logs_query_%s.txt" name )
    let log = sprintf "Array: %A\n" array
    File.AppendAllText(path,log)                

///Printing query duration to logfile dsadsa
let printLogFile (stopWatch:Diagnostics.Stopwatch) query querynr func name par length () =
    printfn "Creating log file: %s" name 
    let duration = stopWatch.Elapsed.TotalSeconds
    stopWatch.Restart() 
    let projDir = Path.Combine(Environment.CurrentDirectory, @"..\..\")
    if not(Directory.Exists(projDir + "logs")) then 
        Directory.CreateDirectory(projDir + "logs") |> ignore
    let path = Path.Combine(Environment.CurrentDirectory, @"..\..\") + (sprintf "logs\\log_%s_%s.txt" name (DateTime.Now.ToString("yyyyMMdd")))
    printfn "writing to logfile %s" path
    let printlog query (duration:float) func par length = 
        let log = sprintf "Query: %s ; Function: %s ; Duration: %fs ; Paramaters: %s ; Length: %i\n" query func duration par length
        File.AppendAllText(path,log)
    if querynr = 1 then
        let log = sprintf "Logfile for %s\n" name
        File.AppendAllText(path,log)
        printlog query duration func par length
    else        
        printlog query duration func par length
    printfn "Finished log file: %s" name 
