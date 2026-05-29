using ClubeBeneficios.Loyalty.Worker.Configuration;
using ClubeBeneficios.Loyalty.Worker.Services;
using Microsoft.Extensions.Options;

namespace ClubeBeneficios.Loyalty.Worker.Workers;

public sealed class LoyaltyProcessingBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<LoyaltyWorkerOptions> _options;
    private readonly ILogger<LoyaltyProcessingBackgroundWorker> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public LoyaltyProcessingBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<LoyaltyWorkerOptions> options,
        ILogger<LoyaltyProcessingBackgroundWorker> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogWarning("Loyalty Worker desabilitado por configuracao.");
            return;
        }

        if (options.RunOnStartup)
        {
            await ExecuteProcessingAsync(stoppingToken);
        }

        if (string.Equals(options.Mode, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Modo Manual finalizado.");
            _applicationLifetime.StopApplication();
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            options = _options.CurrentValue;

            var delaySeconds = Math.Max(15, options.PollingIntervalSeconds);

            await Task.Delay(
                TimeSpan.FromSeconds(delaySeconds),
                stoppingToken);

            if (!options.Enabled)
            {
                _logger.LogDebug("Execucao ignorada. Worker desabilitado.");
                continue;
            }

            await ExecuteProcessingAsync(stoppingToken);
        }
    }

    private async Task ExecuteProcessingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var processingService =
            scope.ServiceProvider.GetRequiredService<ILoyaltyProcessingService>();

        try
        {
            await processingService.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Execucao cancelada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha no processamento automatico de fidelidade.");
        }
    }
}