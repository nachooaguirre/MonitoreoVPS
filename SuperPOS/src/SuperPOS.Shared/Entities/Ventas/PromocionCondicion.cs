using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class PromocionCondicion
{
    public int Id { get; set; }
    public int IdPromocion { get; set; }
    public Promocion? Promocion { get; set; }

    public string Tipo { get; set; } = "";
    public int? IdArticulo { get; set; }
    public Articulo? Articulo { get; set; }

    public decimal Cantidad { get; set; }
    public decimal Importe { get; set; }
    public int Item { get; set; }

    // Campos de la base de producción real tecnolar.Mdb
    public decimal? ValorDesde { get; set; }
    public decimal? ValorHasta { get; set; }
    public string? TipoValor { get; set; }
    public bool? Excluye { get; set; }
}
