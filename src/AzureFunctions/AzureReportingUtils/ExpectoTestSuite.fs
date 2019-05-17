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
[<FunctionName("ExpectoTestSuite")>]
let Run([<TimerTrigger("0 0 0 1 * *")>] myTimer : TimerInfo, log : ILogger) =
    task {
        let (_, dateFrom) = "01.02.2019 00:00" |> DateTime.TryParse 
        let vuFrom = dateFrom |> SortableRowKey.toRowKey
        let! weatherData  = getWeatherData weather
        let testList =
            testList "Test WeatherData"
               [ for weather in weatherData -> 
                      testCase "Test WeatherData is bigger or equal 0."
                        <| fun () -> Expect.isGreaterThanOrEqual weather.Temperature 0. "Temperature should be bigger than 0"]
        log.LogInformation "Start ExpectoTest"                                           
        let result =
            testList
            |> runTests defaultConfig
        log.LogInformation ("Test Success {0}", result)                                    
    }
