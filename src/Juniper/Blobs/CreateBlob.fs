module CreateBlob

open Microsoft.WindowsAzure.Storage
open BlobNames

let connection = CloudStorageAccount.Parse("")
let blobClient = connection.CreateCloudBlobClient()
let testReportContainer = blobClient.GetContainerReference(TestReport)

testReportContainer.CreateIfNotExistsAsync() |> ignore
