using System;
using System.Text.Json.Serialization;

namespace SuperPOS.Client.Views.Ai;

/// <summary>Entrada persistida: análisis (compra/venc/ventas) o turno de consulta libre.</summary>
public sealed class IaBitacoraItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public string Tipo { get; set; } = "";
    public int? DiasAnalisis { get; set; }
    public string? InstruccionOUsuario { get; set; }
    public string TextoIa { get; set; } = "";

    /// <summary>Consulta libre: id de la conversación para filtrar / trazabilidad.</summary>
    public string? IdConversacion { get; set; }

    [JsonIgnore]
    public string LineaEncabezado => Tipo switch
    {
        "compra"      => $"🛒 Compra{FmtDias()} · {Local:dd/MM/yyyy HH:mm}",
        "vencimiento" => $"⏰ Vencimientos{FmtDias()} · {Local:dd/MM/yyyy HH:mm}",
        "ventas"      => $"📈 Ventas{FmtDias()} · {Local:dd/MM/yyyy HH:mm}",
        "consulta"    => $"💬 Consulta libre · {Local:dd/MM/yyyy HH:mm}",
        _             => $"📌 {Tipo} · {Local:dd/MM/yyyy HH:mm}"
    };

    [JsonIgnore] public DateTime Local => Utc == default ? DateTime.Now : Utc.ToLocalTime();

    private string FmtDias()
    {
        if (DiasAnalisis is not { } d || d <= 0) return "";
        return $" · últ. {d} días";
    }
}
