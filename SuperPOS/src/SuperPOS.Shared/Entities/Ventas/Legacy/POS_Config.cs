namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_Config
{
    public int NroCaja { get; set; }
    public bool Animacion { get; set; }
    public int? PanelLogin { get; set; }
    public decimal? VentaCantidadMaxima { get; set; }
    public decimal? VentaImporteMaximo { get; set; }
    public decimal? VentaImporteMinimo { get; set; }
    public decimal? VentaCantidadDefecto { get; set; }
    public decimal? VentaCantidadMaximaPagos { get; set; }
    public bool VentaPedirCantidad { get; set; }
    public bool VerVideo { get; set; }
    public int? PanelPrincipal { get; set; }
    public string? PuertoScanner { get; set; }
    public string? PuertoDisplay { get; set; }
    public string? PuertoBalanza { get; set; }
    public string? PuertoFiscal { get; set; }
    public string? PathImagenesArticulos { get; set; }
    public string? PathImagenesCajeros { get; set; }
    public string? FiscalMarca { get; set; }
    public string? FiscalModelo { get; set; }
    public string? ModoCobro { get; set; }
    public int? NetoMinimoPercepcionIIBB { get; set; }
    public bool MuestraCodigoEnPantalla { get; set; }
    public bool RendicionGeneraRetiro { get; set; }
    public bool SubtotalObligatorio { get; set; }
    public string? ModoItem { get; set; }
    public bool ClienteObligatorio { get; set; }
    public int? SucursalFacturacion { get; set; }
    public int? SucursalFacturacion2 { get; set; }
    public bool ConfirmaFacturacion { get; set; }
    public bool StockOnLine { get; set; }
    public bool SumaPuntos { get; set; }
    public bool UsaDescripcionLarga { get; set; }
    public bool ClienteFacturaCtaCte { get; set; }
    public int? PuntosXPeso { get; set; }
    public bool ImprimeEAN { get; set; }
    public bool ZetaObligatoria { get; set; }
    public bool ConfirmaZeta { get; set; }
    public bool ZetaEnviaVenta { get; set; }
    public bool ControlarCajon { get; set; }
    public string? PathImagenes { get; set; }
    public string? PathImagenesServidor { get; set; }
    public decimal? VentaImporteMinimoCbte { get; set; }
    public decimal? VentaImporteMaximoCbte { get; set; }
    public bool TruncaPuntos { get; set; }
    public decimal? PesosXPunto { get; set; }
    public bool ClienteFacturaPuntos { get; set; }
    public bool ObligarCierreCajero { get; set; }
}
