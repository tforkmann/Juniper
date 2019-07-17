module JuniperTests

open System
open SpecificDomain.DomainIds
open SpecificDomain.HeatPrognose
open Juniper
open Juniper.Logging
open FileWriter
open Ids
open Expecto
open Thoth.Json.Net
let reportInfo = 
    { ReportName = "Test"
      ReportTime = "Test"
      ReportIntervall = Monthly
      ReportTyp = "Test"
      ReportID = ReportId 1 }
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
    logOk Local "SheetData"
    { Locations = testLocation
      Measures = locationValues }      
let testSheetInsert = 
    logOk Local "SheetInsert"
    let excelPackage = startExcelApp ()
    let sheetInsert =
        { ExportedReport = reportInfo
          ReportData = 
            logOk Local "ReportData"
            Some (
                try 
                    DomainSheetData.Encoder sheetData |> Encode.toString 0 
                with 
                | exn -> 
                    logError exn Local "Couldn't cast to obj"
                    failwithf "Couldn't cast to obj")

          ExcelPackage = Some excelPackage }
    logOk Local (sprintf "SheetInsert %A" sheetInsert)
    sheetInsert
let testWorkSheets () = 
    try
        logOk Local "Test Worksheet"
        // logOk Local (sprintf "SheetInsert %A" sheetInsert)
        [ ReportSheet.testSheet, "TestWorksheet" ]
    with    
    | exn -> 
        logError exn Local "Couldn't add testWorksheet"
        failwithf "Couldn't add testWorksheet"        

let expectoTests (reportData:ReportData) =
    logOk Local "ExpectoTests"
    let sheetInsert = 
        match reportData.SheetInsert with
        | Some sheetInsert -> sheetInsert
        | None -> 
            logOk Local "No tst possible"
            failwith "no test possible"
    let sumMeasures = 
        match sheetInsert.ReportData with
        | Some data ->
            let domainSheetData = 
                try 
                    match Decode.fromString DomainSheetData.Decoder data  with
                    | Ok x -> x
                    | _ -> failwith "decoding failed"
                with 
                | exn -> 
                    logError exn Local "Couldn't downcast to DomainShetData"
                    failwithf "Couldn't downcast to DomainShetData"          
            domainSheetData.Measures |> Array.sumBy (fun x -> x.Value)
        | None -> 0.
         
    testList "Test if Sum of measures is not out of scope"
       [ testCase "Test Sum of measures is bigger or equal 0."
            <| fun () -> Expect.isGreaterThanOrEqual sumMeasures 0. "SumData should be bigger than or equal"]

let testReport =
    try 
        report {
            sheetInsert testSheetInsert
            // testReportData expectoTests
            // worksheetList testWorkSheets
            // logSuccess "Finished QuarterlyReportExternal"
        }
    with exn ->
        let msg = sprintf "Can't excecute Async ReportBuilding. %s" exn.Message
        logError exn Local msg
        failwith msg
