module TableStorage

open Microsoft.WindowsAzure.Storage.Table
open FSharp.Control.Tasks.ContextInsensitive
open SpecificDomain.Config
open Chia.FileWriter
open Chia.CreateTable
open SpecificDomain.HeatPrognose
//MasterData
let weatherDataEntity (message:WeatherData) = 
    DynamicTableEntity(message.LocationId.GetValueAsString,message.Time)
    |> setDoubleProperty "Humidity" message.Humidity
    |> setDoubleProperty "Temperature" message.Temperature
    |> setDoubleProperty "Visibility" message.Visibility
    |> setDoubleProperty "WindSpeed" message.WindSpeed

let saveWeatherBatch (table:CloudTable) (messages:WeatherData [] ) = task {
    let entities =
        messages 
        |> Array.map weatherDataEntity
    let batchOperation = 
        try
            TableBatchOperation () 
        with
        | exn -> failwithf "Couldn't Open New Table operation. Message: %s" exn.Message
    try 
        entities
        |> Array.iter batchOperation.InsertOrReplace
    with
        | exn ->    printfn  "Couldn't Add Entity Message: %s" exn.Message   
                    failwithf  "Couldn't Add Entity Message: %s" exn.Message   
    try 
        table.ExecuteBatchAsync(batchOperation) |> ignore
    with
        | exn ->    
            let msg = sprintf  "Couldn't Add Entity Message: %s" exn.Message   
            logError exn fileWriterInfo msg
            failwith msg

    ()
}