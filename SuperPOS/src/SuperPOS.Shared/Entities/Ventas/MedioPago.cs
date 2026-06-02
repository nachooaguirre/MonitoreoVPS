namespace SuperPOS.Shared.Entities.Ventas;

public class MedioPago
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoMedioPago Tipo { get; set; }
    public bool RequiereReferencia { get; set; }
    public bool Activo { get; set; } = true;
    public string? CodigoAfip { get; set; }     // Código AFIP para comprobantes de pago electrónico
    public bool EsTarjeta => Tipo is TipoMedioPago.TarjetaDebito or TipoMedioPago.TarjetaCredito;
}

public enum TipoMedioPago
{
    Efectivo = 1,
    TarjetaDebito = 2,
    TarjetaCredito = 3,
    Cheque = 4,
    MercadoPago = 5,
    Transferencia = 6,
    CtaCte = 7,
    Vale = 8
}
