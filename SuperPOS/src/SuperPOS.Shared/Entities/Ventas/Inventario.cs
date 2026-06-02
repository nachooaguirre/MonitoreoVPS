namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Inventario físico / Toma de datos (equivalente a Inventarios en Tecnolar)
/// </summary>
public class Inventario
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    /// <summary>Sucursal donde se toma el stock de sistema y donde se aplica el ajuste al cerrar.</summary>
    public int IdSucursal { get; set; } = 1;
    public int IdUsuario { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public EstadoInventario Estado { get; set; } = EstadoInventario.EnProceso;
    public int TotalArticulos { get; set; }
    public int ArticulosContados { get; set; }
    public decimal DiferenciaValorizada { get; set; }
    public string? Observaciones { get; set; }

    public Sucursal? Sucursal { get; set; }
    public ICollection<InventarioDetalle> Detalles { get; set; } = [];
}

public class InventarioDetalle
{
    public int Id { get; set; }
    public int IdInventario { get; set; }
    public int IdArticulo { get; set; }
    public decimal StockSistema { get; set; }    // Stock que tenía el sistema
    public decimal StockContado { get; set; }    // Lo que físicamente se contó
    /// <summary>Indica si se registró al menos un conteo (incluido 0 en estantería vacía).</summary>
    public bool FueConteado { get; set; }
    public decimal Diferencia => StockContado - StockSistema;
    public decimal PrecioCosto { get; set; }
    public decimal DiferenciaValorizada => Diferencia * PrecioCosto;
    public DateTime FechaConteo { get; set; } = DateTime.UtcNow;
    public string? Observaciones { get; set; }

    public Articulo? Articulo { get; set; }
    public Inventario? Inventario { get; set; }
}

public enum EstadoInventario
{
    EnProceso = 0,
    Cerrado = 1,
    Aplicado = 2  // Ya se ajustó el stock en el sistema
}
