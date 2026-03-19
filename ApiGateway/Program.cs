using ApiGateway.LoadBalancer;
using AppHost.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
var weights = builder.Configuration
    .GetSection("LoadBalancerWeights")
    .Get<Dictionary<string, double>>() ?? [];
builder.Services
    .AddOcelot()
    .AddCustomLoadBalancer<WeightedRandomBalancer>((_, _, discoveryProvider) => new(discoveryProvider.GetAsync, weights));

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
app.UseCors();

await app.UseOcelot();

app.Run();
