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
    Vale = 8,
    Giro = 9,
    Ticket = 10,
    Otros = 11,
    CtaDni = 12
}

/// <summary>Marca de tarjeta soportada por el cobro integrado (Posnet), con su recargo/descuento % opcional.</summary>
public class TarjetaMarca
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsCredito { get; set; }
    /// <summary>% que se suma al monto a cobrar al elegir esta tarjeta. Negativo = descuento.</summary>
    public decimal PorcentajeRecargo { get; set; }
    public bool Activo { get; set; } = true;
}
