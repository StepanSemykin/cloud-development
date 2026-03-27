using ApiGateway.LoadBalancer;
using AppHost.ServiceDefaults;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Globalization;

static (Dictionary<string, string?> RouteOverrides, Dictionary<string, double> weights)
    BuildEnv(IConfiguration configuration)
{
    var namedWeights = configuration
        .GetSection("LoadBalancerWeights")
        .Get<Dictionary<string, double>>() ?? [];

    var routeOverrides = new Dictionary<string, string?>();
    var weights = new Dictionary<string, double>();

    Uri? firstEndpoint = null;
    for (var i = 0; i < 5; ++i)
    {
        var envKey = $"services__generation-service-{i}__http__0";
        var raw = Environment.GetEnvironmentVariable(envKey);
        if (string.IsNullOrWhiteSpace(raw))
            continue;

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid endpoint in {envKey}: {raw}");

        firstEndpoint ??= uri;

        routeOverrides[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] = uri.Host;
        routeOverrides[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] =
            uri.Port.ToString(CultureInfo.InvariantCulture);

        var key = $"{uri.Host}_{uri.Port}";
        var serviceName = $"generation-service-{i}";
        weights[key] = namedWeights.TryGetValue(serviceName, out var w) ? w : 1.0;
    }

    routeOverrides["Routes:0:DownstreamScheme"] = firstEndpoint!.Scheme;

    return (routeOverrides, weights);
}

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var (routeOverrides, weights) = BuildEnv(builder.Configuration);
builder.Configuration.AddInMemoryCollection(routeOverrides);

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