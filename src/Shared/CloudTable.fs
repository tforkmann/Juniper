module CloudTable

open TableNames
open Chia.Domain.Logging
open Chia.CreateTable

let weather = getTable Azure Weather connected
let location = getTable Azure Location connected