namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_Rendicion
{
    public string Rendicion { get; set; } = null!;
    public int? Panel { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? Ancho { get; set; }
    public int? Alto { get; set; }
    public int? FontSize { get; set; }
    public bool MuestraImportesCaja { get; set; }
}
