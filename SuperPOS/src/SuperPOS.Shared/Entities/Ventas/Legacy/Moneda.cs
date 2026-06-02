namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class Moneda
{
    public short Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Cotizacion { get; set; }
    public int? Acumulador { get; set; }
    public string? Tipo { get; set; }
    public decimal? Cuenta { get; set; }
    public bool EsDivisa { get; set; }
    public bool Transmitido { get; set; }
    public decimal? Comision { get; set; }
    public int? DiasCobro { get; set; }
    public string? DescripcionImpresion { get; set; }
    public decimal? ImporteRetiro { get; set; }
    public int? MonedaCompletaRecargo { get; set; }
}
