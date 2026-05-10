using Domain.Entities;
using System.Text.Json;

namespace Tests;

/// <summary>
/// Проводит интеграционные тесты.
/// </summary>
public class IntegrationTests(Fixture fixture) : IClassFixture<Fixture>
{
    private static readonly Random _random = new();

    /// <summary>
    /// Проверяет полный пайплайн: генерация пациента, публикация в SNS, сохранение в S3.
    /// </summary>
    [Fact]
    public async Task Pipeline()
    {
        var id = _random.Next(1, 100);

        using var gatewayClient = fixture.App.CreateHttpClient("apigateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/patient?id={id}");
        var patient = JsonSerializer.Deserialize<MedicalPatient>(
            await gatewayResponse.Content.ReadAsStringAsync());

        var storageObjects = await fixture.WaitForObjectAsync($"patient_{id}.json");

        using var sinkClient = fixture.App.CreateHttpClient("eventsink", "http");
        using var listResponse = await sinkClient.GetAsync("/api/storage");
        var fileList = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync());

        using var storageResponse = await sinkClient.GetAsync($"/api/storage/patient_{id}.json");
        var storagePatient = JsonSerializer.Deserialize<MedicalPatient>(
            await storageResponse.Content.ReadAsStringAsync());

        Assert.NotEmpty(storageObjects);
        Assert.NotNull(fileList);
        Assert.Contains($"patient_{id}.json", fileList);
        Assert.NotNull(patient);
        Assert.NotNull(storagePatient);
        Assert.Equal(id, storagePatient.Id);
        Assert.Equivalent(patient, storagePatient);
    }

    /// <summary>
    /// Проверяет что Api Gateway возвращает 200.
    /// </summary>
    [Fact]
    public async Task Gateway()
    {
        var id = _random.Next(1, 100);

        using var gatewayClient = fixture.App.CreateHttpClient("apigateway", "http");
        using var response = await gatewayClient.GetAsync($"/patient?id={id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет что данные пациентов сохраняются корректно.
    /// </summary>
    [Fact]
    public async Task MultiplePatients()
    {
        var ids = Enumerable.Range(1, 3).Select(_ => _random.Next(100, 200)).ToList();

        using var gatewayClient = fixture.App.CreateHttpClient("apigateway", "http");
        foreach (var id in ids)
            await gatewayClient.GetAsync($"/patient?id={id}");

        foreach (var id in ids)
            await fixture.WaitForObjectAsync($"patient_{id}.json");

        using var sinkClient = fixture.App.CreateHttpClient("eventsink", "http");
        using var listResponse = await sinkClient.GetAsync("/api/storage");
        var fileList = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);
        foreach (var id in ids)
            Assert.Contains($"patient_{id}.json", fileList);
    }

    /// <summary>
    /// Проверяет что список файлов в S3 не пустой после генерации.
    /// </summary>
    [Fact]
    public async Task FileList()
    {
        var id = _random.Next(200, 300);

        using var gatewayClient = fixture.App.CreateHttpClient("apigateway", "http");
        await gatewayClient.GetAsync($"/patient?id={id}");
        await fixture.WaitForObjectAsync($"patient_{id}.json");

        using var sinkClient = fixture.App.CreateHttpClient("eventsink", "http");
        using var listResponse = await sinkClient.GetAsync("/api/storage");
        var fileList = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);
        Assert.NotEmpty(fileList);
    }

    /// <summary>
    /// Проверяет что файл скачивается и десериализуется корректно.
    /// </summary>
    [Fact]
    public async Task DownloadingFile()
    {
        var id = _random.Next(300, 400);

        using var gatewayClient = fixture.App.CreateHttpClient("apigateway", "http");
        await gatewayClient.GetAsync($"/patient?id={id}");
        await fixture.WaitForObjectAsync($"patient_{id}.json");

        using var sinkClient = fixture.App.CreateHttpClient("eventsink", "http");
        using var storageResponse = await sinkClient.GetAsync($"/api/storage/patient_{id}.json");
        var patient = JsonSerializer.Deserialize<MedicalPatient>(
            await storageResponse.Content.ReadAsStringAsync());

        Assert.NotNull(patient);
        Assert.Equal(id, patient.Id);
        Assert.False(string.IsNullOrEmpty(patient.Name));
        Assert.False(string.IsNullOrEmpty(patient.Address));
    }

    /// <summary>
    /// Проверяет что повторный запрос того же id возвращает верного пациента.
    /// </summary>
    [Fact]
    public async Task RetrieveFromCache()
    {
        var id = _random.Next(400, 500);

        using var gatewayClient = fixture.App.CreateHttpClient("apigateway", "http");
        using var firstResponse = await gatewayClient.GetAsync($"/patient?id={id}");
        var firstPatient = JsonSerializer.Deserialize<MedicalPatient>(
            await firstResponse.Content.ReadAsStringAsync());

        using var secondResponse = await gatewayClient.GetAsync($"/patient?id={id}");
        var secondPatient = JsonSerializer.Deserialize<MedicalPatient>(
            await secondResponse.Content.ReadAsStringAsync());

        Assert.NotNull(firstPatient);
        Assert.NotNull(secondPatient);
        Assert.Equivalent(firstPatient, secondPatient);
    }
}
