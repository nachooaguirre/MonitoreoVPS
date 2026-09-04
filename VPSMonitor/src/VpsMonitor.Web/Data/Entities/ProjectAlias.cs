namespace VpsMonitor.Web.Data.Entities;

public sealed class ProjectAlias
{
    public Guid Id { get; set; }
    public string ProjectKey { get; set; } = "";
    public string Alias { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}
