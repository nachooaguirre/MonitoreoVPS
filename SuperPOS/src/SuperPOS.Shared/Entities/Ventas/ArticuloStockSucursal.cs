namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Stock físico de un artículo en una sucursal (depósito central o local).
/// </summary>
public class ArticuloStockSucursal
{
    public int Id { get; set; }
    public int IdArticulo { get; set; }
    public int IdSucursal { get; set; }
    public decimal Cantidad { get; set; }
    /// <summary>Mínimo de stock total (depósito + góndola) para este local antes de alertar reposición.</summary>
    public decimal StockMinimo { get; set; }
    /// <summary>Mínimo que debe haber exhibido en góndola/salón (subconjunto de Cantidad).</summary>
    public decimal StockMinimoGondola { get; set; }

    public Articulo? Articulo { get; set; }
    public Sucursal? Sucursal { get; set; }
}
