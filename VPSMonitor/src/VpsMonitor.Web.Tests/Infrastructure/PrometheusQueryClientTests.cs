namespace VpsMonitor.Web.Tests.Infrastructure;

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using VpsMonitor.Web.Infrastructure.Prometheus;
using Xunit;

public class PrometheusQueryClientTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
            : this(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            })
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    [Fact]
    public async Task QueryScalarAsync_ParsesPrometheusInstantQueryResponseCorrectly()
    {
        // Arrange
        var json = @"{
            ""status"": ""success"",
            ""data"": {
                ""resultType"": ""vector"",
                ""result"": [
                    {
                        ""metric"": { ""instance"": ""vps"" },
                        ""value"": [ 1725436559.123, ""45.2"" ]
                    }
                ]
            }
        }";

        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://prometheus:9090/") };
        var client = new PrometheusQueryClient(httpClient, NullLogger<PrometheusQueryClient>.Instance);

        // Act
        var result = await client.QueryScalarAsync("100 - (avg(rate(node_cpu_seconds_total{mode=\"idle\"}[5m])) * 100)");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(45.2, result.Value);
    }

    [Fact]
    public async Task GetVpsMetricsAsync_CalculatesAllMetricsCorrectly()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(req =>
        {
            var query = req.RequestUri?.Query ?? "";
            string val = "0.0";
            if (query.Contains("node_cpu")) val = "42.5";
            else if (query.Contains("node_memory")) val = "65.0";
            else if (query.Contains("node_filesystem")) val = "30.0";
            else if (query.Contains("node_network")) val = "1500.0";
            else if (query.Contains("node_time")) val = "3600.0";

            var json = $@"{{
                ""status"": ""success"",
                ""data"": {{
                    ""resultType"": ""vector"",
                    ""result"": [
                        {{
                            ""metric"": {{}},
                            ""value"": [ 1725436559.123, ""{val}"" ]
                        }}
                    ]
                }}
            }}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://prometheus:9090/") };
        var client = new PrometheusQueryClient(httpClient, NullLogger<PrometheusQueryClient>.Instance);

        // Act
        var metrics = await client.GetVpsMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(42.5, metrics.CpuPercent);
        Assert.Equal(65.0, metrics.MemoryPercent);
        Assert.Equal(30.0, metrics.DiskPercent);
        Assert.Equal(1500.0, metrics.NetworkBps);
        Assert.Equal(3600.0, metrics.UptimeSeconds);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ParsesActiveAlertsCorrectly()
    {
        // Arrange
        var json = @"{
            ""status"": ""success"",
            ""data"": {
                ""alerts"": [
                    {
                        ""labels"": {
                            ""alertname"": ""HighCpuUsage"",
                            ""severity"": ""warning"",
                            ""instance"": ""vps""
                        },
                        ""annotations"": {
                            ""summary"": ""High CPU Usage"",
                            ""description"": ""CPU is > 85%""
                        },
                        ""state"": ""firing""
                    }
                ]
            }
        }";

        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://prometheus:9090/") };
        var client = new PrometheusQueryClient(httpClient, NullLogger<PrometheusQueryClient>.Instance);

        // Act
        var alerts = await client.GetActiveAlertsAsync();

        // Assert
        Assert.Single(alerts);
        var alert = alerts[0];
        Assert.Equal("HighCpuUsage", alert.AlertName);
        Assert.Equal("warning", alert.Severity);
        Assert.Equal("firing", alert.State);
        Assert.Equal("High CPU Usage", alert.Summary);
        Assert.Equal("CPU is > 85%", alert.Description);
    }

    [Fact]
    public async Task GetVpsMetricsAsync_HandlesHttpErrorGracefully()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://prometheus:9090/") };
        var client = new PrometheusQueryClient(httpClient, NullLogger<PrometheusQueryClient>.Instance);

        // Act
        var metrics = await client.GetVpsMetricsAsync();

        // Assert
        Assert.Null(metrics);
    }

    [Fact]
    public async Task QueryScalarAsync_HandlesEmptyOrInvalidResultGracefully()
    {
        // Arrange
        var json = @"{
            ""status"": ""success"",
            ""data"": {
                ""resultType"": ""vector"",
                ""result"": []
            }
        }";

        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://prometheus:9090/") };
        var client = new PrometheusQueryClient(httpClient, NullLogger<PrometheusQueryClient>.Instance);

        // Act
        var result = await client.QueryScalarAsync("some_query");

        // Assert
        Assert.Null(result);
    }
}
