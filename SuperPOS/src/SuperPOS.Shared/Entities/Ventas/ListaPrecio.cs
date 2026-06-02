namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Lista de precios (equivalente a ArticulosPreciosListas en Tecnolar)
/// Permite tener precio minorista, mayorista, empleados, etc.
/// </summary>
public class ListaPrecio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;       // "Minorista", "Mayorista", "Empleados"
    public string Descripcion { get; set; } = string.Empty;
    public TipoListaPrecio Tipo { get; set; } = TipoListaPrecio.PorcentajeSobreVenta;
    public decimal Valor { get; set; }      // % de ajuste sobre precio venta base
    public bool EsDefault { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Cliente> Clientes { get; set; } = [];
    public ICollection<ArticuloPrecioLista> PreciosEspeciales { get; set; } = [];
}

public enum TipoListaPrecio
{
    PorcentajeSobreVenta = 0,   // precio base + X%
    PorcentajeSobreCosto = 1,   // costo + X%
    PrecioFijo = 2               // precio absoluto por artículo
}

/// <summary>
/// Precio especial de un artículo en una lista específica
/// </summary>
public class ArticuloPrecioLista
{
    public int Id { get; set; }
    public int IdLista { get; set; }
    public int IdArticulo { get; set; }
    public decimal Precio { get; set; }
    public decimal? PorcentajeAjuste { get; set; }

    public ListaPrecio? Lista { get; set; }
    public Articulo? Articulo { get; set; }
}

/// <summary>
/// Rango de bonificación por cantidad (descuento por volumen)
/// Equivalente a ArticulosPreciosRangos en Tecnolar
/// </summary>
public class BonificacionRango
{
    public int Id { get; set; }
    public int IdArticulo { get; set; }
    public decimal CantidadDesde { get; set; }
    public decimal CantidadHasta { get; set; }
    public decimal PorcentajeDescuento { get; set; }

    public Articulo? Articulo { get; set; }
}
