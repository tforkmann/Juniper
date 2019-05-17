module TableMappers

open Microsoft.WindowsAzure.Storage.Table
open Domain
open CreateTable

let mapLocation (entity : DynamicTableEntity) : Location =
    { LocationId = entity.PartitionKey
      Name = entity.RowKey
      PostalCode = getIntProperty "PostalCode" entity }
let mapWeatherData (entity : DynamicTableEntity) : WeatherData =
    { LocationId = entity.PartitionKey
      Time = entity.RowKey
      WindSpeed = getDoubleProperty "WindSpeed" entity
      Humidity = getDoubleProperty "Humidity" entity
      Temperature = getDoubleProperty "Temperature" entity
      Visibility = getDoubleProperty "Visibility" entity }
