namespace SuperPOS.Shared.Entities.Ventas;

public class TipoComprobante
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Abreviatura { get; set; }
    public bool EsVenta { get; set; }
    public bool EsCompra { get; set; }
    public bool RequiereCAE { get; set; }
    public int? CodigoAfip { get; set; }
    public bool Activo { get; set; } = true;
}
