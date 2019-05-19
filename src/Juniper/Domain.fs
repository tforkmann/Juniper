namespace Juniper

open System
open System.Threading.Tasks
open OfficeOpenXml
open FSharp.Control.Tasks.ContextInsensitive
open Expecto

/// Define your domain specific domain ids
module Ids =
    type ReportId =
        | ReportId of reportId : int
        member this.GetValue = (fun (ReportId id) -> id) this
        member this.GetValueAsString = (fun (ReportId id) -> string id) this

    type LocationId =
        | LocationId of locationId : int
        member this.GetValue = (fun (LocationId id) -> id) this
        member this.GetValueAsString = (fun (LocationId id) -> string id) this

    type SortableRowKey =
        | SortableRowKey of string
        member this.GetValue = (fun (SortableRowKey id) -> id) this


/// Add your Domain types
module HeatPrognose =
    type Location =
        { LocationId : Ids.LocationId
          Name : string
          PostalCode : string option }

    type WeatherData =
        { LocationId : Ids.LocationId
          Time : string
          WindSpeed : float
          Humidity : float
          Temperature : float
          Visibility : float }

    type Measure =
        { Value : float
          Description : string
          UnitOfMeasure : string
          Time : DateTime }

[<AutoOpen>]
module ReportPipeLine =

    type ReportIntervall =
        | Dayly
        | Weekly
        | Monthly
        | Quarterly
        | Halfyearly
        | Yearly

    type XLSReport =
        { ReportName : string
          ReportTime : string
          ReportIntervall : ReportIntervall
          ReportTyp : string
          ReportID : Ids.ReportId }
    /// Combine your specific domain model as a type SheetData
    type SheetData =
        { Location : HeatPrognose.Location []
          Measures : HeatPrognose.Measure [] }

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
    let toRowKey (dateTime : DateTime) = String.Format("{0:D19}", DateTime.MaxValue.Ticks - dateTime.Ticks) |> Ids.SortableRowKey
    let toDate (Ids.SortableRowKey ticks) = DateTime(DateTime.MaxValue.Ticks - int64 ticks)

module Escalation =
    type MailContent =
        { Subject : string
          Recipient : string
          RecipientEMail : string
          Text : string
          Attachments : (string * string * string) [] }