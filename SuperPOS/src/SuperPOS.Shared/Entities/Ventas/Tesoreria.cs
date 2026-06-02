namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Cuenta bancaria o caja de tesorería
/// </summary>
public class CuentaTesoreria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;    // Ej: "Banco Nación Cta. Cte", "Caja Chica"
    public TipoCuentaTesoreria Tipo { get; set; }
    public string? NroCuenta { get; set; }
    public string? CBU { get; set; }
    public string? Banco { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal SaldoActual { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public ICollection<MovimientoTesoreria> Movimientos { get; set; } = [];
}

public enum TipoCuentaTesoreria
{
    CajaEfectivo = 0,
    CuentaCorrienteBancaria = 1,
    CajaAhorroBancaria = 2,
    TarjetaCredito = 3,
    Otro = 9
}

/// <summary>
/// Movimiento de tesorería: ingreso, egreso, transferencia entre cuentas
/// </summary>
public class MovimientoTesoreria
{
    public int Id { get; set; }
    public int IdCuenta { get; set; }
    public int? IdCuentaDestino { get; set; }           // Para transferencias entre cuentas
    public TipoMovimientoTesoreria Tipo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? NroDocumento { get; set; }           // Nro cheque, transferencia, etc.
    public string? Beneficiario { get; set; }           // Proveedor, empleado, etc.
    public int IdUsuario { get; set; }
    public int? IdVenta { get; set; }
    public int? IdCompra { get; set; }
    public string? Observaciones { get; set; }
    public bool Conciliado { get; set; }                // Si fue verificado contra extracto bancario
    public DateTime? FechaConciliacion { get; set; }
    public CuentaTesoreria? Cuenta { get; set; }
    public CuentaTesoreria? CuentaDestino { get; set; }
}

public enum TipoMovimientoTesoreria
{
    Ingreso = 0,
    Egreso = 1,
    Transferencia = 2,          // Entre cuentas propias
    AjustePositivo = 3,
    AjusteNegativo = 4
}

/// <summary>
/// Cheque recibido (de cliente) o emitido (a proveedor)
/// </summary>
public class Cheque
{
    public int Id { get; set; }
    public TipoCheque Tipo { get; set; }
    public EstadoCheque Estado { get; set; } = EstadoCheque.Cartera;
    public string NroCheque { get; set; } = string.Empty;
    public string Banco { get; set; } = string.Empty;
    public string? NroCuenta { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaPago { get; set; }             // Fecha de cobro/pago
    public decimal Monto { get; set; }
    public string? Librador { get; set; }               // Quien lo emitió (para cheques recibidos)
    public int? IdCliente { get; set; }
    public int? IdProveedor { get; set; }
    public int? IdCuenta { get; set; }                  // Cuenta destino cuando se deposita
    public int IdUsuario { get; set; }
    public int? IdChequera { get; set; }
    public string? Observaciones { get; set; }
    public bool EsRechazado { get; set; }
    public DateTime? FechaRechazo { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public Proveedor? Proveedor { get; set; }
    public CuentaTesoreria? Cuenta { get; set; }
    public Chequera? Chequera { get; set; }
}

public enum TipoCheque
{
    Recibido = 0,   // De clientes
    Emitido = 1     // A proveedores
}

public enum EstadoCheque
{
    Cartera = 0,
    Depositado = 1,
    Cobrado = 2,
    Entregado = 3,  // Emitido a proveedor y entregado
    Rechazado = 4,
    Anulado = 5
}

/// <summary>
/// Gasto por caja chica (equivalente a "gastos por caja" en LoginMarket)
/// </summary>
public class GastoCaja
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public CategoriaGasto Categoria { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int IdCajaOrigen { get; set; }
    public int IdUsuario { get; set; }
    public string? NroComprobante { get; set; }
    public string? Observaciones { get; set; }
}

public enum CategoriaGasto
{
    Limpieza = 0,
    Insumos = 1,
    Servicios = 2,
    Mantenimiento = 3,
    Personal = 4,
    Logistica = 5,
    Varios = 9
}
