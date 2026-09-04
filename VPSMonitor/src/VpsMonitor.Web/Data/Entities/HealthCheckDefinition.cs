namespace VpsMonitor.Web.Data.Entities;

public sealed class HealthCheckDefinition
{
    public Guid Id { get; set; }
    public string ProjectKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Method { get; set; } = "GET";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
