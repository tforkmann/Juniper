module GetTableEntry

open Microsoft.WindowsAzure.Storage.Table
open FSharp.Control.Tasks.ContextInsensitive
open TableMappers
open FileWriter
open Domain
open System
let getAllLocations (table:CloudTable) = task {
    let rec getResults token = task {
        let! result = table.ExecuteQuerySegmentedAsync(TableQuery(), token)
        let token = result.ContinuationToken
        let result = result |> Seq.toList
        if isNull token then
            return result
        else
            let! others = getResults token
            return result @ others }

    let! results = getResults null
    
    return [| for result in results -> mapLocation result |] 
}

let getWeatherData (table:CloudTable) = task {
    let rec getResults token = task {
        let! result = table.ExecuteQuerySegmentedAsync(TableQuery(), token)
        let token = result.ContinuationToken
        let result = result |> Seq.toList
        if isNull token then
            return result
        else
            let! others = getResults token
            return result @ others }

    let! results = getResults null
    
    return [| for result in results -> mapWeatherData result |] 
}

