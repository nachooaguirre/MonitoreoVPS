namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Bitácora de cada llamada a AFIP (FECAESolicitar) por comprobante: request/response XML crudos
/// y resultado. Permite auditar/depurar sin depender de que el operador recuerde el error.
/// Un comprobante puede tener varias filas si hubo reintentos (ver SolicitarCAE).
/// </summary>
public class ComprobanteAfipLog
{
    public long Id { get; set; }
    public long IdComprobante { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public char Resultado { get; set; }   // 'A' aceptado, 'R' rechazado, 'E' error/excepción, 'C' recuperado (10016)
    public string? Detalle { get; set; }  // mensaje de error/observaciones de AFIP
    public string? RequestXml { get; set; }
    public string? ResponseXml { get; set; }

    public Comprobante? Comprobante { get; set; }
}
