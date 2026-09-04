namespace VpsMonitor.Web.Tests.Infrastructure;

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VpsMonitor.Web.Infrastructure.Ai;
using VpsMonitor.Web.Infrastructure.Docker;
using Xunit;

public class AiProjectPlannerServiceTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task PlanTaskFromProposalAsync_ParsesJsonFromAiResponse()
    {
        // Arrange
        var aiJsonResponse = @"{
          ""choices"": [
            {
              ""message"": {
                ""content"": ""{\""ProjectKey\"": \""superpos\"", \""Title\"": \""Agregar Factura Electrónica\"", \""Description\"": \""Integración con web service de AFIP\"", \""Priority\"": \""High\"", \""ActionPlanSteps\"": [\""Crear cliente SOAP AFIP\"", \""Agregar modelo de Comprobante\"", \""Probar en Homologación\""]}""
              }
            }
          ]
        }";

        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(aiJsonResponse)
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Ai:Enabled", "true" },
            { "Ai:Model", "deepseek-ai/deepseek-r1" }
        }).Build();

        var service = new AiProjectPlannerService(httpClient, config, NullLogger<AiProjectPlannerService>.Instance);
        var projects = new List<ProjectSummary>
        {
            new("superpos", "SuperPOS", 2, new List<DockerContainerInfo>(), 0, "healthy", "coolify")
        };

        // Act
        var result = await service.PlanTaskFromProposalAsync("El cliente de superpos quiere facturación AFIP", projects);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("superpos", result.ProjectKey);
        Assert.Equal("Agregar Factura Electrónica", result.Title);
        Assert.Equal("High", result.Priority);
        Assert.Equal(3, result.ActionPlanSteps.Count);
    }

    [Fact]
    public async Task PlanTaskFromProposalAsync_FallbackWhenAiFails()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Ai:Enabled", "true" }
        }).Build();

        var service = new AiProjectPlannerService(httpClient, config, NullLogger<AiProjectPlannerService>.Instance);
        var projects = new List<ProjectSummary>
        {
            new("tiendaveloo", "TiendaVeloo", 1, new List<DockerContainerInfo>(), 0, "healthy", "coolify")
        };

        // Act
        var result = await service.PlanTaskFromProposalAsync("Requerimiento para tiendaveloo", projects);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tiendaveloo", result.ProjectKey);
        Assert.Contains("tiendaveloo", result.Title, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.ActionPlanSteps);
    }
}
