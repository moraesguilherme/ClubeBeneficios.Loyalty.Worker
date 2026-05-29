namespace ClubeBeneficios.Loyalty.Worker.Services;

public sealed class LoyaltyProcessingResult
{
    public string RunType { get; set; } = string.Empty;
    public int EtlProcessedRows { get; set; }
    public int EtlCreatedEvents { get; set; }
    public int EtlIgnoredRows { get; set; }
    public int EtlFailedRows { get; set; }
    public int MetricsProcessedClients { get; set; }
    public int ReclassificationProcessedClients { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
}
