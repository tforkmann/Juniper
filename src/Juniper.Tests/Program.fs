open Expecto
open JuniperTests
[<EntryPoint>]
let main argv =
    testReport.Result
    0
    // runTestsInAssembly defaultConfig argv