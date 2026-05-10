using System.Text.Json.Nodes;

namespace EventSink.Storage;

/// <summary>
/// Интерфейс службы для взаимодействия с файлами в S3.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Отправляет файл в хранилище.
    /// </summary>
    /// <param name="fileData">Строковая репрезентация сохраняемого файла.</param>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Получает список всех файлов из хранилища.
    /// </summary>
    /// <returns>Список путей к файлам.</returns>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Получает строковую репрезентацию файла из хранилища.
    /// </summary>
    /// <param name="filePath">Путь к файлу в бакете.</param>
    /// <returns>Строковая репрезентация прочтенного файла.</returns>
    public Task<JsonNode> DownloadFile(string filePath);

    /// <summary>
    /// Создает бакет.
    /// </summary>
    public Task EnsureBucketExists();
}
