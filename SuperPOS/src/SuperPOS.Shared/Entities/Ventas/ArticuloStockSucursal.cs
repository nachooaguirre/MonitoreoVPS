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

    public Articulo? Articulo { get; set; }
    public Sucursal? Sucursal { get; set; }
}
