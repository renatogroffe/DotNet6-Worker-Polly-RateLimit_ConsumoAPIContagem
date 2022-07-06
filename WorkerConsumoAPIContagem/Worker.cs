using System.Net.Http.Json;
using WorkerConsumoAPIContagem.Models;
using Polly.RateLimit;

namespace WorkerConsumoAPIContagem;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly AsyncRateLimitPolicy<ResultadoContador> _resiliencePolicy;

    public Worker(ILogger<Worker> logger,
        IConfiguration configuration,
        AsyncRateLimitPolicy<ResultadoContador> resiliencePolicy)
    {
        _logger = logger;
        _configuration = configuration;
        _resiliencePolicy = resiliencePolicy;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var httpClient = new HttpClient();
        var urlApiContagem = _configuration["UrlApiContagem"];

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var resultado = await _resiliencePolicy.ExecuteAsync(() =>
                {
                    return httpClient
                        .GetFromJsonAsync<ResultadoContador>(urlApiContagem)!;
                });

                _logger.LogInformation($"* {DateTime.Now:HH:mm:ss} * " +
                    $"Contador = {resultado.ValorAtual} | " +
                    $"Mensagem = {resultado.Mensagem}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"# {DateTime.Now:HH:mm:ss} # " +
                    $"Falha ao invocar a API: {ex.GetType().FullName} | {ex.Message}");
            }

            await Task.Delay(800 + new Random().Next(0, 450), stoppingToken);
        }
    }
}