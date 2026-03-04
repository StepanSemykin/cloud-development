using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Domain.Entities;

namespace CachingService.Services;

/// <summary>
/// Реализация сервиса кэширования с использованием распределенного кэша (Redis).
/// </summary>
public class CacheService(IDistributedCache cache, IConfiguration configuration) : ICacheService
{
    private readonly TimeSpan _cacheExpiration =  int.TryParse(configuration["CacheTime"], out var sec)
        ? TimeSpan.FromSeconds(sec)
        : TimeSpan.FromSeconds(3600);

    /// <summary>
    /// Извлекает данные пациента из кэша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <returns>
    /// Объект <see cref="MedicalPatient"/> если найден в кэше, иначе null.
    /// </returns>
    public async Task<MedicalPatient?> RetriveFromCache(int id)
    {
        var cachedData = await cache.GetStringAsync(id.ToString());
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<MedicalPatient>(cachedData);
        }

        return null;
    }

    /// <summary>
    /// Сохраняет данные пациента в кэш.
    /// </summary>
    /// <param name="patient">Объект <see cref="MedicalPatient"/> для сохранения.</param>
    public async Task PutInCache(MedicalPatient patient)
    {
        var data = JsonSerializer.Serialize(patient);
        await cache.SetStringAsync(patient.Id.ToString(), data,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });
    }
}