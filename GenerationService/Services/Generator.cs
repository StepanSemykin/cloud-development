using Bogus;
using Domain.Entities;

namespace GenerationService.Services;

/// <summary>
/// Статический генератор тестовых данных для пациентов медицинской базы данных.
/// </summary>
public static class Generator
{
    private const double MinAdultHeight = 1.50;
    private const double MaxAdultHeight = 2.20;
    private const int BloodGroupCount = 4;
    private const int MaxAge = 120;
    private const int MinBmi = 18;
    private const int MaxBmi = 30;

    private const int DigitsRound = 2;

    private static readonly Faker<MedicalPatient> _faker = new Faker<MedicalPatient>("en")
        .RuleFor(m => m.Name, f => $"{f.Name.FirstName()} {f.Name.LastName()}")
        .RuleFor(m => m.Address, f => f.Address.FullAddress())
        .RuleFor(m => m.BirthDate, f => f.Date.PastDateOnly(MaxAge))
        .RuleFor(m => m.Height, (f, m) => GenerateHeightByAge(m.BirthDate, f))
        .RuleFor(m => m.Weight, (f, m) => GenerateWeightByHeight(m.Height, f))
        .RuleFor(m => m.BloodGroup, f => f.Random.Int(1, BloodGroupCount))
        .RuleFor(m => m.Rh, f => f.Random.Bool())
        .RuleFor(m => m.LastVisit, (f, m) => f.Date.BetweenDateOnly(m.BirthDate, DateOnly.FromDateTime(DateTime.Now)))
        .RuleFor(m => m.Vaccination, f => f.Random.Bool());

    /// <summary>
    /// Определяет диапазон роста на основе возраста.
    /// </summary>
    /// <param name="age">Возраст пациента в годах.</param>
    /// <returns>Минимальный и максимальный рост для данного возраста.</returns>
    private static (double Min, double Max) GetHeightRangeByAge(double age)
    {
        return age switch
        {
            < 1 => (0.35, 0.80),
            < 3 => (0.80, 1.00),
            < 7 => (1.00, 1.30),
            < 13 => (1.30, 1.60),
            < 18 => (1.60, 1.90),
            _ => (MinAdultHeight, MaxAdultHeight)
        };
    }

    /// <summary>
    /// Генерирует рост пациента на основе возраста.
    /// </summary>
    /// <param name="birthDate">Дата рождения пациента.</param>
    /// <param name="faker">Генератор случайных данных.</param>
    /// <returns>Рост пациента в метрах.</returns>
    private static double GenerateHeightByAge(DateOnly birthDate, Faker faker)
    {
        var age = CalculateAge(birthDate);
        var (minHeight, maxHeight) = GetHeightRangeByAge(age);

        return Math.Round(faker.Random.Double(minHeight, maxHeight), DigitsRound);
    }

    /// <summary>
    /// Генерирует массу пациента на основе роста через индекс массы тела.
    /// </summary>
    /// <param name="height">Рост пациента в метрах.</param>
    /// <param name="faker">Генератор случайных данных.</param>
    /// <returns>Масса пациента в килограммах.</returns>
    private static double GenerateWeightByHeight(double height, Faker faker)
    {
        var bmi = faker.Random.Double(MinBmi, MaxBmi);

        return (int)(bmi * height * height);
    }

    /// <summary>
    /// Вычисляет возраст пациента на основе его даты рождения и текущей даты. 
    /// Учитывает случаи, когда день рождения еще не наступил в текущем году.
    /// </summary>
    /// <param name="birthDate">Дата рождения пациента.</param>
    /// <returns>Возраст пациента в годах.</returns>
    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    /// <summary>
    /// Генерирует случайного пациента с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <returns>Сгенерированный объект <see cref="MedicalPatient"/> с заполненными полями.</returns>
    public static MedicalPatient Generate(int id)
    {
        var patient = _faker.Generate();
        patient.Id = id;

        return patient;
    }
}
