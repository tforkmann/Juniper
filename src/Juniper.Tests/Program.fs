open Expecto
open Juniper
open JuniperTests
[<EntryPoint>]
let main argv =
    testReport.Result
    printfn "finished Report"
    0