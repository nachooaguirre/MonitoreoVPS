using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using VpsMonitor.Web;
using Xunit;

namespace VpsMonitor.Web.Tests;

public class HealthSmokeTests
{
    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var builder = VpsMonitorApp.CreateBuilder();
        builder.WebHost.UseTestServer();

        var app = VpsMonitorApp.BuildApp(builder);
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.Ok);
    }

    private sealed record HealthResponse(bool Ok);
}
