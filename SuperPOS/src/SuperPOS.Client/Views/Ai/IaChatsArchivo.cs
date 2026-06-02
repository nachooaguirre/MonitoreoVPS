using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SuperPOS.Client.Services;

namespace SuperPOS.Client.Views.Ai;

/// <summary>Historial de consulta libre: varias conversaciones con memoria aislada por hilo (persiste en la PC del usuario).</summary>
public class IaChatsRoot
{
    public int Version { get; set; } = 1;
    public string? ChatActivoId { get; set; }
    public List<IaConversacionArchivo> Conversaciones { get; set; } = new();
}

public class IaConversacionArchivo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Titulo { get; set; } = "Nueva conversación";
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<AiChatMensajeDto> Mensajes { get; set; } = new();

    [JsonIgnore]
    public DateTime UpdatedLocal => UpdatedUtc != default ? UpdatedUtc.ToLocalTime() : DateTime.Now;
}
