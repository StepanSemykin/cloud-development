using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Domain.Entities;
using System.Net;
using System.Text.Json;

namespace GenerationService.Messaging;

/// <summary>
/// Сервис для публикации сообщений в SNS.
/// </summary>
/// <param name="client">Клиент SNS.</param>
/// <param name="configuration">Конфигурация приложерния.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class PublicationService(IAmazonSimpleNotificationService client, IConfiguration configuration, ILogger<PublicationService> logger) : IProductionService
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new InvalidOperationException("AWS:Resources:SNSTopicArn is not configured");

    ///<inheritdoc/>
    public async Task SendMessage(MedicalPatient patient)
    {
        try
        {
            var json = JsonSerializer.Serialize(patient);
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };

            var response = await client.PublishAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Patient {Id} was published to SNS", patient.Id);
            }
            else
            {
                throw new Exception($"SNS publish returned {response.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish patient {Id} to SNS", patient.Id);
        }
    }
}
