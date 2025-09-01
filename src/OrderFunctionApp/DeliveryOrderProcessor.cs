using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderFunctionApp;

public class DeliveryOrderProcessor
{
    private readonly ILogger<DeliveryOrderProcessor> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public DeliveryOrderProcessor(ILogger<DeliveryOrderProcessor> logger, CosmosClient cosmosClient)
    {
        _logger = logger;

        _cosmosClient = cosmosClient;
        _container = _cosmosClient.GetContainer("eshopdb", "orders");
    }

    [Function("DeliveryOrderProcessor")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("DeliveryOrderProcessor function triggered.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        OrderDto order;
        try
        {
            order = JsonConvert.DeserializeObject<OrderDto>(requestBody);
        }
        catch
        {
            return new BadRequestObjectResult("Invalid payload format");
        }

        if (order == null)
        {
            return new BadRequestObjectResult("Order cannot be null");
        }

        // Ensure Cosmos DB id is set
        order.Id = order.OrderId.ToString();

        // Store in Cosmos DB
        await _container.CreateItemAsync(order, new PartitionKey(order.Id));

        // Log or process order
        _logger.LogInformation($"Received Order {order.OrderId} for Buyer {order.Id}");
        _logger.LogInformation($"Shipping to {order.ShippingAddress.Street}, {order.ShippingAddress.City}");

        foreach (var item in order.Items)
        {
            _logger.LogInformation($"Item: {item.ProductName}, Qty: {item.Units}, Price: {item.UnitPrice}");
        }

        // You can now add logic like saving to Cosmos DB, pushing to Service Bus, etc.

        return new OkObjectResult(new
        {
            Message = "Order received successfully",
            OrderId = order.OrderId,
            TotalItems = order.Items.Count
        });
    }
}

public class OrderDto
{
    public string Id { get; set; }  // Required by Cosmos DB
    public int OrderId { get; set; }
    public string BuyerId { get; set; }
    public AddressDto ShippingAddress { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

public class AddressDto
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string ZipCode { get; set; }
}

public class OrderItemDto
{
    public int ProductItemId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
}