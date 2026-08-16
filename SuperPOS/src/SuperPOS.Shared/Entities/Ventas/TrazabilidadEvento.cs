namespace SuperPOS.Shared.Entities.Ventas;

public class TrazabilidadEvento
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; }
    public TipoTrazabilidadEvento Tipo { get; set; }

    /// <summary>
    /// Ubicación libre (ej: "Depósito", "Sala", "Caja 1", "Pasillo 3", "Góndola A2").
    /// </summary>
    public string? Ubicacion { get; set; }

    public int? IdUsuario { get; set; }

    // Referencias (opcionales) a documentos del sistema
    public int? IdRemito { get; set; }
    public int? IdRemitoDetalle { get; set; }
    public long? IdComprobante { get; set; }
    public long? IdComprobanteDetalle { get; set; }
    public int? IdInventario { get; set; }
    public int? IdInventarioDetalle { get; set; }

    // Trazabilidad por lote/serie (si aplica)
    public string? LoteNro { get; set; }
    public string? NroSerie { get; set; }
    public DateTime? FechaVencimiento { get; set; }

    public string? Observaciones { get; set; }

    public Articulo? Articulo { get; set; }
}

public enum TipoTrazabilidadEvento
{
    RecepcionDeposito = 1,
    ReposicionSala = 2,
    VentaCaja = 3,
    AnulacionVenta = 4,
    AjusteInventario = 5,
    CompraRecepcion = 6,
    Merma = 7,
    Transferencia = 8,
    SalidaRemito = 9,
    MovimientoInterno = 10
}

