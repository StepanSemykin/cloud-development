using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;

namespace Tests;

/// <summary>
/// Подготавливает окружение для интеграционных тестов: поднимает локальные ресурсы, предоставляет S3 клиент, ожидает инициализации.
/// </summary>
public class Fixture : IAsyncLifetime
{
    private const string BucketName = "medical-patient-bucket";

    public DistributedApplication App { get; private set; } = null!;
    public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;
    public AmazonS3Client StorageClient { get; private set; } = null!;

    /// <summary>
    /// Инициализация фикстуры: создание билдера, запуск приложения и подготовка S3.
    /// </summary>
    public async Task InitializeAsync()
    {
        Builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost_AppHost>();
        Builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        Builder.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
                options.Retry.MaxRetryAttempts = 10;
                options.Retry.Delay = TimeSpan.FromSeconds(3);
            }));

        App = await Builder.BuildAsync();
        await App.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceHealthyAsync("localstack", cts.Token),
            App.ResourceNotifications.WaitForResourceHealthyAsync("apigateway", cts.Token),
            App.ResourceNotifications.WaitForResourceHealthyAsync("eventsink", cts.Token)
        );

        var localStackUrl = App.GetEndpoint("localstack", "http").ToString().TrimEnd('/');
        StorageClient = new AmazonS3Client("test", "test", new AmazonS3Config
        {
            ServiceURL = localStackUrl,
            ForcePathStyle = true
        });

        await EnsureBucketExistsAsync(cts.Token);
    }

    /// <summary>
    /// Ожидает появления объектов в S3 с указанным префиксом.
    /// </summary>
    /// <param name="prefix">Префикс ключа объекта в бакете.</param>
    /// <param name="maxAttempts">Максимальное число попыток проверки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список найденных объектов S3.</returns>
    public async Task<List<S3Object>> WaitForObjectAsync(string prefix, int maxAttempts = 10, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var listResponse = await StorageClient.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = prefix
            }, cancellationToken);

            if (listResponse.S3Objects.Count > 0)
            {
                return listResponse.S3Objects;
            }
        }

        return [];
    }

    /// <summary>
    /// Создаёт бакет в S3, если он не существует.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var buckets = await StorageClient.ListBucketsAsync(cancellationToken);

        if (buckets.Buckets.Exists(b => b.BucketName == BucketName))
        {
            return;
        }
 
        await StorageClient.PutBucketAsync(new PutBucketRequest
        {
            BucketName = BucketName,
            UseClientRegion = true
        }, cancellationToken);
    }

    /// <summary>
    /// Останавливает тесты и освобождаяет ресурсы.
    /// </summary>
    public async Task DisposeAsync()
    {
        StorageClient.Dispose();

        await App!.StopAsync();
        await App.DisposeAsync();
        await Builder!.DisposeAsync();
    }
}
