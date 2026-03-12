using CachingService.Services;
using Domain.Entities;

namespace GenerationService.Services;

/// <summary>
/// Сервис для генерации данных пациентов с поддержкой кэширования.
/// </summary>
/// <param name="cacheService">Сервис для работы с кэшем Redis.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class GeneratorService(ICacheService cacheService, ILogger<GeneratorService> logger) : IGeneratorService
{
    /// <summary>
    /// Обрабатывает запрос на генерацию данных пациента с указанным идентификатором.
    /// Cначала проверяет наличие данных в кэше, если данных нет - генерирует новые и сохраняет в кэш.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <returns>Сгенерированный объект <see cref="MedicalPatient"/> с заполненными полями.</returns>
    public async Task<MedicalPatient> GenerateAsync(int id)
    {
        MedicalPatient? cachedPatient = null;

        try
        {
            cachedPatient = await cacheService.RetriveFromCache(id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for Id {Id}.", id);
        }

        if (cachedPatient is not null)
        {
            logger.LogInformation("Patient with Id {Id} retrieved from cache.", id);

            return cachedPatient;
        }

        MedicalPatient patient;
        try
        {
            patient = Generator.Generate(id);
            logger.LogInformation("Patient with Id {Id} generated.", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Patient generation failed for Id {Id}", id);
            throw;
        }

        try
        {
            await cacheService.PutInCache(patient);
            logger.LogInformation("Patient with Id {Id} stored in cache.", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for Id {Id}", id);
        }

        return patient;
    }
}
