using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables
            .Add("DEBUG", "1");
        container.AdditionalEnvironmentVariables
            .Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });

var cloudFormationTemplate = "CloudFormation/event-sink-sns-localstack.yaml";
var awsResources = builder.AddAWSCloudFormationTemplate("resources", cloudFormationTemplate, "event-sink")
    .WithReference(awsConfig)
    .WaitFor(localstack!);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "cache-insight");

var gateway = builder.AddProject<Projects.ApiGateway>("apigateway");

for (var i = 0; i < 5; ++i)
{
    var generationService = builder.AddProject<Projects.GenerationService>($"generation-service-{i}", launchProfileName: null)
        .WithHttpEndpoint(8000 + i)
        .WithReference(cache, "RedisCache")
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources)
        .WithHttpHealthCheck("/health");
    gateway
        .WithReference(generationService)
        .WaitFor(generationService);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.AddProject<Projects.EventSink>("eventsink", launchProfileName: null)
    .WithHttpEndpoint(5280)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(awsResources)
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();
