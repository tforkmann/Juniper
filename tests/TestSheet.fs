module TestSheet
open Juniper
open FSharp.Control.Tasks.ContextInsensitive

///GET WORKSHEET
let testSheet (sheet : SheetInsert option) =
    task {
        match sheet with
        | Some sheetInsert ->
            let reportInformation = sheetInsert.ReportInformation
            match sheetInsert.ExcelPackage with
            | Some package ->
                // NAME WORKSHEET
                let wks = package.Workbook.Worksheets
                let reportName = sheetInsert.ReportInformation.ReportName
                let testSheet = wks.Add reportName

                let measures =
                    match sheetInsert.ReportData with
                    | Some reportData -> reportData.Measures
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
                printfn "Generated %s %s" reportName (matchReportIntervall reportInformation.ReportIntervall)
            | None -> 
                printfn "no excelPackage init"                    
                failwith "no excelPackage init"                    
        | None -> 
            printfn "no SheetData"
            failwith "no SheetData"
    }
