module EscalationLvlHigh

open Microsoft.Azure.WebJobs
open Newtonsoft.Json
open Juniper
open System.Net.Mail
open Chia.CreateBlob
open FSharp.Control.Tasks.ContextInsensitive
open System.IO
open System.Net.Mime
open Microsoft.Extensions.Logging
open TriggerNames
open Escalation
open CloudTable.BlobConfig

let server = "mail.addresse.something"
let user = "escalation@domain.something"
[<FunctionName("EscalationLvlHigh")>]

let Run([<QueueTrigger(EscalationLvlHigh)>] content:string, log:ILogger) =
    task {
        let mail = JsonConvert.DeserializeObject<MailContent>(content)
        let from = MailAddress(user,"EscalationLvlHigh")
        let tos = MailAddress(mail.RecipientEMail,mail.Recipient)


        use smtpclient = new SmtpClient(server)
        use message = new MailMessage(from,tos)
        message.Sender <- from
        message.Subject <- mail.Subject
        message.Body <- mail.Text

        for containerRef,blobID,name in mail.Attachments do
            let blobClient = connection.CreateCloudBlobClient()

            let invoiceContainer = blobClient.GetContainerReference containerRef
            let! _ = invoiceContainer.CreateIfNotExistsAsync()

            let blockBlob = invoiceContainer.GetBlockBlobReference(blobID)

            use stream = new MemoryStream()
            do! blockBlob.DownloadToStreamAsync(stream)
            let buffer = stream.GetBuffer()
            let attachmentStream = new MemoryStream(buffer)
            attachmentStream.Position <- int64 0
            let contentType = ContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            let data = new Attachment(attachmentStream, name)
            data.ContentType <- contentType
            message.Attachments.Add(data)
        smtpclient.Send(message)
    }
