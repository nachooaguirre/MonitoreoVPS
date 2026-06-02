namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Remito de entrega (equivalente a Remitos en Tecnolar)
/// Documenta la entrega de mercadería sin transacción económica.
/// Puede estar asociado a una OC recibida o a una venta con entrega diferida.
/// </summary>
public class Remito
{
    public int Id { get; set; }
    public int NroRemito { get; set; }
    public TipoRemito Tipo { get; set; } = TipoRemito.Entrada;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? IdProveedor { get; set; }
    public int? IdCliente { get; set; }
    public int? IdOrdenCompra { get; set; }
    public int? IdCompra { get; set; }
    public int IdUsuario { get; set; }

    public string? NroRemitoExterno { get; set; }   // Nro del remito del proveedor
    public string? Transportista { get; set; }
    public string? Observaciones { get; set; }
    public EstadoRemito Estado { get; set; } = EstadoRemito.Pendiente;

    public Proveedor? Proveedor { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<RemitoDetalle> Detalles { get; set; } = [];
}

public class RemitoDetalle
{
    public int Id { get; set; }
    public int IdRemito { get; set; }
    public int IdArticulo { get; set; }
    public decimal CantidadRemitida { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal PrecioCosto { get; set; }
    public string? LoteNro { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? NroSerie { get; set; }
    public string? Observaciones { get; set; }

    public Articulo? Articulo { get; set; }
    public Remito? Remito { get; set; }
}

public enum TipoRemito
{
    Entrada = 0,    // Mercadería que entra (de proveedor)
    Salida = 1      // Mercadería que sale (a cliente, transferencia)
}

public enum EstadoRemito
{
    Pendiente = 0,
    Confirmado = 1,
    Anulado = 2
}
