namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Registro automático de auditoría: qué cambió, en qué entidad, quién lo hizo y cuándo.
/// Se genera solo desde SuperPOSDbContext.SaveChangesAsync (ver override), nunca a mano.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public string Entidad { get; set; } = string.Empty;      // Nombre de la clase (ej. "Articulo")
    public string EntidadId { get; set; } = string.Empty;    // PK como string (soporta int/long/compuestas)
    public TipoAccionAuditoria Accion { get; set; }
    public string? Descripcion { get; set; }                 // Resumen legible ("StockActual: 2 -> 1")
    public string? CambiosJson { get; set; }                  // { "Campo": { "anterior": x, "nuevo": y } }
}

public enum TipoAccionAuditoria
{
    Creacion = 1,
    Modificacion = 2,
    Eliminacion = 3
}
