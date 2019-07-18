module UnitTests

open Expecto
open Juniper
open JuniperTests


let testReport = 
    report {
        sheetInsert testSheetInsert
        testReportData expectoTests
        worksheetList testWorkSheets
        logSuccess "Finished QuarterlyReportExternal"
    }

[<Tests>]
let tests =
  printfn "Starting Tests"  
  testTask "Report should be executed" {
       do! testReport     
    }
