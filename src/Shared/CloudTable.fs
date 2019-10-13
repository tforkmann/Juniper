module CloudTable

open TableNames
open Chia.CreateTable
open SpecificDomain.Config
module BlobConfig =
    open Microsoft.WindowsAzure.Storage
    open Chia.CreateBlob
    let connection = CloudStorageAccount.Parse("")
    let blobClient = connection.CreateCloudBlobClient()
    let testReportContainer = getContainer (connection,"test-report")

module AzureConnection =
    open Config
    open Chia.FileWriter
    let connection = AzureConnection storageConnString
    let connected =
        try
            connection.Connect()
        with
        | exn ->
            let msg = sprintf  "Could not connect to AzurePortal %s"  exn.Message
            logError exn fileWriterInfo msg
            failwith msg
open AzureConnection
let weather fileWriterInfo = getTable Weather fileWriterInfo connected
let location fileWriterInfo = getTable Location fileWriterInfo connected
