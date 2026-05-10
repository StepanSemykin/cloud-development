using EventSink.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;

namespace EventSink.Controllers;

/// <summary>
/// Контроллер для взаимодействия с S3.
/// </summary>
/// <param name="storageService">Служба для работы с S3.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
[ApiController]
[Route("api/storage")]
public class StorageController(IStorageService storageService, ILogger<StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Получает список хранящихся в S3 файлов.
    /// </summary>
    /// <returns>Список с ключами файлов.</returns>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("Method {method} of {controller} was called", nameof(ListFiles), nameof(StorageController));

        try
        {
            var list = await storageService.GetFileList();
            logger.LogInformation("Got a list of {count} files from bucket", list.Count);

            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured during {method} of {controller}", nameof(ListFiles), nameof(StorageController));

            return BadRequest(ex);
        }
    }

    /// <summary>
    /// Получает строковое представление хранящегося в S3 документа.
    /// </summary>
    /// <param name="key">Ключ файла.</param>
    /// <returns>Строковое представление файла.</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("Method {method} of {controller} was called", nameof(GetFile), nameof(StorageController));

        try
        {
            var node = await storageService.DownloadFile(key);
            logger.LogInformation("Received json of {size} bytes", Encoding.UTF8.GetByteCount(node.ToJsonString()));

            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured during {method} of {controller}", nameof(GetFile), nameof(StorageController));

            return BadRequest(ex);
        }
    }
}
