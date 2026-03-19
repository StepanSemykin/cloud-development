using AppHost.ServiceDefaults;
using CachingService.Services;
using GenerationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");
builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddScoped<ICacheService, CacheService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/patient", (IGeneratorService service, int id) => service.GenerateAsync(id));

app.Run();
