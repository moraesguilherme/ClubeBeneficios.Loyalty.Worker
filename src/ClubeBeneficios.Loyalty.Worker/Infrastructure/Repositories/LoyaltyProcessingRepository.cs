using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ClubeBeneficios.Loyalty.Worker.Infrastructure.Repositories;

public sealed class LoyaltyProcessingRepository : ILoyaltyProcessingRepository
{
    public async Task<int> TryAcquireApplicationLockAsync(
        SqlConnection connection,
        string lockName,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                DECLARE @Result int;

                EXEC @Result = sys.sp_getapplock
                    @Resource = @LockName,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Session',
                    @LockTimeout = @TimeoutMilliseconds;

                SELECT @Result;
                """,
                new
                {
                    LockName = lockName,
                    TimeoutMilliseconds = timeoutMilliseconds
                },
                commandType: CommandType.Text,
                cancellationToken: cancellationToken));
    }

    public async Task ReleaseApplicationLockAsync(
        SqlConnection connection,
        string lockName,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                EXEC sys.sp_releaseapplock
                    @Resource = @LockName,
                    @LockOwner = 'Session';
                """,
                new { LockName = lockName },
                commandType: CommandType.Text,
                cancellationToken: cancellationToken));
    }

    public async Task<dynamic?> ProcessPendingEtlRowsAsync(
        SqlConnection connection,
        int batchSize,
        string runType,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync(
            new CommandDefinition(
                "dbo.usp_loyalty_process_pending_etl_rows",
                new
                {
                    BatchSize = batchSize,
                    CreatedByUserId = (Guid?)null,
                    RunType = runType
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    public async Task RebuildAllBalancesAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.usp_customer_loyalty_balance_rebuild_all",
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    public async Task<dynamic?> RebuildMetricsBatchAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync(
            new CommandDefinition(
                "dbo.usp_loyalty_metrics_rebuild_batch",
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    public async Task<dynamic?> ReclassifyBatchAsync(
        SqlConnection connection,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        return await connection.QueryFirstOrDefaultAsync(
            new CommandDefinition(
                "dbo.usp_loyalty_reclassify_batch_by_latest_metrics",
                new
                {
                    CreatedByUserId = (Guid?)null
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    public async Task ExpirePointsAsync(
        SqlConnection connection,
        DateTime asOf,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.usp_customer_loyalty_expire_inactive_points",
                new
                {
                    AsOf = asOf,
                    CreatedByUserId = (Guid?)null
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }
}