using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton(s =>
{
    var cosmosConnectionString = builder.Configuration["CosmosDbConnection"];
    return new CosmosClient(cosmosConnectionString, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    });
});

// Register Blob Storage client
builder.Services.AddSingleton(s =>
{
    var blobConnectionString = builder.Configuration["BlobStorageConnection"];
    return new BlobServiceClient(blobConnectionString);
});

builder.Build().Run();
