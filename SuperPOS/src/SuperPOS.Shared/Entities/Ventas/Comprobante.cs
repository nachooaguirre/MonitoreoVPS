namespace SuperPOS.Shared.Entities.Ventas;

public class Comprobante
{
    public long Id { get; set; }
    public int IdTipoComprobante { get; set; }
    public char Letra { get; set; } = 'B';
    public int PuntoVenta { get; set; }
    public long Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    /// <summary>Null en comprobantes que son notas A PROVEEDOR (ver IdProveedor) — un comprobante tiene cliente O proveedor, no ambos.</summary>
    public int? IdCliente { get; set; }
    /// <summary>Si está seteado, este comprobante es una nota de débito/crédito A PROVEEDOR (no una venta) — ver POST /api/ventas/nota-proveedor.</summary>
    public int? IdProveedor { get; set; }
    public int IdCaja { get; set; }
    public int IdSucursal { get; set; }
    public int IdUsuario { get; set; }
    /// <summary>Comisión (recargo por medio de pago/plataforma) incluida en el comprobante. Si es &gt; 0 en una Factura A/B, AFIP exige emitir como Factura de Crédito Electrónica MiPyME (tipo 60/61) en vez del tipo estándar.</summary>
    public decimal Comision { get; set; }
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
    public Proveedor? Proveedor { get; set; }
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
