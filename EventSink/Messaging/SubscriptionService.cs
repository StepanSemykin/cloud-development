using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace EventSink.Messaging;

/// <summary>
/// Служба для подписки на SNS.
/// </summary>
/// <param name="snsClient">Клиент SNS.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class SubscriptionService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SubscriptionService> logger)
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    private readonly string _endpoint = configuration["AWS:Resources:SNSUrl"]
        ?? throw new KeyNotFoundException("SNS endpoint was not found in configuration");

    /// <summary>
    /// Эндпоинт для отправки запроса на подписку.
    /// </summary>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Sending subscribe request for {topic}", _topicArn);

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = _endpoint,
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to subscribe to {topic}", _topicArn);
        }
        else
        {
            logger.LogInformation("Subscription request for {topic} is successful, waiting for confirmation", _topicArn);
        }
    }
}