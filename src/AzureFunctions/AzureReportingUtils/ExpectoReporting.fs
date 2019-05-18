module ExpectoReporting

open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.Azure.WebJobs
open Microsoft.Extensions.Logging
[<FunctionName("ExpectoReporting")>]
let Run([<TimerTrigger("0 0 0 1 * *")>] myTimer : TimerInfo, log : ILogger) =
    task {
        // let resultPath testName = testPath + (sprintf "TestResults_%s.xml" testName) 
        // let writeResults testName = TestResults.writeNUnitSummary (resultPath testName, "Expecto.Tests")
        // let getConfig testName = defaultConfig.appendSummaryHandler (writeResults testName)
        ()                         
    }
