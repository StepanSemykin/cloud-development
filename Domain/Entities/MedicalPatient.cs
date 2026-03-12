using System.Text.Json.Serialization;

namespace Domain.Entities;

/// <summary>
/// Класс определяющий пациента в медицинской базе данных.
/// </summary>
public class MedicalPatient
{
    /// <summary>
    /// Идентификатор пациента.
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    /// <summary>
    /// Имя.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Адрес.
    /// </summary>
    [JsonPropertyName("address")]
    public required string Address { get; set; }

    /// <summary>
    /// Дата рождения.
    /// </summary>
    [JsonPropertyName("birthDate")]
    public required DateOnly BirthDate { get; set; }

    /// <summary>
    /// Рост в метрах.
    /// </summary>
    [JsonPropertyName("height")]
    public required double Height { get; set; }

    /// <summary>
    /// Масса в килограммах.
    /// </summary>
    [JsonPropertyName("weight")]
    public required double Weight { get; set; }

    /// <summary>
    /// Группа крови.
    /// </summary>
    [JsonPropertyName("bloodGroup")]
    public int? BloodGroup { get; set; }

    /// <summary>
    /// Резус-фактор.
    /// </summary>
    [JsonPropertyName("rh")]
    public bool? Rh { get; set; }

    /// <summary>
    /// Дата последнего визита.
    /// </summary>
    [JsonPropertyName("lastVisit")]
    public DateOnly? LastVisit { get; set; }

    /// <summary>
    /// Статус вакцинации.
    /// </summary>
    [JsonPropertyName("vaccination")]
    public bool? Vaccination { get; set; }
}
