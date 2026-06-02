using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperPOS.API;

namespace SuperPOS.API.Services;

public interface IAiService
{
    /// <summary>Analiza artículos con stock bajo y recomienda órdenes de compra. <paramref name="buscarEnWeb"/> añade contexto de internet (mercado, competencia, tendencias).</summary>
    /// <param name="maxFilasSugeridas">Máx. filas a devolver (grilla + JSON); null usa configuración de la API.</param>
    Task<AiRespuesta> SugerenciasCompraAsync(int diasAnalisis = 30, string? instruccionExtra = null, bool buscarEnWeb = false, int? maxFilasSugeridas = null);

    /// <summary>Alerta sobre lotes con vencimiento próximo. <paramref name="buscarEnWeb"/> puede traer buenas prácticas o contexto de mercado.</summary>
    Task<AiRespuesta> AlertasVencimientosAsync(int diasAlerta = 30, string? instruccionExtra = null, bool buscarEnWeb = false);

    /// <summary>Análisis de tendencias de ventas de los últimos N días. <paramref name="buscarEnWeb"/> añade benchmarks públicos o noticias del sector.</summary>
    Task<AiRespuesta> AnalisisVentasAsync(int dias = 30, string? instruccionExtra = null, bool buscarEnWeb = false);

    /// <summary>Consulta libre: el usuario escribe una pregunta y la IA responde con contexto de la BD. Opcional: historial (multi-turno) y búsqueda pública en internet (competidores, precios aproximados) si <paramref name="buscarEnWeb"/> es true.</summary>
    Task<AiRespuesta> ConsultaLibreAsync(string pregunta, IReadOnlyList<AiChatMensaje>? historial = null, bool buscarEnWeb = false);

    /// <summary>Convierte texto o imagen (listas, PDFs exportados, fotos) en filas estructuradas con bonificaciones.</summary>
    Task<AiImportListaProveedorResult> EstructurarListaProveedorAsync(
        string? textoBruto, string? imagenBase64, string? imagenMime, string? nombreProveedor, CancellationToken cancellationToken = default);

    /// <summary>Recomienda cantidades a comprar según escalas de bonificación, ventas y necesidad en N días.</summary>
    Task<AiRespuesta> RecomendarCompraConBonificacionesAsync(
        int idListaProveedor, int diasProyeccion, string? instruccion, CancellationToken cancellationToken = default);
}

/// <summary>Mensaje previo de la conversación (rol: user o assistant).</summary>
public class AiChatMensaje
{
    public string Rol { get; set; } = "user";
    public string Contenido { get; set; } = string.Empty;
}

// ─── DTOs de respuesta ────────────────────────────────────────────────────────

public class AiRespuesta
{
    public bool Exito { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string? Error { get; set; }

    /// <summary>Solo consulta libre: true si se usó búsqueda en internet (o se intentó sin resultados). Null en otros endpoints.</summary>
    public bool? BusquedaWebAplicada { get; set; }

    /// <summary>Artículos con stock ≤ mínimo de góndola (ficha). Null si no aplica.</summary>
    public int? SugerenciasTotalBajoMinimo { get; set; }

    /// <summary>Cantidad de filas enviadas a la IA y devueltas (puede ser menor que Total si hay límite).</summary>
    public int? SugerenciasIncluidas { get; set; }

    // Datos estructurados opcionales (según el endpoint)
    public List<AiSugerenciaCompra>? SugerenciasCompra { get; set; }
    public List<AiAlertaVencimiento>? AlertasVencimiento { get; set; }
    public AiAnalisisVentas? AnalisisVentas { get; set; }
}

public class AiSugerenciaCompra
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal CantidadSugerida { get; set; }
    public decimal CantidadVendida30Dias { get; set; }
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal TotalEstimado { get; set; }
    public decimal AlicuotaIva { get; set; }
    public string Prioridad { get; set; } = "Media"; // Alta / Media / Baja

    /// <summary>Si el artículo figura vinculado en la tarifa de compra más reciente de ese proveedor, precio unitario y metadatos.</summary>
    public decimal? PrecioListaCompraReciente { get; set; }
    public string? NombreTarifaCompra { get; set; }
    public DateTime? FechaTarifaCompra { get; set; }
    /// <summary>Resumen breve de bonificaciones (JSON) en la lista, si hay.</summary>
    public string? BonifTarifaCompra { get; set; }

    /// <summary>BajoMinGondola = stock ≤ mínimo; Rotación = venta en el periodo aún con stock &gt; mínimo (reponer preventivo hasta el tope de filas).</summary>
    public string OrigenSugerencia { get; set; } = "BajoMinGondola";

    /// <summary>cantidadVendida30Dias / días de análisis; referencia de ritmo de venta.</summary>
    public decimal VelocidadVentaDiaria { get; set; }

    /// <summary>Días de stock al ritmo actual (stock / velocidad); null si no hay rotación en el periodo.</summary>
    public int? CoberturaDiasAproximada { get; set; }
}

public class AiAlertaVencimiento
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? LoteNro { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int DiasRestantes { get; set; }
    public decimal Cantidad { get; set; }
    public string Urgencia { get; set; } = "Normal"; // Critica / Alta / Normal
}

public class AiAnalisisVentas
{
    public int DiasAnalizados { get; set; }
    public decimal TotalFacturado { get; set; }
    public int CantidadVentas { get; set; }
    public decimal TicketPromedio { get; set; }
    public List<AiTopProducto> TopProductos { get; set; } = [];
    public List<AiVentaDia> VentasPorDia { get; set; } = [];
}

public class AiTopProducto
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalFacturado { get; set; }
}

public class AiVentaDia
{
    public DateOnly Fecha { get; set; }
    public decimal Total { get; set; }
    public int Comprobantes { get; set; }
}
