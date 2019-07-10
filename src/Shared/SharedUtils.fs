module SharedUtils
open Domain
open Domain.Ids
open Juniper
open System


let joinString (s : string []) = String.concat (";") (s)

let createPartKey (latitude, longitude) = 
    (latitude |> string) + "-" + (longitude |> string)

let getNiceDateString (sortableRowKey : SortableRowKey) =
    let date = sortableRowKey |> SortableRowKey.toDate
    date.ToString("dd.MM.yyyy HH:mm")

let matchTimeFrameToCRON (intervall:ReportIntervall) = 
    match intervall with
    | Dayly -> "0 0 0 * * *"
    | _ -> failwith "not match"

