using System;

namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class Auditoria
{
    public long Id { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Hora { get; set; }
    public string? TipoCbte { get; set; }
    public int? NroCbte { get; set; }
    public string? Tipo { get; set; }
    public string? Codigo { get; set; } // Representa el EAN del artículo
    public decimal? Cantidad { get; set; }
    public decimal? Importe { get; set; }
    public int? Acumulador { get; set; }
    public bool EsEnvase { get; set; }
    public int? Zeta { get; set; }
    public int? CodigoInterno { get; set; }
    public int? ProcesoStock { get; set; }
    public int? Cliente { get; set; }
    public int? Cajero { get; set; }
}
