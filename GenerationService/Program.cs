using AppHost.ServiceDefaults;
using CachingService.Services;
using GenerationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<IGeneratorService, GeneratorService>();
builder.Services.AddScoped<ICacheService, CacheService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
var allowedMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>();
var allowedHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins != null)
        {
            _ = allowedOrigins.Contains("*")
                ? policy.AllowAnyOrigin()
                : policy.WithOrigins(allowedOrigins);
        }

        if (allowedMethods != null)
        {
            _ = allowedMethods.Contains("*")
                ? policy.AllowAnyMethod()
                : policy.WithMethods(allowedMethods);
        }

        if (allowedHeaders != null)
        {
            _ = allowedHeaders.Contains("*")
                ? policy.AllowAnyHeader()
                : policy.WithHeaders(allowedHeaders);
        }
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/patient", (IGeneratorService service, int id) => service.GenerateAsync(id));
app.UseCors();

app.Run();
