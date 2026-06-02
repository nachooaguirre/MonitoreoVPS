namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Lista de precios de un proveedor (compra) — distinta a la lista de precios al público.
/// Suelen subirse por Excel/PDF/foto; las líneas son editables e incluyen escalas de bonificación.
/// </summary>
public class ListaPrecioProveedor
{
    public int Id { get; set; }
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = "";
    public string? Notas { get; set; }
    public DateTime FechaCargaUtc { get; set; }
    public string? ArchivoOrigenNombre { get; set; }
    public string? ArchivoOrigenRutaRelativa { get; set; }
    public bool Activo { get; set; } = true;

    public Proveedor? Proveedor { get; set; }
    public ICollection<ListaPrecioProveedorLinea> Lineas { get; set; } = [];
}

public class ListaPrecioProveedorLinea
{
    public int Id { get; set; }
    public int IdLista { get; set; }
    public int? IdArticulo { get; set; }
    public string CodigoProveedor { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public decimal? IvaPorcentaje { get; set; }
    /// <summary>
    /// JSON: [{ "cantidadMin": 10, "porcentaje": 5, "nota": "escala 1" }, ...] ordenado por cantidad mínima.
    /// </summary>
    public string BonificacionesJson { get; set; } = "[]";

    public ListaPrecioProveedor? Lista { get; set; }
    public Articulo? Articulo { get; set; }
}
