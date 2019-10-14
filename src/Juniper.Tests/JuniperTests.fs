module JuniperTests

open System
open SpecificDomain.DomainIds
open SpecificDomain.HeatPrognose
open Juniper
open Chia.Domain.Ids
open Chia.Domain.Time
open Chia.FileWriter
open Expecto
open Thoth.Json.Net
open SpecificDomain.Config

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
    logOk fileWriterInfo "SheetData"
    { Locations = testLocation
      Measures = locationValues }
let testSheetInsert =
    logOk fileWriterInfo "SheetInsert"
    let excelPackage = startExcelApp fileWriterInfo ()
    let sheetInsert =
        { ExportedReport = reportInfo
          SheetData =
            logOk fileWriterInfo "ReportData"
            Some (
                try
                    DomainSheetData.Encoder sheetData |> Encode.toString 0
                with
                | exn ->
                    logError exn fileWriterInfo "Couldn't cast to obj"
                    failwithf "Couldn't cast to obj")

          ExcelPackage = excelPackage }
    sheetInsert
let testWorkSheets =
    try
        logOk fileWriterInfo "Test Worksheet"
        [ ReportSheet.testSheet, "TestWorksheet" ]
    with
    | exn ->
        logError exn fileWriterInfo "Couldn't add testWorksheet"
        failwithf "Couldn't add testWorksheet"

let expectoTests (reportData:ReportData) =
    logOk fileWriterInfo "ExpectoTests"
    let sheetInsert =
        match reportData.SheetInsert with
        | Some sheetInsert -> sheetInsert
        | None ->
            logOk fileWriterInfo "No test possible"
            failwith "no test possible"
    let sumMeasures =
        match sheetInsert.SheetData with
        | Some data ->
            let domainSheetData =
                try
                    match Decode.fromString DomainSheetData.Decoder data  with
                    | Ok x -> x
                    | Error exn -> failwithf "decoding failed %A" exn
                with
                | exn ->
                    logError exn fileWriterInfo "Couldn't downcast to DomainShetData"
                    failwithf "Couldn't downcast to DomainShetData"
            domainSheetData.Measures |> Array.sumBy (fun x -> x.Value)
        | None -> 0.

    testList "Test if Sum of measures is not out of scope"
       [ testCase "Test Sum of measures is bigger or equal 0."
            <| fun () -> Expect.isGreaterThanOrEqual sumMeasures 0. "SumData should be bigger than or equal"]
