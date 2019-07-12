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
  testList "Basic tests" [
    testTask "Report should be executed" {
       do! testReport     
    }
    ]
