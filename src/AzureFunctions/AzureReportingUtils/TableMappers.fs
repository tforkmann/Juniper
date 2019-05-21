module TableMappers

open Microsoft.WindowsAzure.Storage.Table
open Juniper.CreateTable
open Juniper.Ids
open Juniper.HeatPrognose
let mapLocation (entity : DynamicTableEntity) : Location =
    { LocationId = LocationId (entity.PartitionKey |> int)
      Name = entity.RowKey
      PostalCode = getOptionalStringProperty "PostalCode" entity }
let mapWeatherData (entity : DynamicTableEntity) : WeatherData =
    { LocationId = LocationId (entity.PartitionKey |> int)
      Time = entity.RowKey
      WindSpeed = getDoubleProperty "WindSpeed" entity
      Humidity = getDoubleProperty "Humidity" entity
      Temperature = getDoubleProperty "Temperature" entity
      Visibility = getDoubleProperty "Visibility" entity
      IsFaulty = getBoolProperty "IsFaulty" entity
       }
