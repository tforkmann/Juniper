module CloudTable

open CreateTable
open Domain
open Domain.Logging
open TableNames
let weather = getTable Azure Weather connected
let location = getTable Azure Location connected