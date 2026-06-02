namespace SuperPOS.Shared.Entities.Ventas;

public class MovimientoCtaCte
{
    public long Id { get; set; }
    public int IdCliente { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimientoCte Tipo { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public long? IdComprobante { get; set; }           // Referencia a la venta (si aplica)
    public int? IdUsuario { get; set; }
    public decimal Debe { get; set; }                  // Lo que el cliente debe (cargo)
    public decimal Haber { get; set; }                 // Lo que el cliente pagó (abono)
    public decimal SaldoAcumulado { get; set; }        // Saldo luego de este movimiento

    public Cliente? Cliente { get; set; }
}

public enum TipoMovimientoCte
{
    VentaCredito = 1,
    Pago = 2,
    NotaCredito = 3,
    NotaDebito = 4,
    AjusteManual = 5
}
