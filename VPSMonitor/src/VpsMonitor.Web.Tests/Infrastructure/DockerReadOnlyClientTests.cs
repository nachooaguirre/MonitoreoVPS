namespace VpsMonitor.Web.Tests.Infrastructure;

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using VpsMonitor.Web.Infrastructure.Docker;
using Xunit;

public class DockerReadOnlyClientTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task ListContainersAsync_ParsesJsonCorrectly()
    {
        // Arrange
        var json = @"[
            {
                ""Id"": ""1234567890ab"",
                ""Names"": [""/superpos-web""],
                ""Image"": ""superpos:latest"",
                ""State"": ""running"",
                ""Status"": ""Up 2 hours"",
                ""Created"": 1700000000,
                ""Labels"": {
                    ""coolify.projectId"": ""superpos-prod""
                }
            }
        ]";

        var handler = new FakeHttpMessageHandler(json);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://docker-proxy:2375/") };
        var dockerClient = new DockerReadOnlyClient(client, NullLogger<DockerReadOnlyClient>.Instance);

        // Act
        var result = await dockerClient.ListContainersAsync();

        // Assert
        Assert.Single(result);
        var container = result[0];
        Assert.Equal("1234567890ab", container.Id);
        Assert.Equal("superpos-web", container.Name);
        Assert.Equal("superpos-prod", container.ProjectKey);
        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
    }

    [Fact]
    public async Task ListContainersAsync_HandlesHttpErrorGracefully()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://docker-proxy:2375/") };
        var dockerClient = new DockerReadOnlyClient(client, NullLogger<DockerReadOnlyClient>.Instance);

        // Act
        var result = await dockerClient.ListContainersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
