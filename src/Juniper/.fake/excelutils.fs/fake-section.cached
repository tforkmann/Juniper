namespace Juniper
open System
open Juniper
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
                        let reportPath = Path.Combine(exportDir + "/" + sheetInsert.ExportedReport.ReportName + "_" + dateTime + ".xlsx")
                        let data = package.GetAsByteArray()
                        File.WriteAllBytes(reportPath, data)
                        printfn "Saving Excel report at %A" reportPath
                    with exn ->
                        printfn "failure with export "
                        failwithf "Can't export Rpeort. %sMessage: %s.%sInnerMessage: %s" Environment.NewLine exn.Message Environment.NewLine exn.InnerException.Message
                | None -> failwith "no package"
            | None -> ()
        }
