using Domain.Entities;

namespace GenerationService.Messaging;

/// <summary>
/// Интерфейс для службы публикации сообщений.
/// </summary>
public interface IProductionService
{
    /// <summary>
    /// Публикует сообщение с данными.
    /// </summary>
    /// <param name="patient">Данные медицинского пациента для публикации.</param>
    public Task SendMessage(MedicalPatient patient);
}
