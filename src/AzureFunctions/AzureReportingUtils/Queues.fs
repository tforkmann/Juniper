module Queues

open Chia.PostToQueue
open SpecificDomain.Config
open Chia.FileWriter
open TriggerNames
open CloudTable.AzureConnection

let juniperReportsQueue = getQueue connected JuniperReports fileWriterInfo
let ecalationLvlHighQueue = getQueue connected EscalationLvlHigh fileWriterInfo
let ecalationLvlLowQueue = getQueue connected EscalationLvlLow fileWriterInfo
let sendReport = getQueue connected SendReport fileWriterInfo
