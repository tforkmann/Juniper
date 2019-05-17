module PostToQueue

open Microsoft.WindowsAzure.Storage
open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.WindowsAzure.Storage.Queue
open CreateTable
open TriggerNames
open FileWriter
open Domain
type AzureConnection = 
    | AzureConnection of string
    member this.Connect() =
        match this with
        | AzureConnection connectionString -> CloudStorageAccount.Parse connectionString
let getQueue (connection:CloudStorageAccount) queueName = 
    try 
        task {
            let queueClient = 
                try 
                    connection.CreateCloudQueueClient()
                with exn ->
                    let msg =  sprintf "Could not create CloudQueueClient. Message: %s. InnerMessage: %s" exn.Message exn.InnerException.Message
                    logError exn Local msg
                    failwith msg            
            let queue = 
                try 
                    queueClient.GetQueueReference queueName
                with exn ->
                    let msg =  sprintf "Could not get Queue Reference. Message: %s. InnerMessage: %s" exn.Message exn.InnerException.Message
                    logError exn Local msg
                    failwith msg      
            let! _q = 
                try
                    queue.CreateAsync()
                with exn ->
                    let msg =  sprintf "Could not createIfNotExistisAsync Queue. Message: %s. InnerMessage: %s" exn.Message exn.InnerException.Message
                    logError exn Local msg
                    failwith msg            
            return queue
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously
    with 
    |exn ->
        let msg =  sprintf "Could not get Queue. Message: %s. InnerMessage: %s" exn.Message exn.InnerException.Message
        logError exn Local msg
        failwith msg        
let postToQueue (queue:CloudQueue) msg = task {
    let message = CloudQueueMessage(Newtonsoft.Json.JsonConvert.SerializeObject msg)
    do! queue.AddMessageAsync(message)
}            
let validationQueue = getQueue connected Validation  
let sendQueue = getQueue connected SendMail
let expandQueue = getQueue connected Expand
let expandAggregationQueue = getQueue connected ExpandAggregation
let createVirtualReadingQueueFanOut = getQueue connected CreateVirtualReadingFanOut
let reloadVirtualReadingQueueFanOut = getQueue connected ReloadVirtualReadingFanOut
let createVirtualReadingQueue = getQueue connected CreateVirtualReading
let createCalcCompensationQueueSecFanOut = getQueue connected CalcCompensationSecFanOut
let createCalcCompensationQueueFanOut = getQueue connected CalcCompensationFanOut