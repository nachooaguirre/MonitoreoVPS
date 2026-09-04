namespace VpsMonitor.Web.Data.Entities;

public sealed class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProjectKey { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
    public string RawInput { get; set; } = string.Empty;
    public string ActionPlanJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
