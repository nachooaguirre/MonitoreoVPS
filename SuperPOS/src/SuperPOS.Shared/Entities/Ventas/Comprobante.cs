namespace SuperPOS.Shared.Entities.Ventas;

public class Comprobante
{
    public long Id { get; set; }
    public int IdTipoComprobante { get; set; }
    public char Letra { get; set; } = 'B';
    public int PuntoVenta { get; set; }
    public long Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int IdCliente { get; set; }
    public int IdCaja { get; set; }
    public int IdSucursal { get; set; }
    public int IdUsuario { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal TotalIva21 { get; set; }
    public decimal TotalIva105 { get; set; }
    public decimal TotalIva0 { get; set; }
    public decimal Total { get; set; }
    public EstadoComprobante Estado { get; set; } = EstadoComprobante.Pendiente;
    public bool EsFacturaElectronica { get; set; }
    public long? CAE { get; set; }
    public DateTime? CAEVencimiento { get; set; }
    public string? QrAfip { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? IdUsuarioAnulacion { get; set; }

    public TipoComprobante? TipoComprobante { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<ComprobanteDetalle> Detalles { get; set; } = [];
    public ICollection<ComprobantePago> Pagos { get; set; } = [];
}

public enum EstadoComprobante
{
    Pendiente = 0,
    Emitido = 1,
    Anulado = 2,
    EnProceso = 3
}
