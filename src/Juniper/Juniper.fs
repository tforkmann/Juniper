namespace Juniper

open FSharp.Control.Tasks.ContextInsensitive
open Domain
open Domain.Logging
open ExcelUtils
open FileWriter
open OfficeOpenXml
open System.IO
open Expecto

[<AutoOpen>]
module ReportPipeline =


    ///Function to start an ExcelApplication
    let startExcelApp() =
        logOk Local "Start ExcelApp"
        let memoryStream = new MemoryStream()
        let xlspackage = new ExcelPackage(memoryStream)
        xlspackage

    let createSheetWithLogAndTrack (reportData : ReportData) =
        task {
            let stopWatch = System.Diagnostics.Stopwatch.StartNew()
            Async.Sleep 5000 |> ignore
            do! reportData.WorkSheet reportData.SheetInsert
            let msgStr = sprintf "Created %s Sheet" reportData.Name
            logOk Local msgStr
            printLogFileTotalTime stopWatch msgStr ()
        }

    let zeroWorkSheet (sheet : SheetInsert option) = 
        task {
            ()
        }
    let zero =
        printfn "Zero"
        { WorkSheet = zeroWorkSheet
          Name = ""
          LogMsg = ""
          BuildMsg = ""
          ExportMsg = ""
          SheetInsert = None
          TestSuccess = false }

    let resultPath testName = testPath + (sprintf "TestResults_%s.xml" testName)
    let writeResults testName = TestResults.writeNUnitSummary (resultPath testName, "Expecto.Tests")
    let getConfig testName = defaultConfig.appendSummaryHandler (writeResults testName)

    [<AutoOpen>]
    module Report =
        type Juniper() =
            member __.Yield(_) = zero
            member __.Bind(m, f) = 
                m |> List.collect f
            member __.Combine (a, b) = a || b ()
            member __.Delay(f) = 
                printfn "Delay"
                f()


            [<CustomOperation("sheetInsert")>]
            member __.SheetInsert(reportData, sheetInsert) = 
                printfn "doing SheetInsert reportData %A sheetInsert %A" reportData sheetInsert
                logOk Local "doing SheetInsert"
                { reportData with SheetInsert = Some sheetInsert}
            [<CustomOperation("testReportData")>]
            member __.TestReportData(reportData, (expectoTest : ReportData -> Test)) =
                let test = {
                    Test = expectoTest reportData
                    Name = "Juniper Test"
                }
                logOk Local "Starting ReportData Tests"
                let config = getConfig test.Name
                let result = test.Test |> runTests config
                { reportData with TestSuccess =
                                      match result with
                                      | 0 -> true
                                      | 1 -> false
                                      | _ -> failwith "no valid Test result" }
            [<CustomOperation("worksheetList")>]
            member __.WorkSheet(reportData, workSheetsAndName) =
                logOk Local "Starting workSheet insert"
                for workSheet, name in workSheetsAndName do
                    logOk Local (sprintf "doing report %s" name)
                    let wksData =
                        { reportData with Name = name
                                          WorkSheet = workSheet }
                    createSheetWithLogAndTrack wksData |> ignore
                reportData

            [<CustomOperation("exportReport")>]
            member __.Run(reportData : ReportData) = 
                logOk Local "Starting ExportReport"
                exportReport reportData.SheetInsert

            [<CustomOperation("logSuccess")>]
            member __.Log(reportData, msg) =
                logOk Local msg
                { reportData with ReportData.LogMsg = msg }

    let report = Juniper()
