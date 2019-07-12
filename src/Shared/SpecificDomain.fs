module SpecificDomain
    open Juniper
    open System
    module DomainIds =
        type LocationId =
        | LocationId of locationId : int
            member this.GetValue = (fun (LocationId id) -> id) this
            member this.GetValueAsString = (fun (LocationId id) -> string id) this
    module HeatPrognose =

        type Location =
            { Name : string
              LocationId : DomainIds.LocationId
              PostalCode : string option }

        type WeatherData =
            { LocationId : DomainIds.LocationId
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

        type DomainSheetData =
            { Location : Location []
              Measures : Measure [] }
// module SheetData =
    //     open Juniper
    //     open Domain
    //     type SheetData with
    //         member x.Print() = Variant.print x
    //     /// **Description**
    //     ///     Validation map operator
    //     let (<!>) f x = map f x

    //     /// **Description**
    //     ///     Validation apply operator
    //     let (<*>) f x = apply f x

    //     /// **Description**
    //     ///     Validation bind operator
    //     let (>>=) x f = bind f x