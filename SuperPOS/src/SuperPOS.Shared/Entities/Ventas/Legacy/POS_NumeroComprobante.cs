namespace SuperPOS.Shared.Entities.Ventas.Legacy;

public class POS_NumeroComprobante
{
    public string TipoCbte { get; set; } = null!;
    public int? NroSiguienteCbte { get; set; }
    public int? FormatoImpresion { get; set; }
    public bool SumaPuntos { get; set; }
}
