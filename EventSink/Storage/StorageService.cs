using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventSink.Storage;

/// <summary>
/// Cлужба для манипуляции файлами в S3.
/// </summary>
/// <param name="client">S3 клиент.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class StorageService(IAmazonS3 client, IConfiguration configuration, ILogger<StorageService> logger) : IStorageService
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    ///<inheritdoc/>
    public async Task<List<string>> GetFileList()
    {
        var list = new List<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = "",
            Delimiter = ",",
        };
        var paginator = client.Paginators.ListObjectsV2(request);

        logger.LogInformation("Began listing files in {bucket}", _bucketName);

        await foreach (var response in paginator.Responses)
            if (response != null && response.S3Objects != null)
            {
                foreach (var obj in response.S3Objects)
                {
                    if (obj != null)
                    {
                        list.Add(obj.Key);
                    }
                    else
                    {
                        logger.LogWarning("Received null object from {bucket}", _bucketName);
                    }
                }
            }
            else
            {
                logger.LogWarning("Received null response from {bucket}", _bucketName);
            }

        return list;
    }

    ///<inheritdoc/>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["id"]?.GetValue<int>() ?? throw new ArgumentException("Passed JSON has invalid structure");

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Began uploading patient {file} onto {bucket}", id, _bucketName);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"patient_{id}.json",
            InputStream = stream
        };

        var response = await client.PutObjectAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload patient {file}: {code}", id, response.HttpStatusCode);

            return false;
        }

        logger.LogInformation("Finished uploading patient {file} to {bucket}", id, _bucketName);

        return true;
    }

    ///<inheritdoc/>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Began downloading {file} from {bucket}", key, _bucketName);

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            using var response = await client.GetObjectAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Failed to download {file}: {code}", key, response.HttpStatusCode);
                throw new InvalidOperationException($"Error occurred downloading {key} - {response.HttpStatusCode}");
            }
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);

            return JsonNode.Parse(reader.ReadToEnd()) ?? throw new InvalidOperationException($"Downloaded document is not a valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {file} downloading ", key);
            throw;
        }
    }

    ///<inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether {bucket} exists", _bucketName);

        try
        {
            await client.EnsureBucketExistsAsync(_bucketName);
            logger.LogInformation("{bucket} existence ensured", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred during {bucket} check", _bucketName);
            throw;
        }
    }
}
