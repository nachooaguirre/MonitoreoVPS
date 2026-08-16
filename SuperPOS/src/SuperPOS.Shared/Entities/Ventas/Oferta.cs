using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class Oferta
{
    public int Id { get; set; }
    public int IdArticulo { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public decimal PrecioOferta { get; set; }
    public decimal? LimiteStock { get; set; }
    public decimal CantidadVendida { get; set; } = 0m;
    public bool Activa { get; set; } = true;

    // Relación
    public Articulo? Articulo { get; set; }
}

public class OfertaGraficaPunto
{
    public string FechaLabel { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Cantidad { get; set; }
}
