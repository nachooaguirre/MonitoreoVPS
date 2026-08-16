namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Orden de Compra a proveedor (equivalente a OrdenesCompras en Tecnolar)
/// </summary>
public class OrdenCompra
{
    public int Id { get; set; }
    public int IdProveedor { get; set; }
    public int IdUsuario { get; set; }
    public int NroOrden { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEntregaEsperada { get; set; }
    public EstadoOrdenCompra Estado { get; set; } = EstadoOrdenCompra.Pendiente;
    public decimal TotalSinIva { get; set; }
    public decimal TotalIva { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public int? IdUsuarioRecepcion { get; set; }
    public int? IdOrdenCompraOriginal { get; set; }
    public string? MotivoDiferencia { get; set; }

    public Proveedor? Proveedor { get; set; }
    public ICollection<OrdenCompraDetalle> Detalles { get; set; } = [];
}

public class OrdenCompraDetalle
{
    public int Id { get; set; }
    public int IdOrdenCompra { get; set; }
    public int IdArticulo { get; set; }
    public decimal CantidadPedida { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal AlicuotaIva { get; set; }
    public decimal Subtotal { get; set; }
    public string? ObservacionDiferencia { get; set; }

    public Articulo? Articulo { get; set; }
    public OrdenCompra? OrdenCompra { get; set; }
}

public enum EstadoOrdenCompra
{
    Pendiente = 0,
    Enviada = 1,
    RecepcionParcial = 2,
    Recibida = 3,
    Anulada = 4,
    /// <summary>OC creada desde el asistente IA; aún no confirmada como pedido “pendiente” normal.</summary>
    Borrador = 5,
    Devolvida = 6
}
