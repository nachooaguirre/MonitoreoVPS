namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Cierre de caja (equivalente a "Zeta" en el sistema Tecnolar)
/// Agrupa todas las ventas de un turno/día y cierra la caja.
/// </summary>
public class Zeta
{
    public int Id { get; set; }
    public int IdCaja { get; set; }
    public int IdSucursal { get; set; }
    public int IdUsuario { get; set; }
    public int NroZeta { get; set; }

    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; } = DateTime.UtcNow;

    // Totales de ventas del turno
    public decimal TotalVentas { get; set; }
    public decimal TotalDescuentos { get; set; }
    public decimal TotalIva21 { get; set; }
    public decimal TotalIva105 { get; set; }
    public decimal TotalIva0 { get; set; }
    public int CantidadVentas { get; set; }
    public int CantidadAnulaciones { get; set; }
    public decimal TotalAnulaciones { get; set; }

    // Arqueo de medios de pago
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjetaDebito { get; set; }
    public decimal TotalTarjetaCredito { get; set; }
    public decimal TotalTransferencia { get; set; }
    public decimal TotalCtaCte { get; set; }
    public decimal TotalMercadoPago { get; set; }
    public decimal TotalOtros { get; set; }

    // Diferencia arqueo (efectivo declarado vs calculado)
    public decimal EfectivoDeclarado { get; set; }
    public decimal DiferenciaArqueo { get; set; }

    public string? Observaciones { get; set; }
    public bool Anulada { get; set; }

    public ICollection<ZetaDetalleMedio> DetallesMedios { get; set; } = [];
}

public class ZetaDetalleMedio
{
    public int Id { get; set; }
    public int IdZeta { get; set; }
    public int IdMedioPago { get; set; }
    public string NombreMedio { get; set; } = string.Empty;
    public int CantOperaciones { get; set; }
    public decimal Total { get; set; }

    public Zeta? Zeta { get; set; }
}
