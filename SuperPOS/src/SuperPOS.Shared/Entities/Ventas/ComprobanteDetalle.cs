namespace SuperPOS.Shared.Entities.Ventas;

public class ComprobanteDetalle
{
    public long Id { get; set; }
    public long IdComprobante { get; set; }
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PrecioUnitarioSinIva { get; set; }
    public decimal AlicuotaIva { get; set; }
    public decimal MontoIva { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal SubTotal { get; set; }
    public string? NroSerie { get; set; }
    public string? NroLote { get; set; }
    public DateTime? FechaVencimiento { get; set; }

    public Comprobante? Comprobante { get; set; }
    public Articulo? Articulo { get; set; }
}
