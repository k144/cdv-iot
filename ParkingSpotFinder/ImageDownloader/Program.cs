using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Database;
using Azure.Storage.Blobs;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddDbContext<ParkingDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton(serviceProvider =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    return new BlobServiceClient(connectionString);
});

builder.Services.AddHttpClient();

builder.Build().Run();
