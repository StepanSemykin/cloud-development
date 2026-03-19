using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancer;

/// <summary>
/// Балансировщик с взвешенным случайным выбором реплики сервиса.
/// </summary>
/// <param name="services">Все доступные экземпляры сервиса из service discovery.</param>
/// <param name="weights">Набор весов для эндпоинтов в формате key:"host_port" value:"weight".
/// </param>
public class WeightedRandomBalancer(Func<Task<List<Service>>> services, Dictionary<string, double> weights) : ILoadBalancer
{
    public string Type => nameof(WeightedRandomBalancer);

    private readonly Func<Task<List<Service>>> _services = services;
    private readonly Dictionary<string, double> _weights = weights;

    /// <summary>
    /// Выбирает реплику сервиса по алгоритму weighted random.
    /// </summary>
    /// <param name="services">Список доступных сервисов.</param>
    /// <param name="weights">Словарь весов для всех сервисов.</param>
    /// <returns>Выбранная реплика сервиса.</returns>
    private Service GetServiceByWeight(List<Service> services, Dictionary<string, double> weights)
    {
        var cumulativeWeights = new double[services.Count];
        double sum = 0;
        for (var i = 0; i < services.Count; ++i)
        {
            var service = services[i];
            var key = $"{service.HostAndPort.DownstreamHost}_{service.HostAndPort.DownstreamPort}";
            var weight = weights.TryGetValue(key, out var w) ? w : 1.0;

            sum += weight;
            cumulativeWeights[i] = sum;
        }

        if (sum <= 0)
        {
            return services[Random.Shared.Next(services.Count)];
        }

        var randomValue = Random.Shared.NextDouble();

        var index = Array.BinarySearch(cumulativeWeights, randomValue);
        if (index < 0)
        {
            index = ~index;
        }
        index = Math.Min(index, services.Count - 1);

        return services[index];
    }

    /// <summary>
    /// Выдает эндпоинт сервиса для текущего запроса.
    /// </summary>
    /// <param name="httpContext">Контекст входящего запроса.</param>
    /// <returns>Выбранный адрес сервиса или ошибка, если сервисы недоступны.</returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services.Invoke();
        if (services == null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreNullError("No services available")
            );
        }

        var selectedService = GetServiceByWeight(services, _weights);

        return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
    }

    /// <summary>
    /// Освобождает ранее выданный эндпоинт.
    /// </summary>
    /// <param name="serviceHostAndPort">Адрес освобождаемого сервиса.</param>
    public void Release(ServiceHostAndPort serviceHostAndPort) { }
}
