using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class PromocionParametroAccion
{
    public int Id { get; set; }
    public int IdPromocion { get; set; }
    public Promocion? Promocion { get; set; }

    public string Tipo { get; set; } = "";
    public int? IdArticulo { get; set; }
    public Articulo? Articulo { get; set; }

    public decimal Cantidad { get; set; }
    public decimal Importe { get; set; }
    public decimal Porcentaje { get; set; }
    public int Item { get; set; }

    // Campos de la base de producción real tecnolar.Mdb (tabla PromocionesAcciones)
    public decimal? Valor { get; set; }
    public string? TipoValor { get; set; }
    public string? AplicaSobre { get; set; }
    public int? Repeticiones { get; set; }
    public bool? PrefiereMenorValor { get; set; }
}
