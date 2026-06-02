namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_Ingreso
{
    public int NroIngreso { get; set; }
    public int? Panel { get; set; }
    public string? Descripcion { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? Ancho { get; set; }
    public int? Alto { get; set; }
    public int? LargoMaximo { get; set; }
    public int? FontSize { get; set; }
    public int? PasswordChar { get; set; }
}
