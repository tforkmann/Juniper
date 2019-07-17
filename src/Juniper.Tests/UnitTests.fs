module UnitTests

open Expecto
open Juniper
open JuniperTests


let testReport = 
    report {
        printfn "Starting Tests"  
        sheetInsert testSheetInsert
        testReportData expectoTests
        // worksheetList testWorkSheets
        logSuccess "Finished QuarterlyReportExternal"
    }

[<Tests>]
let tests =
  printfn "Starting Tests"  
  testList "Basic tests" [
    testTask "Report should be executed" {
       do! testReport     
    }
    ]
