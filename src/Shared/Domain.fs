namespace Juniper
open OfficeOpenXml
open System
open Expecto
open System.Threading.Tasks
open Chia.Domain.Time
open Chia.Domain.Ids
open Chia.FileWriter


/// Add your Domain types


[<AutoOpen>]
module Domain =
    printfn "opening Domain"

    module Ids =

        type SortableRowKey =
            | SortableRowKey of string
            member this.GetValue = (fun (SortableRowKey id) -> id) this


    type XLSReport =
        { ReportName : string
          ReportTime : string
          ReportIntervall : ReportIntervall
          ReportTyp : string
          ReportId : ReportId
          ReportDir : string }

    type SheetInsert =
        { ExportedReport : XLSReport
          ExcelPackage : ExcelPackage
          SheetData : string option }

    type ReportData =
        { WorkSheet : SheetInsert option -> Task<Unit>
          Name : string
          LogMsg : string
          ExportMsg : string
          BuildMsg : string
          TestSuccess : bool
          SheetInsert : SheetInsert option
          FileWriterInfo : FileWriterInfo option }

    type TestInfo =
        { Test : Test
          Name : string }

module SortableRowKey =
    let toRowKey (dateTime : DateTime) =
        String.Format("{0:D19}", DateTime.MaxValue.Ticks - dateTime.Ticks) |> Ids.SortableRowKey
    let toDate (Ids.SortableRowKey ticks) = DateTime(DateTime.MaxValue.Ticks - int64 ticks)



module Escalation =
    type MailContent =
        { Subject : string
          Recipient : string
          RecipientEMail : string
          Text : string
          Attachments : (string * string * string) [] }
