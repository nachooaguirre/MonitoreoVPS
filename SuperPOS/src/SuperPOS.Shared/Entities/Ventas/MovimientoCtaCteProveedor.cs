namespace SuperPOS.Shared.Entities.Ventas;

public class MovimientoCtaCteProveedor
{
    public long Id { get; set; }
    public int IdProveedor { get; set; }
    public DateTime Fecha { get; set; }
    public TipoMovimientoCteProveedor Tipo { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public long? IdCompra { get; set; }                 // Referencia a la compra (si aplica)
    public int? IdUsuario { get; set; }
    public decimal Debe { get; set; }                   // Lo que nosotros le debemos al proveedor (cargo)
    public decimal Haber { get; set; }                  // Lo que le pagamos (abono)
    public decimal SaldoAcumulado { get; set; }          // Saldo luego de este movimiento

    public Proveedor? Proveedor { get; set; }
}

public enum TipoMovimientoCteProveedor
{
    CompraCredito = 1,
    Pago = 2,
    NotaCredito = 3,
    NotaDebito = 4,
    AjusteManual = 5
}
