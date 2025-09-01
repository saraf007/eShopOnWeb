using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class OrderServiceBus
{
    private readonly IConfiguration _configuration;

    public OrderServiceBus(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private const string topicName = "order";

    public async Task Orders(Order order)
    {
        string serviceBusConnectionString = _configuration.GetConnectionString("ServiceBusConnection")!;

        // Create a client
        await using var client = new ServiceBusClient(serviceBusConnectionString);

        // Create a sender for the topic
        ServiceBusSender sender = client.CreateSender(topicName);

        try
        {
            string jsonBody = JsonSerializer.Serialize(order);

            ServiceBusMessage message = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonBody))
            {
                ContentType = "application/json" // good practice
            };

            await sender.SendMessageAsync(message);

            Console.WriteLine($"Sent order {order.Id} to topic {topicName}");
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
