module ReportSheet

open ExcelUtils
open Domain
open FSharp.Control.Tasks.ContextInsensitive
open SpecificDomain.HeatPrognose
open OfficeOpenXml
///ReportHeader
let reportHeader (reportName : string) (measures : Measure []) (wks : ExcelWorksheet) =
    let timeFrom =
        if measures <> [||] then
            measures
            |> Array.map (fun x -> x.Time)
            |> Array.min
            |> getNiceDateString
        else ""

    let timeTo =
        if measures <> [||] then
            measures
            |> Array.map (fun x -> x.Time)
            |> Array.max
            |> getNiceDateString
        else ""

    wks.Cells.[1, 1].Value <- "ReportName:"
    wks.Cells.[1, 2].Value <- reportName
    wks.Cells.[3, 1].Value <- "Date from:"
    wks.Cells.[4, 1].Value <- "Dat to:"
    wks.Cells.[3, 2].Value <- timeFrom
    wks.Cells.[4, 2].Value <- timeTo
///GET WORKSHEET
let testSheet (sheet : SheetInsert option) =
    printfn "blubb"
    task {
        match sheet with
        | Some sheetInsert ->
            let exportedReport = sheetInsert.ExportedReport
            match sheetInsert.ExcelPackage with
            | Some package ->
                // NAME WORKSHEET
                let wks = package.Workbook.Worksheets
                let reportName = sheetInsert.ExportedReport.ReportName
                let testSheet = wks.Add reportName

                let measures =
                    match sheetInsert.ReportData with
                    | Some reportData -> 
                        let data = reportData :?> DomainSheetData
                        data.Measures
                    | None -> [||]
                // REPORT HEADER
                reportHeader reportName measures testSheet
                // DEFINING STARTROWS AND STARTCOLOUMS
                let startRow = 9
                if measures <> [||] then
                    measures
                    |> Array.sortBy (fun x -> x.Time)
                    |> Array.Parallel.iteri (fun i x ->
                           testSheet.Cells.[startRow + i, 1].Value <- x.Time |> getNiceDateString
                           testSheet.Cells.[startRow + i, 2].Value <- x.Value
                           testSheet.Cells.[startRow + i, 3].Value <- x.UnitOfMeasure)
                printfn "Generated %s %s" reportName (matchReportIntervall exportedReport.ReportIntervall)
            | None -> 
                printfn "no excelPackage init"                    
                failwith "no excelPackage init"                    
        | None -> 
            printfn "no SheetData"
            failwith "no SheetData"
    }
