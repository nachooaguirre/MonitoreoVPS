namespace SuperPOS.Shared.Entities.Ventas;

public class Compra
{
    public long Id { get; set; }
    public int IdProveedor { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? NumeroFactura { get; set; }         // Número de factura del proveedor
    public string? LetraFactura { get; set; }
    public int IdTipoComprobante { get; set; }
    public EstadoCompra Estado { get; set; } = EstadoCompra.Pendiente;
    public decimal SubTotal { get; set; }
    public decimal TotalIva { get; set; }
    public decimal PercepcionIva { get; set; }
    public decimal PercepcionNacional { get; set; }
    public decimal PercepcionIIBB { get; set; }
    public decimal PercepcionMunicipal { get; set; }
    public decimal ImpuestosInternos { get; set; }
    public decimal ImporteNoGravado { get; set; }
    public decimal ImporteExento { get; set; }
    public int PuntoVentaProveedor { get; set; }
    public string? DespachoImportacion { get; set; }
    public decimal IvaComision { get; set; }
    public string? CuitCorredor { get; set; }
    public string? NombreCorredor { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }

    public Proveedor? Proveedor { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public ICollection<CompraDetalle> Detalles { get; set; } = [];
}

public class CompraDetalle
{
    public long Id { get; set; }
    public long IdCompra { get; set; }
    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioCosto { get; set; }           // Precio de costo en esta compra
    public decimal Bonificacion { get; set; }          // % descuento del proveedor
    public decimal PrecioCostoNeto { get; set; }       // Costo final después de bonif.
    public decimal AlicuotaIva { get; set; } = 21;
    public decimal SubTotal { get; set; }
    public bool ActualizaPrecio { get; set; } = true;  // Si actualiza precio de costo del artículo

    public Compra? Compra { get; set; }
    public Articulo? Articulo { get; set; }
}

public enum EstadoCompra
{
    Pendiente = 0,
    Recibida = 1,
    Parcial = 2,
    Anulada = 3
}
