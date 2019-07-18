module SpecificDomain
    open Thoth.Json.Net
    open System
    let reportDir = @".\..\..\reports"
    module DomainIds =
        type LocationId =
        | LocationId of locationId : int
            member this.GetValue = (fun (LocationId id) -> id) this
            member this.GetValueAsString = (fun (LocationId id) -> string id) this
    module HeatPrognose =
        open DomainIds
        type Location =
            { Name : string
              LocationId : DomainIds.LocationId
              PostalCode : string }
            static member Encoder(location : Location) =
                Encode.object [ "Name", Encode.string location.Name
                                "LocationId", Encode.int location.LocationId.GetValue
                                "PostalCode", Encode.string location.PostalCode]

            static member Decoder =
                Decode.object
                    (fun get ->
                    { Name = get.Required.Field "Name" Decode.string
                      LocationId = LocationId (get.Required.Field "LocationId" Decode.int)
                      PostalCode = get.Required.Field "PostalCode" Decode.string})

        type WeatherData =
            { LocationId : DomainIds.LocationId
              Time : string
              WindSpeed : float
              Humidity : float
              Temperature : float
              Visibility : float
              IsFaulty : bool }
            static member Encoder(weatherData : WeatherData) =
                Encode.object [ "LocationId", Encode.int weatherData.LocationId.GetValue
                                "Time", Encode.string weatherData.Time
                                "WindSpeed", Encode.float weatherData.WindSpeed
                                "Humidity", Encode.float weatherData.Humidity
                                "Temperature", Encode.float weatherData.Temperature
                                "Visibility", Encode.float weatherData.Visibility
                                "IsFaulty", Encode.bool weatherData.IsFaulty ]
            static member Decoder =
                Decode.object
                    (fun get ->
                    { LocationId = LocationId (get.Required.Field "LocationId" Decode.int)
                      Time = get.Required.Field "Time" Decode.string
                      WindSpeed = get.Required.Field "WindSpeed" Decode.float
                      Humidity = get.Required.Field "Humidity" Decode.float
                      Temperature  = get.Required.Field "Temperature" Decode.float
                      Visibility = get.Required.Field "Visibility" Decode.float
                      IsFaulty = get.Required.Field "IsFaulty" Decode.bool })

                               
        type Measure =
            { Value : float
              Description : string
              UnitOfMeasure : string
              Time : DateTime }
            static member Encoder(measure : Measure) =
                Encode.object [ "Value", Encode.float measure.Value
                                "Description", Encode.string measure.Description
                                "UnitOfMeasure", Encode.string measure.UnitOfMeasure
                                "Time", Encode.datetime measure.Time ]

            static member Decoder =
                Decode.object
                    (fun get ->
                    { Value = get.Required.Field "Value" Decode.float
                      Description = get.Required.Field "Description" Decode.string
                      UnitOfMeasure = get.Required.Field "UnitOfMeasure" Decode.string
                      Time = get.Required.Field "Time" Decode.datetime })


        type DomainSheetData =
            { Locations : Location []
              Measures : Measure [] }
            static member Encoder(sheetData : DomainSheetData) =
                Encode.object [ "Locations",
                                sheetData.Locations
                                |> Array.map Location.Encoder
                                |> Encode.array
                                "Measures",
                                sheetData.Measures
                                |> Array.map Measure.Encoder
                                |> Encode.array  ]

            static member Decoder =
                Decode.object
                    (fun get ->
                    { Locations = get.Required.Field "Locations" (Decode.array Location.Decoder)
                      Measures = get.Required.Field "Measures" (Decode.array Measure.Decoder) })