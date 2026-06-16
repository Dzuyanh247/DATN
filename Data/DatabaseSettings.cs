namespace Datn.PcStore.Data;

public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    public string PrimaryConnectionName { get; set; } = "SqlExpressConnection";

    public string FallbackConnectionName { get; set; } = "LocalDbConnection";

    public int ConnectionTestTimeoutSeconds { get; set; } = 2;
}
