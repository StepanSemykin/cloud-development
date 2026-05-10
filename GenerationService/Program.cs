using Amazon.SimpleNotificationService;
using AppHost.ServiceDefaults;
using CachingService.Services;
using GenerationService.Messaging;
using GenerationService.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<IProductionService, PublicationService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/patient", (IGeneratorService service, int id) => service.GenerateAsync(id));

app.Run();
