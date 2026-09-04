namespace VpsMonitor.Web.Infrastructure.Prometheus;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

public sealed record VpsMetrics(
    double CpuPercent,
    double MemoryPercent,
    double DiskPercent,
    double NetworkBps,
    double UptimeSeconds
);

public sealed record PrometheusAlertInfo(
    string AlertName,
    string Severity,
    string State,
    string Summary,
    string Description,
    IReadOnlyDictionary<string, string> Labels
);

public interface IPrometheusQueryClient
{
    Task<VpsMetrics?> GetVpsMetricsAsync(CancellationToken ct = default);
    Task<double?> QueryScalarAsync(string query, CancellationToken ct = default);
    Task<List<PrometheusAlertInfo>> GetActiveAlertsAsync(CancellationToken ct = default);
}

public sealed class PrometheusQueryClient : IPrometheusQueryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PrometheusQueryClient> _logger;

    public PrometheusQueryClient(HttpClient httpClient, ILogger<PrometheusQueryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<VpsMetrics?> GetVpsMetricsAsync(CancellationToken ct = default)
    {
        try
        {
            var cpuTask = QueryScalarAsync("100 - (avg(rate(node_cpu_seconds_total{mode=\"idle\"}[1m])) * 100)", ct);
            var memTask = QueryScalarAsync("(sum(node_memory_MemTotal_bytes) - sum(node_memory_MemAvailable_bytes)) / sum(node_memory_MemTotal_bytes) * 100", ct);
            var diskTask = QueryScalarAsync("100 - (sum(node_filesystem_avail_bytes{fstype!~\"tmpfs|overlay|fuse.lxcfs|squashfs\"}) / sum(node_filesystem_size_bytes{fstype!~\"tmpfs|overlay|fuse.lxcfs|squashfs\"}) * 100)", ct);
            var netTask = QueryScalarAsync("sum(rate(node_network_receive_bytes_total[1m])) + sum(rate(node_network_transmit_bytes_total[1m]))", ct);
            var uptimeTask = QueryScalarAsync("node_time_seconds - node_boot_time_seconds", ct);

            await Task.WhenAll(cpuTask, memTask, diskTask, netTask, uptimeTask);

            var cpu = await cpuTask;
            if (cpu is null)
            {
                cpu = await QueryScalarAsync("100 - (avg(irate(node_cpu_seconds_total{mode=\"idle\"}[1m])) * 100)", ct);
            }

            var mem = await memTask;
            var disk = await diskTask;
            var net = await netTask;
            var uptime = await uptimeTask;

            if (cpu is null && mem is null && disk is null && net is null && uptime is null)
            {
                return null;
            }

            return new VpsMetrics(
                CpuPercent: Math.Round(Math.Max(0.0, cpu ?? 0.0), 2),
                MemoryPercent: Math.Round(Math.Max(0.0, mem ?? 0.0), 2),
                DiskPercent: Math.Round(Math.Max(0.0, disk ?? 0.0), 2),
                NetworkBps: Math.Round(Math.Max(0.0, net ?? 0.0), 2),
                UptimeSeconds: Math.Round(Math.Max(0.0, uptime ?? 0.0), 2)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to assemble VPS metrics from Prometheus.");
            return null;
        }
    }

    public async Task<double?> QueryScalarAsync(string query, CancellationToken ct = default)
    {
        try
        {
            var requestUrl = $"api/v1/query?query={Uri.EscapeDataString(query)}";
            using var response = await _httpClient.GetAsync(requestUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return ExtractScalarValue(doc.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Prometheus with query: {Query}", query);
            return null;
        }
    }

    public async Task<List<PrometheusAlertInfo>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/v1/alerts", ct);
            if (!response.IsSuccessStatusCode)
            {
                return new List<PrometheusAlertInfo>();
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;

            var alerts = new List<PrometheusAlertInfo>();

            if (root.TryGetProperty("status", out var statusProp) &&
                statusProp.GetString() == "success" &&
                root.TryGetProperty("data", out var dataProp) &&
                dataProp.TryGetProperty("alerts", out var alertsArr) &&
                alertsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in alertsArr.EnumerateArray())
                {
                    var state = item.TryGetProperty("state", out var stateProp) ? stateProp.GetString() ?? "" : "";

                    var labelsDict = new Dictionary<string, string>();
                    if (item.TryGetProperty("labels", out var labelsProp) && labelsProp.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in labelsProp.EnumerateObject())
                        {
                            labelsDict[p.Name] = p.Value.GetString() ?? "";
                        }
                    }

                    var alertName = labelsDict.TryGetValue("alertname", out var an) ? an : "";
                    var severity = labelsDict.TryGetValue("severity", out var sev) ? sev : "warning";

                    var summary = "";
                    var description = "";
                    if (item.TryGetProperty("annotations", out var annProp) && annProp.ValueKind == JsonValueKind.Object)
                    {
                        if (annProp.TryGetProperty("summary", out var s)) summary = s.GetString() ?? "";
                        if (annProp.TryGetProperty("description", out var d)) description = d.GetString() ?? "";
                    }

                    alerts.Add(new PrometheusAlertInfo(
                        AlertName: alertName,
                        Severity: severity,
                        State: state,
                        Summary: summary,
                        Description: description,
                        Labels: labelsDict
                    ));
                }
            }

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active alerts from Prometheus.");
            return new List<PrometheusAlertInfo>();
        }
    }

    private static double? ExtractScalarValue(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("status", out var statusProp) &&
                statusProp.GetString() == "success" &&
                root.TryGetProperty("data", out var dataProp) &&
                dataProp.TryGetProperty("result", out var resultProp) &&
                resultProp.ValueKind == JsonValueKind.Array &&
                resultProp.GetArrayLength() > 0)
            {
                var firstResult = resultProp[0];
                if (firstResult.TryGetProperty("value", out var valueProp) &&
                    valueProp.ValueKind == JsonValueKind.Array &&
                    valueProp.GetArrayLength() >= 2)
                {
                    var valStr = valueProp[1].GetString();
                    if (double.TryParse(valStr, CultureInfo.InvariantCulture, out var parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        catch
        {
            // ignore parse exceptions
        }

        return null;
    }
}
