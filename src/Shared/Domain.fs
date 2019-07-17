module Domain

open OfficeOpenXml
open System
open Thoth.Json
open Expecto
open System.Threading.Tasks

/// Add your Domain types


[<AutoOpen>]
module Domain = 
    printfn "opening Domain"
    type ReportIntervall =
        | Dayly
        | Weekly
        | Monthly
        | Quarterly
        | Halfyearly
        | Yearly
    
    module Ids =
        type ReportId =
            | ReportId of reportId : int
            member this.GetValue = (fun (ReportId id) -> id) this
            member this.GetValueAsString = (fun (ReportId id) -> string id) this


        type SortableRowKey =
            | SortableRowKey of string
            member this.GetValue = (fun (SortableRowKey id) -> id) this


    type XLSReport =
        { ReportName : string
          ReportTime : string
          ReportIntervall : ReportIntervall
          ReportTyp : string
          ReportID : Ids.ReportId }

    type SheetData = string
         
    type SheetInsert =
        { ExportedReport : XLSReport
          ExcelPackage : ExcelPackage option
          ReportData : SheetData option }

    type ReportData =
        { WorkSheet : SheetInsert option -> Task<Unit>
          Name : string
          LogMsg : string
          ExportMsg : string
          BuildMsg : string
          TestSuccess : bool
          SheetInsert : SheetInsert option }

    type TestInfo =
        { Test : Test
          Name : string }

module Logging = 
  type DevOption =
    | Local
    | Azure

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
