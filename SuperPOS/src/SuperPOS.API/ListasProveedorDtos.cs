namespace SuperPOS.API;

/// <summary>Respuesta de estructuración con IA a partir de texto/PDF/Excel bruto.</summary>
public class AiImportListaProveedorResult
{
    public bool Exito { get; set; }
    public string? Error { get; set; }
    public string? AvisoOrigen { get; set; }
    public List<LineaImportProveedorDto> Lineas { get; set; } = [];
}

public class LineaImportProveedorDto
{
    public string CodigoProveedor { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public decimal? IvaPorcentaje { get; set; }
    public List<BonifEscalaImportDto> Bonificaciones { get; set; } = [];
}

public class BonifEscalaImportDto
{
    public decimal CantidadMin { get; set; }
    public decimal Porcentaje { get; set; }
    public string? Nota { get; set; }
}

public record RecomendarListaRequest(int IdLista, int DiasProyeccion = 10, string? Instruccion = null);

public class ListaLineaUpdateDto
{
    public string? CodigoProveedor { get; set; }
    public string? Descripcion { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public decimal? IvaPorcentaje { get; set; }
    public string? BonificacionesJson { get; set; }
    public int? IdArticulo { get; set; }
}
