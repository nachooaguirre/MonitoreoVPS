namespace VpsMonitor.Web.Tests.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VpsMonitor.Web.Infrastructure.Notifications;
using Xunit;

public class EmailNotificationServiceTests
{
    [Fact]
    public async Task SendAlertNotificationAsync_DoesNotThrowWhenSmtpDisabled()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Smtp:Enabled", "false" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var service = new EmailNotificationService(config, NullLogger<EmailNotificationService>.Instance);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.SendAlertNotificationAsync("Test Subject", "Test Body", "admin@vpsmonitor.local"));
        Assert.Null(exception);
    }
}
