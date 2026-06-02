namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_BusquedaCampo
{
    public string Busqueda { get; set; } = null!;
    public int Posicion { get; set; }
    public string? Campo { get; set; }
    public int? AnchoColumna { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? Ancho { get; set; }
    public int? Alto { get; set; }
    public int? FontSize { get; set; }
    public int? PosXEnLista { get; set; }
    public int? PosYEnLista { get; set; }
    public int? NroIngreso { get; set; }
    public int? CaracterComienzoBusqueda { get; set; }
}
