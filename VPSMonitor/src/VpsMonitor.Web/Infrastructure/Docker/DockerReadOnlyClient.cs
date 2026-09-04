namespace VpsMonitor.Web.Infrastructure.Docker;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public interface IDockerReadOnlyClient
{
    Task<List<DockerContainerInfo>> ListContainersAsync(CancellationToken ct = default);
    Task<DockerContainerStats?> GetContainerStatsAsync(string containerId, CancellationToken ct = default);
}

public sealed class DockerReadOnlyClient : IDockerReadOnlyClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DockerReadOnlyClient> _logger;

    public DockerReadOnlyClient(HttpClient httpClient, ILogger<DockerReadOnlyClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DockerContainerInfo>> ListContainersAsync(CancellationToken ct = default)
    {
        try
        {
            var rawList = await _httpClient.GetFromJsonAsync<List<DockerContainerRawJson>>("containers/json?all=true", ct);
            if (rawList is null) return new List<DockerContainerInfo>();

            var results = new List<DockerContainerInfo>();
            foreach (var item in rawList)
            {
                var name = item.Names?.FirstOrDefault()?.TrimStart('/') ?? item.Id[..Math.Min(12, item.Id.Length)];
                var labels = item.Labels ?? new Dictionary<string, string>();
                
                string projectKey = "unassigned";
                if (labels.TryGetValue("coolify.projectId", out var coolifyId) && !string.IsNullOrWhiteSpace(coolifyId))
                {
                    projectKey = coolifyId;
                }
                else if (labels.TryGetValue("com.docker.compose.project", out var composeProj) && !string.IsNullOrWhiteSpace(composeProj))
                {
                    projectKey = composeProj;
                }

                results.Add(new DockerContainerInfo(
                    Id: item.Id,
                    Name: name,
                    Image: item.Image ?? "",
                    Labels: labels,
                    State: item.State ?? "unknown",
                    Status: item.Status ?? "",
                    Created: item.Created,
                    RestartCount: 0,
                    ProjectKey: projectKey
                ));
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch container list from Docker proxy.");
            return new List<DockerContainerInfo>();
        }
    }

    public async Task<DockerContainerStats?> GetContainerStatsAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"containers/{containerId}/stats?stream=false", ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;

            double cpuPercent = CalculateCpuPercent(root);
            
            long memoryUsage = 0;
            long memoryLimit = 0;
            if (root.TryGetProperty("memory_stats", out var memStats))
            {
                if (memStats.TryGetProperty("usage", out var usageProp)) memoryUsage = usageProp.GetInt64();
                if (memStats.TryGetProperty("limit", out var limitProp)) memoryLimit = limitProp.GetInt64();
            }

            long rxBytes = 0, txBytes = 0;
            if (root.TryGetProperty("networks", out var networks))
            {
                foreach (var netProp in networks.EnumerateObject())
                {
                    if (netProp.Value.TryGetProperty("rx_bytes", out var rx)) rxBytes += rx.GetInt64();
                    if (netProp.Value.TryGetProperty("tx_bytes", out var tx)) txBytes += tx.GetInt64();
                }
            }

            long readBytes = 0, writeBytes = 0;
            if (root.TryGetProperty("blkio_stats", out var blkio) && 
                blkio.TryGetProperty("io_service_bytes_recursive", out var ioArr) && 
                ioArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in ioArr.EnumerateArray())
                {
                    var op = item.TryGetProperty("op", out var opProp) ? opProp.GetString() : "";
                    var val = item.TryGetProperty("value", out var valProp) ? valProp.GetInt64() : 0;
                    if (string.Equals(op, "read", StringComparison.OrdinalIgnoreCase)) readBytes += val;
                    if (string.Equals(op, "write", StringComparison.OrdinalIgnoreCase)) writeBytes += val;
                }
            }

            return new DockerContainerStats(
                ContainerId: containerId,
                CpuPercent: Math.Round(cpuPercent, 2),
                MemoryUsageMb: Math.Round(memoryUsage / 1024.0 / 1024.0, 2),
                MemoryLimitMb: Math.Round(memoryLimit / 1024.0 / 1024.0, 2),
                NetworkRxBytes: rxBytes,
                NetworkTxBytes: txBytes,
                BlockReadBytes: readBytes,
                BlockWriteBytes: writeBytes
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get stats for container {ContainerId}", containerId);
            return null;
        }
    }

    private static double CalculateCpuPercent(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("cpu_stats", out var cpuStats) ||
                !root.TryGetProperty("precpu_stats", out var preCpuStats))
            {
                return 0.0;
            }

            if (!cpuStats.TryGetProperty("cpu_usage", out var cpuUsage) ||
                !preCpuStats.TryGetProperty("cpu_usage", out var preCpuUsage))
            {
                return 0.0;
            }

            long totalUsage = cpuUsage.TryGetProperty("total_usage", out var tu) ? tu.GetInt64() : 0;
            long preTotalUsage = preCpuUsage.TryGetProperty("total_usage", out var ptu) ? ptu.GetInt64() : 0;

            long systemCpuUsage = cpuStats.TryGetProperty("system_cpu_usage", out var scu) ? scu.GetInt64() : 0;
            long preSystemCpuUsage = preCpuStats.TryGetProperty("system_cpu_usage", out var pscu) ? pscu.GetInt64() : 0;

            long cpuDelta = totalUsage - preTotalUsage;
            long systemDelta = systemCpuUsage - preSystemCpuUsage;

            int cpus = 1;
            if (cpuStats.TryGetProperty("online_cpus", out var onlineCpus) && onlineCpus.GetInt32() > 0)
            {
                cpus = onlineCpus.GetInt32();
            }
            else if (cpuUsage.TryGetProperty("percpu_usage", out var perCpu) && perCpu.ValueKind == JsonValueKind.Array)
            {
                cpus = Math.Max(1, perCpu.GetArrayLength());
            }

            if (systemDelta > 0 && cpuDelta > 0)
            {
                return ((double)cpuDelta / systemDelta) * cpus * 100.0;
            }
            return 0.0;
        }
        catch
        {
            return 0.0;
        }
    }

    private sealed record DockerContainerRawJson(
        [property: JsonPropertyName("Id")] string Id,
        [property: JsonPropertyName("Names")] List<string>? Names,
        [property: JsonPropertyName("Image")] string? Image,
        [property: JsonPropertyName("State")] string? State,
        [property: JsonPropertyName("Status")] string? Status,
        [property: JsonPropertyName("Created")] long Created,
        [property: JsonPropertyName("Labels")] Dictionary<string, string>? Labels
    );
}
