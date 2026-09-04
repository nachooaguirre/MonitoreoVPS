namespace VpsMonitor.Web.Data.Entities;

public sealed class MonitorSession
{
    public Guid Id { get; set; }
    public Guid MonitorUserId { get; set; }
    public MonitorUser? MonitorUser { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
