module TableStorage

open Microsoft.WindowsAzure.Storage.Table
open Juniper
open Juniper.Logging
open FSharp.Control.Tasks.ContextInsensitive
open FileWriter
open CreateTable

//MasterData
let weatherDataEntity (message:HeatPrognose.WeatherData) = 
    DynamicTableEntity(message.LocationId.GetValueAsString,message.Time)
    |> setDoubleProperty "Humidity" message.Humidity
    |> setDoubleProperty "Temperature" message.Temperature
    |> setDoubleProperty "Visibility" message.Visibility
    |> setDoubleProperty "WindSpeed" message.WindSpeed

let saveWeatherBatch (table:CloudTable) (messages:HeatPrognose.WeatherData [] ) = task {
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
            logError exn Local msg
            failwith msg
    ()
}