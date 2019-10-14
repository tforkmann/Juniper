namespace Juniper

open FSharp.Control.Tasks.ContextInsensitive
open Domain
open ExcelUtils
open OfficeOpenXml
open System.IO
open Expecto
open Chia.FileWriter

[<AutoOpen>]
module ReportPipeline =
    ///Function to start an ExcelApplication
    let startExcelApp (fileWriterInfo:FileWriterInfo) () =
        logOk fileWriterInfo "Start ExcelApp"
        let memoryStream = new MemoryStream()
        let xlspackage = new ExcelPackage(memoryStream)
        xlspackage

    let createSheetWithLogAndTrack (reportData : ReportData) =
        match reportData.FileWriterInfo with
        | Some fileWriterInfo ->
            logOk fileWriterInfo "Start CreateSheetWithLogAndTrack"
            task {
                let stopWatch = System.Diagnostics.Stopwatch.StartNew()
                Async.Sleep 5000 |> ignore
                do! reportData.WorkSheet reportData.SheetInsert
                let msgStr = sprintf "Created %s Sheet" reportData.Name
                logOk fileWriterInfo msgStr
                printLogFileTotalTime stopWatch msgStr fileWriterInfo ()
            }
        | None ->
            printfn "Can't start Juniper - please init FileWriter info first"
            failwithf "Can't start Juniper - please init FileWriter info first"

    let zeroWorkSheet _ =
        task { () }

    let zero =
        { WorkSheet = zeroWorkSheet
          Name = ""
          LogMsg = ""
          BuildMsg = ""
          ExportMsg = ""
          SheetInsert = None
          TestSuccess = false
          FileWriterInfo = None }

    let resultPath fileWriterInfo testName = testPath fileWriterInfo + (sprintf "TestResults_%s.xml" testName)
    let writeResults fileWriterInfo testName =
        TestResults.writeNUnitSummary (resultPath fileWriterInfo testName, "Expecto.Tests")
    let getConfig fileWriterInfo testName =
        defaultConfig.appendSummaryHandler (writeResults fileWriterInfo testName)

[<AutoOpen>]
module Report =

    type Juniper internal () =

        member __.Yield(_) =
            zero

        member __.Bind(m, f) =
            m |> List.collect f

        member __.Combine(a, b) =
            a || b()

        member __.Delay(f) =
            f()

        [<CustomOperation("initFileWriterInfo")>]
        member __.FileWriterInfo(reportData, fileWriterInfo) =
            logOk fileWriterInfo "InitFileWriter"
            let reportDataWithFileWriterInfo = { reportData with FileWriterInfo = Some fileWriterInfo }
            reportDataWithFileWriterInfo
        [<CustomOperation("worksheetList")>]
        member __.WorkSheet(reportData, workSheetsAndName) =
            match reportData.FileWriterInfo with
            | Some fileWriterInfo ->
                logOk fileWriterInfo "Starting workSheet insert"
                for workSheet, name in workSheetsAndName do
                    logOk fileWriterInfo (sprintf "doing report %s" name)
                    let wksData =
                        { reportData with Name = name
                                          WorkSheet = workSheet }
                    createSheetWithLogAndTrack wksData |> ignore
                reportData
            | None ->
                printfn "Can't start Juniper - please init FileWriter info first"
                failwithf "Can't start Juniper - please init FileWriter info first"


        [<CustomOperation("exportReport")>]
        member __.Run(reportData : ReportData) =
            match reportData.FileWriterInfo with
            | Some fileWriterInfo ->
                logOk fileWriterInfo "Starting ExportReport"
                exportReport reportData.SheetInsert
            | None ->
                printfn "Can't start Juniper - please init FileWriter info first"
                failwithf "Can't start Juniper - please init FileWriter info first"

        [<CustomOperation("sheetInsert")>]
        member __.SheetInsert(reportData, sheetInsert) =
            match reportData.FileWriterInfo with
            | Some _ ->
                { reportData with SheetInsert = Some sheetInsert }
            | None ->
                printfn "Can't start Juniper - please init FileWriter info first"
                failwithf "Can't start Juniper - please init FileWriter info first"

        [<CustomOperation("testReportData")>]
        member __.TestReportData(reportData, (expectoTest : ReportData -> Test)) =
            match reportData.FileWriterInfo with
            | Some fileWriterInfo ->
                logOk fileWriterInfo "TestReportData"
                let test =
                    { Test = expectoTest reportData
                      Name = "Juniper Test" }
                logOk fileWriterInfo "Starting ReportData Tests"
                let config = getConfig fileWriterInfo test.Name
                let result = test.Test |> runTests config
                { reportData with TestSuccess =
                                      match result with
                                      | 0 -> true
                                      | 1 -> false
                                      | _ -> failwith "no valid Test result" }
            | None ->
                printfn "Can't start Juniper - please init FileWriter info first"
                failwithf "Can't start Juniper - please init FileWriter info first"

        [<CustomOperation("logSuccess")>]
        member __.Log(reportData, msg) =
            match reportData.FileWriterInfo with
            | Some fileWriterInfo ->
                logOk fileWriterInfo msg
                { reportData with ReportData.LogMsg = msg }
            | None ->
                printfn "Can't start Juniper - please init FileWriter info first"
                failwithf "Can't start Juniper - please init FileWriter info first"

    let report =
        Juniper()
