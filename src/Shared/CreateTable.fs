module CreateTable

open Config
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open System.Threading.Tasks
open Domain
open Domain.Logging
open FileWriter
open System

type AzureConnection = 
    | AzureConnection of string
    member this.Connect() =
        match this with
        | AzureConnection connectionString -> CloudStorageAccount.Parse connectionString

let deleteTable isLocal tableName (connection: CloudStorageAccount) =
    printfn "Try to Delete %s" tableName
    async {
        let client = connection.CreateCloudTableClient()
        let table = client.GetTableReference tableName
        logOk isLocal (sprintf "Got TableReference to Delete %A" table)
        // Azure will temporarily lock table names after deleting and can take some time before the table name is made available again.
        let deleteTableSafe() =
            try
                table.DeleteIfExistsAsync()
            with
            | exn -> 
                let msg = sprintf  "Could not delete Table %s" exn.Message
                logError exn isLocal msg
                failwith msg
        return "Deleted Table" 
    } 
    |> Async.RunSynchronously
let getTable isLocal tableName (connection: CloudStorageAccount) =
    printfn "GetTable %s" tableName
    async {
        let client = connection.CreateCloudTableClient()
        let table = 
            try 
                client.GetTableReference tableName
            with
            | exn -> 
                let msg = sprintf  "Could not get TableReference %s" exn.Message
                logError exn isLocal msg 
                failwith msg
        logOk isLocal (sprintf "Got tableReference %A" table)
        // writeLog (sprintf "Got tableReference %A" table) Ok               
        // Azure will temporarily lock table names after deleting and can take some time before the table name is made available again.
        let rec createTableSafe() = async {
            try
                let! _ = table.CreateIfNotExistsAsync() |> Async.AwaitTask
                ()
            with
            | _ ->
                do! Task.Delay 5000 |> Async.AwaitTask
                return! createTableSafe() }

        do! createTableSafe()
        return table 
    } 
    |> Async.RunSynchronously

let getTableReference tableName (connection: CloudStorageAccount) =    
    let client = connection.CreateCloudTableClient()
    client.GetTableReference tableName
let connection = AzureConnection storageConnString
let connected = 
    try 
        connection.Connect()
    with
    | exn -> 
        let msg = sprintf  "Could not connect to AzurePortal %s"  exn.Message
        logError exn Local msg
        failwith msg

let inline getProperty (propName:string) (entity: DynamicTableEntity) =
    try
        entity.Properties.[propName]
    with
    | exn -> failwithf "Could not get property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message


let inline getOptionalProperty (propName:string) (entity: DynamicTableEntity) =
    match entity.Properties.TryGetValue propName with
    | true, v -> Some v
    | _ -> None

let inline getBoolProperty (propName:string) (entity: DynamicTableEntity) =
    let prop = getProperty propName entity
    try
        prop.BooleanValue.Value
    with
    | exn -> failwithf "Could not get boolean value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getStringProperty (propName:string) (entity: DynamicTableEntity) =
    let prop = getProperty propName entity
    try
        prop.StringValue
    with
    | exn -> failwithf "Could not get string value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getOptionalBoolProperty (propName:string) (entity: DynamicTableEntity) =
    try
        getOptionalProperty propName entity
        |> Option.map (fun prop -> prop.BooleanValue.Value)
    with
    | exn -> failwithf "Could not get bool value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message


let inline getOptionalStringProperty (propName:string) (entity: DynamicTableEntity) =
    try
        getOptionalProperty propName entity
        |> Option.map (fun prop -> prop.StringValue)
        // |> Option.defaultValue ""
    with
    | exn -> failwithf "Could not get string value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getDateTimeOffsetProperty (propName:string) (entity: DynamicTableEntity) =
    let prop = getProperty propName entity
    try
        prop.DateTimeOffsetValue.Value
    with
    | exn -> failwithf "Could not get DateTimeOffset value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getOptionalDateTimeOffsetProperty (propName:string) (entity: DynamicTableEntity) =
    try
        getOptionalProperty propName entity
        |> Option.map (fun prop -> prop.DateTimeOffsetValue.Value)
    with
    | exn -> failwithf "Could not get DateTimeOffset value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getIntProperty (propName:string) (entity: DynamicTableEntity) =
    let prop = getProperty propName entity
    try
        prop.Int32Value.Value
    with
    | exn -> failwithf "Could not get Int32 value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getBigIntProperty (propName:string) (entity: DynamicTableEntity) =
    let prop = getProperty propName entity
    try
        prop.Int64Value.Value
    with
    | exn -> failwithf "Could not get Int64 value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getOptionalIntProperty (propName:string) (entity: DynamicTableEntity) =
    try
        getOptionalProperty propName entity
        |> Option.map (fun prop -> prop.Int32Value.Value)
    with
    | exn -> failwithf "Could not get Int32 value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message

let inline getDoubleProperty (propName:string) (entity: DynamicTableEntity) =
    let prop = getProperty propName entity
    try
        prop.DoubleValue.Value
    with
    | exn -> failwithf "Could not get Double value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message
let inline getOptionalDoubleProperty (propName:string) (entity: DynamicTableEntity) =
    try
        getOptionalProperty propName entity
        |> Option.map (fun prop -> prop.DoubleValue.Value)
    with
    | exn -> failwithf "Could not get Double value of property %s for entity %s %s. Message: %s" propName entity.PartitionKey entity.RowKey exn.Message
let setStringProperty propertyName value (entity:DynamicTableEntity) =
    entity.Properties.[propertyName] <- EntityProperty.GeneratePropertyForString value
    entity
let setIntProperty propertyName value (entity:DynamicTableEntity) =
    entity.Properties.[propertyName] <- EntityProperty.GeneratePropertyForInt (Nullable value)
    entity
let setBigIntProperty propertyName value (entity:DynamicTableEntity) =
    entity.Properties.[propertyName] <- EntityProperty.GeneratePropertyForLong (Nullable value)
    entity    
let setDateTimeOffsetProperty propertyName value (entity:DynamicTableEntity) =
    entity.Properties.[propertyName] <- EntityProperty.GeneratePropertyForDateTimeOffset (Nullable value)
    entity
let setDoubleProperty propertyName value (entity:DynamicTableEntity) =
    entity.Properties.[propertyName] <- EntityProperty.GeneratePropertyForDouble (Nullable value)
    entity
let setBoolProperty propertyName value (entity:DynamicTableEntity) =
    entity.Properties.[propertyName] <- EntityProperty.GeneratePropertyForBool (Nullable value)
    entity
let setOptionalDoubleProperty propertyName value (entity:DynamicTableEntity) =
    match value with
    | Some x -> setDoubleProperty propertyName x entity
    | None -> entity    
let setOptionalIntProperty propertyName value (entity:DynamicTableEntity) =
    match value with
    | Some x -> setIntProperty propertyName x entity
    | None -> entity   

let setOptionalStringProperty propertyName value (entity:DynamicTableEntity) =
    match value with
    | Some x -> setStringProperty propertyName x entity
    | None -> entity   
let setOptionalBoolProperty propertyName value (entity:DynamicTableEntity) =
    match value with
    | Some x -> setBoolProperty propertyName x entity
    | None -> entity    
let setOptionalDateTimeProperty propertyName value (entity:DynamicTableEntity) =
    match value with
    | Some x -> setDateTimeOffsetProperty propertyName x entity
    | None -> entity    