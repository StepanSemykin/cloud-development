namespace Domain.SeedData;

/// <summary>
/// Статические наборы данных для генерации ФИО пациентов.
/// </summary>
public static class SeedNames
{
    /// <summary>
    /// Список имён для случайной генерации.
    /// </summary>
    public static readonly string[] firstNames =
    {
        "James", "John", "Robert", "Michael", "William",
        "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth"
    };

    /// <summary>
    /// Список фамилий для случайной генерации.
    /// </summary>
    public static readonly string[] lastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones",
        "Miller", "Davis", "Garcia", "Rodriguez", "Wilson"
    };

    /// <summary>
    /// Список отчеств для случайной генерации.
    /// </summary>
    public static readonly string[] middleNames =
    {
        "Alexander", "Edward", "Joseph", "Thomas", "Charles",
        "Ann", "Marie", "Louise", "Grace", "Elizabeth"
    };
}
