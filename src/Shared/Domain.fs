module Domain

open OfficeOpenXml
open System
open FSharp.Control.Tasks.ContextInsensitive
open Expecto
open System.Threading.Tasks


[<AutoOpen>]
module Domain = 
    
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

        type LocationId =
            | LocationId of locationId : int
            member this.GetValue = (fun (LocationId id) -> id) this
            member this.GetValueAsString = (fun (LocationId id) -> string id) this
        type SortableRowKey =
            | SortableRowKey of string
            member this.GetValue = (fun (SortableRowKey id) -> id) this
    type XLSReport =
        { ReportName : string
          ReportTime : string
          ReportIntervall : ReportIntervall
          ReportTyp : string
          ReportID : Ids.ReportId }
    type Location =
        { Name : string
          LocationId : Ids.LocationId
          PostalCode : string option }
    type Measure =
        { Value : float
          Description : string
          UnitOfMeasure : string
          Time : DateTime }

    type SheetData =
        { Location : Location []
          Measures : Measure [] }

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
          Visibility : float
          IsFaulty : bool }

    type Measure =
        { Value : float
          Description : string
          UnitOfMeasure : string
          Time : DateTime }

module Escalation =
    type MailContent =
        { Subject : string
          Recipient : string
          RecipientEMail : string
          Text : string
          Attachments : (string * string * string) [] }
