namespace VpsMonitor.Web.Infrastructure.Docker;

public sealed record DockerContainerInfo(
    string Id,
    string Name,
    string Image,
    IReadOnlyDictionary<string, string> Labels,
    string State,
    string Status,
    long Created,
    int RestartCount,
    string ProjectKey,
    string DisplayName = ""
);

public sealed record DockerContainerStats(
    string ContainerId,
    double CpuPercent,
    double MemoryUsageMb,
    double MemoryLimitMb,
    long NetworkRxBytes,
    long NetworkTxBytes,
    long BlockReadBytes,
    long BlockWriteBytes
);

public sealed record ProjectSummary(
    string ProjectKey,
    string DisplayName,
    int ContainerCount,
    IReadOnlyList<DockerContainerInfo> Containers,
    int TotalRestarts,
    string OverallStatus,
    string AssignmentSource
);
