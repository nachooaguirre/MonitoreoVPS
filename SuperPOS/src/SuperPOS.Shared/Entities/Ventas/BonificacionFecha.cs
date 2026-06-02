using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class BonificacionFecha
{
    public int Id { get; set; }
    public int IdArticulo { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public decimal Porcentaje { get; set; }
    public bool Aplicado { get; set; } = true;

    // Navigation Property
    public Articulo? Articulo { get; set; }
}
