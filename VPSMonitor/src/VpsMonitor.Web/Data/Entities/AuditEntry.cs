namespace VpsMonitor.Web.Data.Entities;

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string Target { get; set; } = "";
    public string RequestIp { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public bool Success { get; set; }
    public string Detail { get; set; } = "";
}
