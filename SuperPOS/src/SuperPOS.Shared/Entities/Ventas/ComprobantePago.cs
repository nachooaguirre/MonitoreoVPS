namespace SuperPOS.Shared.Entities.Ventas;

public class ComprobantePago
{
    public long Id { get; set; }
    public long IdComprobante { get; set; }
    public int IdMedioPago { get; set; }
    public decimal Importe { get; set; }
    public string? Referencia { get; set; }
    public decimal Vuelto { get; set; }

    public Comprobante? Comprobante { get; set; }
    public MedioPago? MedioPago { get; set; }
}
