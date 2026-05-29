using ClubeBeneficios.Loyalty.Worker.Configuration;
using ClubeBeneficios.Loyalty.Worker.Infrastructure.Database;
using Microsoft.Extensions.Options;

namespace ClubeBeneficios.Loyalty.Worker.Services;

public interface ILoyaltyProcessingService
{
    Task<LoyaltyProcessingResult> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class LoyaltyProcessingService : ILoyaltyProcessingService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILoyaltyProcessingRepository _repository;
    private readonly IOptionsMonitor<LoyaltyWorkerOptions> _options;
    private readonly ILogger<LoyaltyProcessingService> _logger;

    public LoyaltyProcessingService(
        IDbConnectionFactory connectionFactory,
        ILoyaltyProcessingRepository repository,
        IOptionsMonitor<LoyaltyWorkerOptions> options,
        ILogger<LoyaltyProcessingService> logger)
    {
        _connectionFactory = connectionFactory;
        _repository = repository;
        _options = options;
        _logger = logger;
    }

    public async Task<LoyaltyProcessingResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var startedAt = DateTime.UtcNow;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var lockAcquired = false;

        try
        {
            var lockResult = await _repository.TryAcquireApplicationLockAsync(
                connection,
                options.SqlApplicationLockName,
                timeoutMilliseconds: 0,
                cancellationToken);

            if (lockResult < 0)
            {
                _logger.LogInformation(
                    "Processamento ignorado. Outro worker esta executando. LockResult={LockResult}",
                    lockResult);

                return new LoyaltyProcessingResult
                {
                    RunType = options.RunType,
                    StartedAt = startedAt,
                    FinishedAt = DateTime.UtcNow
                };
            }

            lockAcquired = true;

            _logger.LogInformation(
                "Iniciando processamento de fidelidade. RunType={RunType}, BatchSize={BatchSize}",
                options.RunType,
                options.BatchSize);

            var etlResult = await _repository.ProcessPendingEtlRowsAsync(
                connection,
                options.BatchSize,
                options.RunType,
                options.CommandTimeoutSeconds,
                cancellationToken);

            await _repository.RebuildAllBalancesAsync(
                connection,
                options.CommandTimeoutSeconds,
                cancellationToken);

            var metricsResult = await _repository.RebuildMetricsBatchAsync(
                connection,
                options.CommandTimeoutSeconds,
                cancellationToken);

            var reclassificationResult = await _repository.ReclassifyBatchAsync(
                connection,
                options.CommandTimeoutSeconds,
                cancellationToken);

            if (options.EnablePointsExpiration)
            {
                await _repository.ExpirePointsAsync(
                    connection,
                    DateTime.UtcNow,
                    options.CommandTimeoutSeconds,
                    cancellationToken);
            }

            var result = new LoyaltyProcessingResult
            {
                RunType = options.RunType,
                EtlProcessedRows = GetInt(etlResult, "processed_rows"),
                EtlCreatedEvents = GetInt(etlResult, "created_events"),
                EtlIgnoredRows = GetInt(etlResult, "ignored_rows"),
                EtlFailedRows = GetInt(etlResult, "failed_rows"),
                MetricsProcessedClients = GetInt(metricsResult, "processed_clients"),
                ReclassificationProcessedClients = GetInt(reclassificationResult, "processed_clients"),
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Processamento de fidelidade finalizado. EtlProcessedRows={EtlProcessedRows}, EtlCreatedEvents={EtlCreatedEvents}, EtlIgnoredRows={EtlIgnoredRows}, EtlFailedRows={EtlFailedRows}, MetricsProcessedClients={MetricsProcessedClients}, ReclassificationProcessedClients={ReclassificationProcessedClients}",
                result.EtlProcessedRows,
                result.EtlCreatedEvents,
                result.EtlIgnoredRows,
                result.EtlFailedRows,
                result.MetricsProcessedClients,
                result.ReclassificationProcessedClients);

            return result;
        }
        finally
        {
            if (lockAcquired)
            {
                await _repository.ReleaseApplicationLockAsync(
                    connection,
                    options.SqlApplicationLockName,
                    cancellationToken);
            }
        }
    }

    private static int GetInt(dynamic? row, string propertyName)
    {
        if (row is null)
            return 0;

        var dictionary = row as IDictionary<string, object>;

        if (dictionary is null || !dictionary.TryGetValue(propertyName, out var value) || value is null)
            return 0;

        return Convert.ToInt32(value);
    }
}