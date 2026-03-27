var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "cache-insight");

var gateway = builder.AddProject<Projects.ApiGateway>("apigateway");

for (var i = 0; i < 5; ++i)
{
    var generationService = builder.AddProject<Projects.GenerationService>($"generation-service-{i}", launchProfileName: null)
        .WithHttpEndpoint(8000 + i)
        .WithReference(cache, "RedisCache")
        .WaitFor(cache)
        .WithHttpHealthCheck("/health");
    gateway
        .WithReference(generationService)
        .WaitFor(generationService);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
