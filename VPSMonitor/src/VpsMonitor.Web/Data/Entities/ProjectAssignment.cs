namespace VpsMonitor.Web.Data.Entities;

public sealed class ProjectAssignment
{
    public Guid Id { get; set; }
    public string ProjectKey { get; set; } = "";
    public Guid MonitorUserId { get; set; }
    public MonitorUser? MonitorUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
