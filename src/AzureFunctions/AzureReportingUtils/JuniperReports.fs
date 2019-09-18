module JuniperReports

open System
open Juniper
open Chia.Domain.Ids
open Chia.Domain.Time
open Chia.Domain.Logging
open FileWriter
open Expecto
open SpecificDomain.DomainIds
open SpecificDomain.HeatPrognose
open Thoth.Json.Net
let reportInfo = 
    { ReportName = "Test"
      ReportTime = "Test"
      ReportIntervall = Monthly
      ReportTyp = "Test"
      ReportId = ReportId 1
      ReportDir = SpecificDomain.reportDir }
let testLocation =
    [|  { Name = "Test1"
          LocationId = LocationId 1
          PostalCode = "" }|]
let locationValues = 
    [| 
        { Value = 0.
          Description = "Measure 1"
          UnitOfMeasure = "MWh"
          Time = DateTime.Now };
        { Value = 1.
          Description = "Measure 2"
          UnitOfMeasure = "t"
          Time = DateTime.Now.AddYears(-1) }  |]

let sheetData =
    { Locations = testLocation
      Measures = locationValues }      
let testSheetInsert = 
    let excelPackage = startExcelApp ()
    { ExportedReport = reportInfo
      SheetData = 
        logOk Local "Set SheetData"
        let sheetData = DomainSheetData.Encoder sheetData |> Encode.toString 0
        Some sheetData
      ExcelPackage = excelPackage }
let testWorkSheets = 
    logOk Local "testWorksheet"
    [ ReportSheet.testSheet, "TestWorksheet" ]

let expectoTests (reportData:ReportData) =
    logOk Local "expectoTests"
    let sheetInsert = 
        match reportData.SheetInsert with
        | Some sheetInsert -> sheetInsert
        | None -> failwith "no test possible"
    let sumMeasures = 
        match sheetInsert.SheetData with
        | Some data -> 
            let castedData = 
              match Decode.fromString DomainSheetData.Decoder data  with
              | Ok x -> x
              | _ -> failwith "decoding failed"
            castedData.Measures |> Array.sumBy (fun x -> x.Value)
        | None -> 0.
         
    testList "Test if Sum of measures is not out of scope"
       [ testCase "Test Sum of measures is bigger or equal 0."
            <| fun () -> Expect.isGreaterThanOrEqual sumMeasures 0. "SumData should be bigger than or equal"]

let testReport =
    // try 
    report {
        sheetInsert testSheetInsert
        testReportData expectoTests
        worksheetList testWorkSheets
        logSuccess "Finished QuarterlyReportExternal"
    }
    // with exn ->
    //     let msg =
    //         sprintf "Can't excecute Async ReportBuilding. %sMessage: %s.%sInnerMessage: %s" Environment.NewLine exn.Message Environment.NewLine
    //             exn.InnerException.Message
    //     logError exn msg
    //     printfn "%s" msg
    //     failwith msg
