using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class CotizacionDetalle
{
    public long Id { get; set; }
    public long IdCotizacion { get; set; }
    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Precio { get; set; }
    public int ItemNro { get; set; }

    public Cotizacion? Cotizacion { get; set; }
    public Articulo? Articulo { get; set; }
}
