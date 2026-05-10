using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Service.Api.Entities;
using System.Text.Json;
using Xunit.Abstractions;

namespace Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Aspire_AppHost>(cancellationToken);
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        _builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });
    }

    /// <summary>
    /// Проверяет, что вызов гейтвея:
    /// <list type="bullet">
    /// <item><description>В ответ отправляет сгенерированный ЗУ</description></item>
    /// <item><description>Сериализует ЗУ в S3 хранилище</description></item>
    /// <item><description>Проверяет, что данные из предыдущих пунктов идентичны</description></item>
    /// </list>
    /// </summary>
    /// <param name="envName">Запускаемый лаунч профайл</param>
    [Theory]
    [InlineData("SQS+MinioS3")]
    [InlineData("SNS+MinioS3")]
    [InlineData("SQS+LocalstackS3")]
    [InlineData("SNS+LocalstackS3")]
    public async Task TestPipeline(string envName)
    {
        var cancellationToken = CancellationToken.None;
        _builder!.Environment.EnvironmentName = envName;
        _app = await _builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var random = new Random();
        var id = random.Next(1, 100);
        using var gatewayClient = _app.CreateHttpClient("landplot-api-gateway", "http");
        using var gatewayResponse = await gatewayClient!.GetAsync($"/land-plot?id={id}");
        var apiLandplot = JsonSerializer.Deserialize<LandPlot>(await gatewayResponse.Content.ReadAsStringAsync());

        await Task.Delay(5000);
        using var sinkClient = _app.CreateHttpClient("landplot-sink", "http");
        using var listResponse = await sinkClient!.GetAsync($"/api/s3");
        var landplotList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());
        using var s3Response = await sinkClient!.GetAsync($"/api/s3/landplot_{id}.json");
        var s3Landplot = JsonSerializer.Deserialize<LandPlot>(await s3Response.Content.ReadAsStringAsync());

        Assert.NotNull(landplotList);
        Assert.Single(landplotList);
        Assert.NotNull(apiLandplot);
        Assert.NotNull(s3Landplot);
        Assert.Equal(id, s3Landplot.Id);
        Assert.Equivalent(apiLandplot, s3Landplot);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
        await _builder!.DisposeAsync();
    }
}
