namespace VpsMonitor.Web.Data.Entities;

public enum MonitorUserRole
{
    Owner,
    Viewer
}

public sealed class MonitorUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public MonitorUserRole Role { get; set; } = MonitorUserRole.Viewer;
    public DateTime CreatedAtUtc { get; set; }
    public List<MonitorSession> Sessions { get; set; } = [];
    public List<ProjectAssignment> ProjectAssignments { get; set; } = [];
}
