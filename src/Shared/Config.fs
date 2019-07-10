module Config

open System.Configuration

let storageConnString = ConfigurationManager.ConnectionStrings.["StorageConnString"].ConnectionString