using Domain.Entities;

namespace GenerationService.Services;

/// <summary>
/// Интерфейс для генератора тестовых данных пациентов медицинской базы данных.
/// </summary>
public interface IGeneratorService
{
    /// <summary>
    /// Обрабатывает запрос на генерацию данных пациента с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <returns>Сгенерированный объект <see cref="MedicalPatient"/> с заполненными полями.</returns>
    public Task<MedicalPatient> GenerateAsync(int id);
}
