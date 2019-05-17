module ExpectoTestSuite

open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.Azure.WebJobs
open FSharp.Core
open Microsoft.Extensions.Logging
open Domain
open System
open GetTableEntry
open CloudTable
open Expecto
[<FunctionName("ValidateCompensationValues")>]
let Run([<TimerTrigger("0 0 0 1 * *")>] myTimer : TimerInfo, log : ILogger) =
    task {
        let (_, dateFrom) = "01.02.2019 00:00" |> DateTime.TryParse 
        let vuFrom = dateFrom |> SortableRowKey.toRowKey
        let! calcCompensation  = getCalcCompensation vuFrom calcCompensationTable
        let testList =
            testList "Teste FlexPrämie"
               [ for calcComp in calcCompensation -> 
                      testCase "Test Sum of measures is bigger or equal 0."
                        <| fun () -> Expect.isGreaterThanOrEqual calcComp.FlexBonus 0. "FlexPrämie sollte größer als null sein"]
        log.LogInformation "Start ExpectoTest"                                           
        let result =
            testList
            |> runTests defaultConfig
        log.LogInformation ("Test Success {0}", result)                                    
    }
