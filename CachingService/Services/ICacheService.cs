using Domain.Entities;

namespace CachingService.Services;

/// <summary>
/// Интерфейс для сервиса кэширования пациентов медицинской базы данных.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Извлекает данные пациента из кэша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    public Task<MedicalPatient?> RetriveFromCache(int id);

    /// <summary>
    /// Сохраняет данные пациента в кэш.
    /// </summary>
    /// <param name="patient">Объект <see cref="MedicalPatient"/> для сохранения в кэш.</param>
    public Task PutInCache(MedicalPatient patient);
}
