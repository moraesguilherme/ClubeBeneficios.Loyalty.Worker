using Microsoft.Data.SqlClient;

public interface ILoyaltyProcessingRepository
{
    Task<int> TryAcquireApplicationLockAsync(
        SqlConnection connection,
        string lockName,
        int timeoutMilliseconds,
        CancellationToken cancellationToken);

    Task ReleaseApplicationLockAsync(
        SqlConnection connection,
        string lockName,
        CancellationToken cancellationToken);

    Task<dynamic?> ProcessPendingEtlRowsAsync(
        SqlConnection connection,
        int batchSize,
        string runType,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken);

    Task RebuildAllBalancesAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken);

    Task<dynamic?> RebuildMetricsBatchAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken);

    Task<dynamic?> ReclassifyBatchAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken);

    Task ExpirePointsAsync(
        SqlConnection connection,
        DateTime asOf,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken);
}