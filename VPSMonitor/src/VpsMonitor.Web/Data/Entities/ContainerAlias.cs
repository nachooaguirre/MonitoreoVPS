namespace VpsMonitor.Web.Data.Entities;

public sealed class ContainerAlias
{
    public Guid Id { get; set; }
    public string ContainerIdOrName { get; set; } = "";
    public string Alias { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}
