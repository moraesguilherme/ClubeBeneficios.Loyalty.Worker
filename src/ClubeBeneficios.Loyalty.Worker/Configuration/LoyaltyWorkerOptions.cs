namespace ClubeBeneficios.Loyalty.Worker.Configuration;

public sealed class LoyaltyWorkerOptions
{
    public const string SectionName = "LoyaltyWorker";

    public bool Enabled { get; set; } = true;
    public string Mode { get; set; } = "Watch";
    public int PollingIntervalSeconds { get; set; } = 300;
    public int BatchSize { get; set; } = 500;
    public bool RunOnStartup { get; set; } = false;
    public string RunType { get; set; } = "scheduled";
    public int CommandTimeoutSeconds { get; set; } = 180;
    public string SqlApplicationLockName { get; set; } = "clube-beneficios-loyalty-processing";
    public bool EnablePointsExpiration { get; set; } = false;
}
