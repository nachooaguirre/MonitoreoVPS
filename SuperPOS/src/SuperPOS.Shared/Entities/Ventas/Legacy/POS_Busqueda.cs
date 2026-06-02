namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_Busqueda
{
    public string Busqueda { get; set; } = null!;
    public int? Panel { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? Ancho { get; set; }
    public int? Alto { get; set; }
    public int? FontSize { get; set; }
    public string? Tabla { get; set; }
    public int? CampoFoco { get; set; }
    public string? TipoCargaDatos { get; set; }
    public string? FiltroBusqueda { get; set; }
    public string? TipoFiltroDatos { get; set; }
    public bool BusquedaRemota { get; set; }
    public string? Servidor { get; set; }
    public string? Base { get; set; }
}
