module SharedUtils
open Domain
open Domain.Ids
open Domain.HeatPrognose
open System


let joinString (s : string []) = String.concat (";") (s)

let createPartKey (latitude, longitude) = 
    (latitude |> string) + "-" + (longitude |> string)

let getNiceDateString (sortableRowKey : SortableRowKey) =
    let date = sortableRowKey |> SortableRowKey.toDate
    date.ToString("dd.MM.yyyy HH:mm")

let matchTimeFrameToCRON (intervall:BatchIntervall) = 
    match intervall with
    | PerMinute -> "0 * * * * *"
    | Hourly -> "0 0 * * * *"
    | Dayly -> "0 0 0 * * *"

