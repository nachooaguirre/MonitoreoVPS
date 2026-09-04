namespace VpsMonitor.Web.Data.Entities;

public sealed class TelegramConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public bool IsAlertsEnabled { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
