using ShipmentFinishGood.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentationLayer()
    .AddPersistenceLayer(builder.Configuration)
    .AddDomainServices()
    .AddAppAuthentication(builder.Configuration);

var app = builder.Build();

app
    .UseAppRequestPipeline()
    .InitializeDatabase()
    .Run();
