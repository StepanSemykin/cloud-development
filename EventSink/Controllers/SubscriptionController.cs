using Amazon.SimpleNotificationService.Util;
using EventSink.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EventSink.Controllers;

/// <summary>
/// Контроллер для обработки входящих сообщений SNS.
/// </summary>
/// <param name="storageService">Служба для загрузки данных в S3 хранилище.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
[ApiController]
[Route("api/sns")]
public class SubscriptionController(IStorageService storageService, ILogger<SubscriptionController> logger) : ControllerBase
{
    /// <summary>
    /// Принимает и обрабатывает входящее сообщение (подтверждение подписки и уведомления).
    /// При получении уведомления содержимое сообщения сохраняется в S3.
    /// </summary>
    /// <returns>Результат обработки HTTP-запроса.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveMessage()
    {
        logger.LogInformation("SNS webhook was called");

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            var snsMessage = Message.ParseMessage(jsonContent);

            if (snsMessage.Type == "SubscriptionConfirmation")
            {
                logger.LogInformation("SubscriptionConfirmation was received");

                using var httpClient = new HttpClient();
                var builder = new UriBuilder(new Uri(snsMessage.SubscribeURL))
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = 4566
                };

                var response = await httpClient.GetAsync(builder.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SubscriptionConfirmation returned {response.StatusCode}: {body}");
                }

                logger.LogInformation("Subscription was successfully confirmed");

                return Ok();
            }

            if (snsMessage.Type == "Notification")
            {
                logger.LogInformation("Notification received: {message}", snsMessage.MessageText);
                await storageService.UploadFile(snsMessage.MessageText);
                logger.LogInformation("Notification was successfully processed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while processing SNS notifications");
        }

        return Ok();
    }
}