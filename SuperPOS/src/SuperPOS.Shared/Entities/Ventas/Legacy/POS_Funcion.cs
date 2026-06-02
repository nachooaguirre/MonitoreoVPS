namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_Funcion
{
    public int NroFuncion { get; set; }
    public string? Funcion { get; set; }
    public int? Acumulador { get; set; }
    public string? Descripcion { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public int? Ancho { get; set; }
    public int? Alto { get; set; }
    public int? Panel { get; set; }
    public int? MoverPanel { get; set; }
    public int? MoverPanelPos { get; set; }
    public int? LlamarFuncion { get; set; }
    public int? Codigo { get; set; }
    public int? FontSize { get; set; }
    public int? FontColor { get; set; }
    public int? Alineacion { get; set; }
    public string? Busqueda { get; set; }
    public bool ImporteObligatorio { get; set; }
    public string? Imagen { get; set; }
    public int? FocoEnIngreso { get; set; }
    public bool EsEnvase { get; set; }
    public decimal? PorcentajeMaximo { get; set; }
    public int? Nivel { get; set; }
    public int? Tiempo { get; set; }
    public bool EsCtaCte { get; set; }
    public int? AcumuladorVuelto { get; set; }
    public int? MonedaAjusteCotizacion { get; set; }
    public int? CantidadCupones { get; set; }
    public int? Formulario { get; set; }
    public int? NroEditVariable { get; set; }
    public bool AbreCajon { get; set; }
    public int? NroCupon { get; set; }
}
