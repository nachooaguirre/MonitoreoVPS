namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Movimiento de mercadería entre sucursales (ej. depósito central → local).
/// Al confirmar: descuenta origen y suma destino.
/// </summary>
public class TransferenciaInterna
{
    public int Id { get; set; }
    public int NroTransferencia { get; set; }
    public int IdSucursalOrigen { get; set; }
    public int IdSucursalDestino { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int IdUsuario { get; set; }
    public EstadoTransferenciaInterna Estado { get; set; } = EstadoTransferenciaInterna.Pendiente;
    public string? Observaciones { get; set; }

    public Sucursal? SucursalOrigen { get; set; }
    public Sucursal? SucursalDestino { get; set; }
    public ICollection<TransferenciaInternaDetalle> Detalles { get; set; } = [];
}

public class TransferenciaInternaDetalle
{
    public int Id { get; set; }
    public int IdTransferencia { get; set; }
    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; }

    public Articulo? Articulo { get; set; }
    public TransferenciaInterna? Transferencia { get; set; }
}

public enum EstadoTransferenciaInterna
{
    Pendiente = 0,
    Confirmada = 1,
    Anulada = 2
}
