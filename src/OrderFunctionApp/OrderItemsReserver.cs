using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace OrderFunctionApp;

public class OrderItemsReserver
{
    private readonly ILogger<OrderItemsReserver> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public OrderItemsReserver(ILogger<OrderItemsReserver> logger, BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    [Function(nameof(OrderItemsReserver))]
    public async Task Run(
        [ServiceBusTrigger("order", "subscribeorder", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        // Convert message body to string
        string body = message.Body.ToString();
        _logger.LogInformation("Message Body: {body}", body);

        // Upload to Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient("orders");
        await containerClient.CreateIfNotExistsAsync();

        string blobName = $"{message.MessageId}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var blobClient = containerClient.GetBlobClient(blobName);

        byte[] bytes = Encoding.UTF8.GetBytes(body);
        using var stream = new MemoryStream(bytes);

        await blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogInformation("Saved message to blob {blobName}", blobName);

        // Complete the message in Service Bus
        await messageActions.CompleteMessageAsync(message);
    }
}