namespace Juniper
open System
open OfficeOpenXml
open System.IO
open CreateBlob
open FSharp.Control.Tasks.ContextInsensitive

[<AutoOpen>]
module ExcelUtils =
    let getNiceDateString (time : DateTime) = time.ToString("dd.MM.yyyy HH:mm")

    ///Function to save ExcelWorrkbook to FileStream
    let saveExcelWbToBlob guid (wb : ExcelPackage) =
        task {
            let blobId = guid + ".xlsx"
            let blobBlock = testReportContainer.GetBlockBlobReference(blobId)
            blobBlock.Properties.ContentType <- "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            let data = wb.GetAsByteArray()
            use stream = new MemoryStream(data)
            do! blobBlock.UploadFromStreamAsync(stream)
        }

    ///ReportHeader
    let reportHeader (reportName : string) (measures : HeatPrognose.Measure []) (wks : ExcelWorksheet) =
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

    /// <summary>Inserts a Array2d in a given worksheet range in a sequence.</summary>
    let insertValues startRow startCol (worksheet : ExcelWorksheet) (array : obj [] []) =
        array |> Array.Parallel.iteri (fun i x -> x |> Array.Parallel.iteri (fun j y -> worksheet.Cells.[startRow + i, startCol + j].Value <- y))

    let matchReportIntervall (intervall : ReportIntervall) =
        match intervall with
        | Dayly -> "dayly"
        | Weekly -> "weekly"
        | Monthly -> "monthly"
        | Quarterly -> "quarterly"
        | Halfyearly -> "halfyearly"
        | Yearly -> "yearly"

    let exportReport (sheet : SheetInsert option) =
        task {
            match sheet with
            | Some sheetInsert ->
                match sheetInsert.ExcelPackage with
                | Some package ->                
                    try
                        let exportDir = @".\reports\"
                        let dateTime = DateTime.Now.ToString("yyyyMMdd_HHmm")
                        let reportPath = Path.Combine(exportDir + "/" + sheetInsert.ReportInformation.ReportName + "_" + dateTime + ".xlsx")
                        let data = package.GetAsByteArray()
                        File.WriteAllBytes(reportPath, data)
                        printfn "Saving Excel report at %A" reportPath
                    with exn ->
                        printfn "failure with export "
                        failwithf "Can't export Rpeort. %sMessage: %s.%sInnerMessage: %s" Environment.NewLine exn.Message Environment.NewLine exn.InnerException.Message
                | None -> failwith "no package"
            | None -> ()
        }