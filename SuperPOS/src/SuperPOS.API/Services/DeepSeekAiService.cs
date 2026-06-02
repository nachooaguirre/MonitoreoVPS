using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Services;

public partial class DeepSeekAiService(SuperPOSDbContext db, IConfiguration config, ILogger<DeepSeekAiService> logger, IWebSearchService webSearch) : IAiService
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    private string ApiKey => config["DeepSeek:ApiKey"] ?? string.Empty;
    private string BaseUrl => config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com/v1";
    private string Model => config["DeepSeek:Model"] ?? "deepseek-chat";
    private string NombreEmpresa => config["SuperPOS:NombreEmpresa"] ?? "la empresa";

    private int MaxArticulosSugerencias() =>
        Math.Clamp(config.GetValue("DeepSeek:MaxArticulosSugerenciasCompra", 2000), 1, 5000);

    private int MaxMensajesHistorialChat() =>
        Math.Clamp(config.GetValue("DeepSeek:MaxMensajesHistorialChat", 200), 4, 400);

    private async Task<(string bloque, bool tuvoContenido)> ConstruirBloqueContextoWebAsync(
        bool activar, string? instruccionExtra, string consultaDefecto)
    {
        if (!activar) return (string.Empty, false);
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(instruccionExtra))
            partes.Add(instruccionExtra.Trim());
        partes.Add(consultaDefecto);
        var q = string.Join(" ", partes);
        if (q.Length > 400) q = q[..400];
        var res = await webSearch.BuscarResumenWebAsync(q);
        if (string.IsNullOrWhiteSpace(res))
            res = await webSearch.BuscarResumenWebAsync(consultaDefecto);
        if (string.IsNullOrWhiteSpace(res))
            return ($"""

            Contexto de internet: sin resultados en este intento. Para mejores datos configurá WebSearch:TavilyApiKey o WebSearch:BingApiKey. No inventes cifras de mercado.
            """, false);
        return ($"""


            Contexto de internet (noticias, competencia, tendencias, referencias públicas; no reemplaza al inventario del POS):
            --- WEB ---
            {res}
            --- FIN WEB ---
            """, true);
    }

    // ─── Sugerencias de compra ────────────────────────────────────────────────

    public async Task<AiRespuesta> SugerenciasCompraAsync(int diasAnalisis = 30, string? instruccionExtra = null, bool buscarEnWeb = false, int? maxFilasSugeridas = null)
    {
        // Parsear días desde instrucción si los menciona (ej. "ultimos 15 dias")
        if (!string.IsNullOrWhiteSpace(instruccionExtra))
        {
            var matchDias = System.Text.RegularExpressions.Regex.Match(instruccionExtra.ToLower(), @"(\d+)\s*d[íi]as");
            if (matchDias.Success && int.TryParse(matchDias.Groups[1].Value, out int diasExtra) && diasExtra > 0)
                diasAnalisis = diasExtra;
        }

        var fechaDesde = DateTime.UtcNow.AddDays(-diasAnalisis);
        var tope = Math.Min(
            Math.Clamp(maxFilasSugeridas ?? config.GetValue("DeepSeek:DefaultFilasSugerenciasCompra", 200), 1, 5000),
            MaxArticulosSugerencias());
        var diasD = Math.Max(1, diasAnalisis);

        var emitidosP = db.Comprobantes.AsNoTracking()
            .Where(c => c.Fecha >= fechaDesde && c.Estado == EstadoComprobante.Emitido);
        var nComps = await emitidosP.CountAsync();
        var facturadoPeriodo = await emitidosP.SumAsync(c => c.Total);

        IQueryable<Articulo> qBase = db.Articulos.AsNoTracking().Where(a => a.Activo);
        bool filtroPersonalizado = false;
        bool reponerVendido = false;

        if (!string.IsNullOrWhiteSpace(instruccionExtra))
        {
            var ins = instruccionExtra.ToLower();
            if (ins.Contains("vendi") || ins.Contains("venta") || ins.Contains("salio") || ins.Contains("reponer"))
            {
                reponerVendido = true;
                filtroPersonalizado = true;
            }
            else
            {
                var toks = ExtraerTerminosBusqueda(ins);
                if (toks.Count > 0)
                {
                    filtroPersonalizado = true;
                    var a0 = toks[0].ToLower();
                    qBase = qBase.Where(a => a.Descripcion.ToLower().Contains(a0) ||
                                             (a.Departamento != null && a.Departamento.Nombre.ToLower().Contains(a0)) ||
                                             (a.Familia != null && a.Familia.Nombre.ToLower().Contains(a0)) ||
                                             (a.Marca != null && a.Marca.Nombre.ToLower().Contains(a0)));
                }
            }
        }

        if (!filtroPersonalizado)
        {
            qBase = qBase.Where(a => a.StockActual <= a.StockMinimo);
        }

        var totalBajo = await qBase.CountAsync();

        if (totalBajo == 0 && !filtroPersonalizado && !config.GetValue("DeepSeek:ComplementarSugerenciasConRotacion", true))
            return new AiRespuesta
            {
                Exito = true,
                Texto = "No se encontraron artículos bajo o en el mínimo de góndola (ficha: StockMinimo). Podés completar con sugerencias por rotación (config `DeepSeek:ComplementarSugerenciasConRotacion` = true) o ampliar el rango de días de venta.",
                SugerenciasTotalBajoMinimo = 0,
                SugerenciasIncluidas = 0
            };

        var cupoCritic = tope;
        if (totalBajo > 0) cupoCritic = Math.Min(totalBajo, tope);

        List<Articulo> articulos = [];
        if (reponerVendido)
        {
            // Fetch top sold articles in the period
            var idsTopVentas = await db.ComprobantesDetalle
                .AsNoTracking()
                .Where(d => d.Comprobante != null && d.Comprobante.Fecha >= fechaDesde && d.Comprobante.Estado != EstadoComprobante.Anulado)
                .GroupBy(d => d.IdArticulo)
                .OrderByDescending(g => g.Sum(d => d.Cantidad))
                .Select(g => g.Key)
                .Take(cupoCritic)
                .ToListAsync();

            articulos = await db.Articulos.AsNoTracking()
                .Include(a => a.Proveedor)
                .Where(a => a.Activo && idsTopVentas.Contains(a.Id))
                .ToListAsync();
        }
        else
        {
            articulos = totalBajo == 0 ? [] : await qBase
                .Include(a => a.Proveedor)
                .OrderByDescending(a => a.StockMinimo - a.StockActual)
                .ThenByDescending(a => a.StockMinimo)
                .ThenBy(a => a.StockActual)
                .Take(cupoCritic)
                .ToListAsync();
        }

        var ids = articulos.Select(a => a.Id).ToList();
        var ventasPorArticulo = ids.Count == 0
            ? new Dictionary<int, decimal>()
            : (await db.ComprobantesDetalle
                .AsNoTracking()
                .Where(d => ids.Contains(d.IdArticulo)
                            && d.Comprobante != null
                            && d.Comprobante.Fecha >= fechaDesde
                            && d.Comprobante.Estado != EstadoComprobante.Anulado)
                .GroupBy(d => d.IdArticulo)
                .Select(g => new { IdArticulo = g.Key, Cantidad = g.Sum(d => d.Cantidad) })
                .ToListAsync())
                .ToDictionary(v => v.IdArticulo, v => v.Cantidad);

        var sugerencias = articulos.Select(a =>
        {
            var vendido = ventasPorArticulo.GetValueOrDefault(a.Id, 0);
            var cantSugerida = reponerVendido ? vendido : Math.Max(a.StockMaximo - a.StockActual, a.StockMinimo * 2);
            if (cantSugerida < 0) cantSugerida = 0;
            var deficit = a.StockMinimo - a.StockActual;
            var prioridad = deficit >= a.StockMinimo ? "Alta" : deficit > 0 ? "Media" : "Baja";
            var s = new AiSugerenciaCompra
            {
                IdArticulo = a.Id,
                Descripcion = a.Descripcion,
                CodigoBarras = a.CodigoBarras,
                StockActual = a.StockActual,
                StockMinimo = a.StockMinimo,
                StockMaximo = a.StockMaximo,
                CantidadSugerida = cantSugerida,
                CantidadVendida30Dias = vendido,
                IdProveedor = a.IdProveedor,
                Proveedor = a.Proveedor?.RazonSocial ?? $"Proveedor #{a.IdProveedor}",
                PrecioCosto = a.PrecioCosto,
                TotalEstimado = cantSugerida * a.PrecioCosto,
                AlicuotaIva = a.AlicuotaIva,
                Prioridad = prioridad,
                OrigenSugerencia = reponerVendido ? "ReposicionVentas" : "BajoMinGondola"
            };
            AplicarVelocidadCobertura(s, vendido, diasD);
            return s;
        }).ToList();

        if (!filtroPersonalizado && config.GetValue("DeepSeek:ComplementarSugerenciasConRotacion", true) && sugerencias.Count < tope)
            await CompletarSugerenciasRotacionAsync(sugerencias, tope, fechaDesde, diasD);

        sugerencias = sugerencias
            .OrderByDescending(s => s.OrigenSugerencia == "BajoMinGondola" ? 1 : 0)
            .ThenByDescending(s => s.Prioridad == "Alta" ? 2 : s.Prioridad == "Media" ? 1 : 0)
            .ThenByDescending(s => s.CantidadVendida30Dias)
            .ToList();

        if (sugerencias.Count == 0)
            return new AiRespuesta
            {
                Exito = true,
                Texto = "No se armaron sugerencias: no hay artículos bajo mínimo y no hubo candidatos con ventas + espacio bajo el máximo en el periodo. Revisá el rango de días o los datos de stock/ventas.",
                SugerenciasTotalBajoMinimo = totalBajo,
                SugerenciasIncluidas = 0
            };

        await RellenarTarifasCompraEnSugerenciasAsync(sugerencias);

        var cortePorLimiteCritic = totalBajo > articulos.Count;
        var cargaIa = new
        {
            notaSistema = "Cada `stockMinimo` es el piso mínimo en góndola. Las filas con `OrigenSugerencia` = BajoMinGondola requieren atención de exposición. Las de Rotación son preventivas (venden en el periodo, stock aún por encima del mínimo) para reponer hacia el máximo sin forzar 10 u otro fijo. `velocidadVentaDiaria` = cantidadVendida30Dias / días; `coberturaDiasAproximada` = días aprox. de góndola a ese ritmo. `resumenVenta` resume facturación de comprobantes **emitidos** en el periodo. Si vienen `PrecioListaCompraReciente` / `NombreTarifaCompra`, es la tarifa de compra más reciente matcheada. Compará con `PrecioCosto` de ficha.",
            resumenVenta = new
            {
                diasAnalisis = diasAnalisis,
                comprobantesEmitidos = nComps,
                totalFacturado = facturadoPeriodo
            },
            totalConBajoOMenorAlMinInventario = totalBajo,
            filasIncluidas = sugerencias.Count,
            cortePorLimiteTecnico = cortePorLimiteCritic || sugerencias.Count >= tope,
            sugerencias
        };
        var dataJson = JsonSerializer.Serialize(cargaIa, _json);

        var (bloqueWeb, webOk) = await ConstruirBloqueContextoWebAsync(
            buscarEnWeb, instruccionExtra,
            "abastecimiento supermercado retail Argentina competencia tendencias ofertas mayoristas 2025");

        var instrExtra = string.IsNullOrWhiteSpace(instruccionExtra) ? "" :
            $"\n\nINSTRUCCIÓN ADICIONAL DEL USUARIO: {instruccionExtra}\nTené en cuenta esta instrucción por encima de las indicaciones anteriores.";

        var prompt = $"""
            Sos el asistente de compras de {NombreEmpresa} (red de supermercados, catálogo amplio: miles de artículos posibles en inventario).
            Trabajá con `stockMinimo` como piso mínimo en góndola que la empresa carga en cada ficha. Los datos JSON ya priorizan por faltante frente a ese piso; no asumas un tope de “10” u otro n rojo: cubrí la diversidad de proveedores y gravedad.
            
            Datos (JSON, incluye resumen y filas concretas):
            {dataJson}
            {bloqueWeb}
            
            Respondé con (no te limites arbitrariamente a 10 filas: cubrí lo que aporte valor según el listado, agrupá si excede muchas líneas):
            1. Resumen ejecutivo (2-4 oraciones) incluyendo si el listado está truncado respecto a `totalConBajoOMenorAlMinInventario` y qué implica.
            2. Análisis priorizado por gravedad y proveedor: incluí tantos renglones accionables como hagan falta del subconjunto enviado; usá secciones (alta / media) si ahorra tokens de lectura.
            3. Inversión estimada (referencial) agregada por proveedor. Si en filas hay `PrecioListaCompraReciente` y `BonifTarifaCompra`, mencioná ofertas por volumen o diferencias con el costo de ficha (`PrecioCosto`) cuando aporte valor a la negociación.
            4. Riesgo de quiebres, concentración de proveedores, y recomendación operativa (cita en pedido, cross-docking, etc.) si aplica.
            
            Formato: texto claro, español, sin devolver JSON en la respuesta.{instrExtra}
            """;

        var texto = await LlamarDeepSeekAsync(prompt, 4096);

        return new AiRespuesta
        {
            Exito = texto != null,
            Texto = texto ?? "No se pudo conectar con el servicio de IA.",
            SugerenciasCompra = sugerencias,
            SugerenciasTotalBajoMinimo = totalBajo,
            SugerenciasIncluidas = sugerencias.Count,
            Error = texto == null ? "Error al llamar a la API de DeepSeek." : null,
            BusquedaWebAplicada = buscarEnWeb
        };
    }

    // ─── Alertas de vencimientos ──────────────────────────────────────────────

    public async Task<AiRespuesta> AlertasVencimientosAsync(int diasAlerta = 30, string? instruccionExtra = null, bool buscarEnWeb = false)
    {
        var fechaLimite = DateTime.UtcNow.AddDays(diasAlerta);
        var hoy = DateTime.UtcNow;

        var lotes = await db.TrazabilidadEventos
            .Include(t => t.Articulo)
            .Where(t => t.FechaVencimiento.HasValue
                     && t.FechaVencimiento.Value >= hoy
                     && t.FechaVencimiento.Value <= fechaLimite
                     && t.Cantidad > 0)
            .OrderBy(t => t.FechaVencimiento)
            .Take(100)
            .ToListAsync();

        // Artículos que requieren vencimiento pero no tienen lotes registrados aún
        var artsSinLote = await db.Articulos
            .Where(a => a.Activo && a.RequiereFechaVencimiento && a.StockActual > 0)
            .Select(a => new { a.Id, a.Descripcion, a.StockActual })
            .ToListAsync();

        var alertas = lotes.Select(t =>
        {
            var dias = (t.FechaVencimiento!.Value - hoy).Days;
            return new AiAlertaVencimiento
            {
                IdArticulo = t.IdArticulo,
                Descripcion = t.Articulo?.Descripcion ?? $"Artículo #{t.IdArticulo}",
                LoteNro = t.LoteNro,
                FechaVencimiento = t.FechaVencimiento.Value,
                DiasRestantes = dias,
                Cantidad = t.Cantidad,
                Urgencia = dias <= 7 ? "Critica" : dias <= 15 ? "Alta" : "Normal"
            };
        }).ToList();

        string texto;
        bool webUsadoVenc = false;

        if (alertas.Count == 0 && artsSinLote.Count == 0)
        {
            texto = $"No hay lotes próximos a vencer en los próximos {diasAlerta} días.";
        }
        else
        {
            var dataJson = JsonSerializer.Serialize(new
            {
                lotesPorVencer = alertas,
                articulosSinFechaLote = artsSinLote.Take(20)
            }, _json);

            var (bloqueWeb, _) = await ConstruirBloqueContextoWebAsync(
                buscarEnWeb, instruccionExtra,
                "vencimientos alimentos retail supermercado promociones liquidación Argentina 2025");

            webUsadoVenc = buscarEnWeb;

            var instrExtra = string.IsNullOrWhiteSpace(instruccionExtra) ? "" :
                $"\n\nINSTRUCCIÓN ADICIONAL DEL USUARIO: {instruccionExtra}\nTené en cuenta esta instrucción por encima de las indicaciones anteriores.";

            var prompt = $"""
                Sos el asistente de {NombreEmpresa}, un supermercado.
                Analizá los lotes próximos a vencer y los artículos que requieren control de vencimiento.
                
                Datos (JSON):
                {dataJson}
                {bloqueWeb}
                
                Respondé con:
                1. Resumen de la situación de vencimientos.
                2. Lista de artículos críticos (vencen en menos de 7 días) con acción recomendada (liquidar, devolver al proveedor, etc.).
                3. Artículos que vencen entre 8 y 30 días y estrategia sugerida.
                4. Si hay artículos sin fecha de lote registrada, mencionalo como riesgo.
                
                Sé concreto y accionable. Texto en español, sin JSON. Si recibiste contexto WEB, usalo solo como guía o tendencias, no precios fijos de tu local.{instrExtra}
                """;

            texto = await LlamarDeepSeekAsync(prompt) ?? "No se pudo conectar con el servicio de IA.";
        }

        return new AiRespuesta
        {
            Exito = true,
            Texto = texto,
            AlertasVencimiento = alertas,
            BusquedaWebAplicada = webUsadoVenc
        };
    }

    // ─── Análisis de ventas ───────────────────────────────────────────────────

    public async Task<AiRespuesta> AnalisisVentasAsync(int dias = 30, string? instruccionExtra = null, bool buscarEnWeb = false)
    {
        var fechaDesde = DateTime.UtcNow.AddDays(-dias);

        var comprobantes = await db.Comprobantes
            .Where(c => c.Fecha >= fechaDesde && c.Estado == EstadoComprobante.Emitido)
            .ToListAsync();

        var detalles = await db.ComprobantesDetalle
            .Include(d => d.Articulo)
            .Where(d => d.Comprobante!.Fecha >= fechaDesde
                     && d.Comprobante.Estado == EstadoComprobante.Emitido)
            .ToListAsync();

        var topProductos = detalles
            .GroupBy(d => d.IdArticulo)
            .Select(g => new AiTopProducto
            {
                IdArticulo = g.Key,
                Descripcion = g.First().Articulo?.Descripcion ?? $"Artículo #{g.Key}",
                CantidadVendida = g.Sum(d => d.Cantidad),
                TotalFacturado = g.Sum(d => d.SubTotal)
            })
            .OrderByDescending(p => p.TotalFacturado)
            .Take(10)
            .ToList();

        var ventasPorDia = comprobantes
            .GroupBy(c => DateOnly.FromDateTime(c.Fecha))
            .Select(g => new AiVentaDia
            {
                Fecha = g.Key,
                Total = g.Sum(c => c.Total),
                Comprobantes = g.Count()
            })
            .OrderBy(v => v.Fecha)
            .ToList();

        var totalFacturado = comprobantes.Sum(c => c.Total);
        var cantVentas = comprobantes.Count;

        var analisis = new AiAnalisisVentas
        {
            DiasAnalizados = dias,
            TotalFacturado = totalFacturado,
            CantidadVentas = cantVentas,
            TicketPromedio = cantVentas > 0 ? totalFacturado / cantVentas : 0,
            TopProductos = topProductos,
            VentasPorDia = ventasPorDia
        };

        if (cantVentas == 0)
        {
            return new AiRespuesta
            {
                Exito = true,
                Texto = $"No se registraron ventas en los últimos {dias} días.",
                AnalisisVentas = analisis
            };
        }

        var dataJson = JsonSerializer.Serialize(analisis, _json);

        var (bloqueWeb, _) = await ConstruirBloqueContextoWebAsync(
            buscarEnWeb, instruccionExtra,
            "tendencias consumo supermercado retail Argentina 2025 competencia cadenas");

        var instrExtra = string.IsNullOrWhiteSpace(instruccionExtra) ? "" :
            $"\n\nINSTRUCCIÓN ADICIONAL DEL USUARIO: {instruccionExtra}\nTené en cuenta esta instrucción por encima de las indicaciones anteriores.";

        var prompt = $"""
            Sos el analista de ventas de {NombreEmpresa}, un supermercado.
            Analizá los datos de ventas de los últimos {dias} días y generá insights accionables. Si hay un bloque WEB, usalo para enriquecer (benchmark, noticias del sector) sin contradecir los numéricos del JSON, que son los reales de este negocio.
            
            Datos (JSON) — fuente de verdad de este local:
            {dataJson}
            {bloqueWeb}
            
            Respondé con:
            1. Resumen ejecutivo: total facturado, ticket promedio, tendencia general.
            2. Top 5 productos más vendidos y qué dice eso sobre el comportamiento del cliente.
            3. Días con mayor y menor ventas, y posible causa.
            4. Recomendaciones: ¿qué artículos conviene destacar, promocionar o reponer con más frecuencia?
            5. Alertas: ¿hay caídas de ventas preocupantes o artículos del top que tienen bajo stock?
            
            Texto en español, claro y profesional.{instrExtra}
            """;

        var texto = await LlamarDeepSeekAsync(prompt, 3500);

        return new AiRespuesta
        {
            Exito = texto != null,
            Texto = texto ?? "No se pudo conectar con el servicio de IA.",
            AnalisisVentas = analisis,
            BusquedaWebAplicada = buscarEnWeb
        };
    }

    // ─── Consulta libre ───────────────────────────────────────────────────────

    public async Task<AiRespuesta> ConsultaLibreAsync(string pregunta, IReadOnlyList<AiChatMensaje>? historial = null, bool buscarEnWeb = false)
    {
        // Contexto general de la empresa para dar a la IA (se refrescá en cada turno)
        var totalArticulos = await db.Articulos.CountAsync(a => a.Activo);
        var stockBajo = await db.Articulos.CountAsync(a => a.Activo && a.StockActual <= a.StockMinimo);
        var ventasHoy = await db.Comprobantes
            .Where(c => c.Fecha.Date == DateTime.UtcNow.Date && c.Estado == EstadoComprobante.Emitido)
            .SumAsync(c => (decimal?)c.Total) ?? 0;
        var ventasMes = await db.Comprobantes
            .Where(c => c.Fecha >= DateTime.UtcNow.AddDays(-30) && c.Estado == EstadoComprobante.Emitido)
            .SumAsync(c => (decimal?)c.Total) ?? 0;
        var vencenSemana = await db.TrazabilidadEventos
            .CountAsync(t => t.FechaVencimiento.HasValue
                          && t.FechaVencimiento.Value >= DateTime.UtcNow
                          && t.FechaVencimiento.Value <= DateTime.UtcNow.AddDays(7)
                          && t.Cantidad > 0);
        var sucursales = await db.Sucursales.Where(s => s.Activo).Select(s => s.Nombre).ToListAsync();
        var articulosCoincidentes = await CargarArticulosCoincidentesAsync(pregunta);
        var resumenListasCompra = await db.ListasPrecioProveedor
            .AsNoTracking()
            .Where(l => l.Activo)
            .OrderByDescending(l => l.FechaCargaUtc)
            .Take(15)
            .Select(l => new
            {
                l.Id,
                l.Nombre,
                l.FechaCargaUtc,
                l.IdProveedor,
                proveedor = l.Proveedor == null ? null : l.Proveedor.RazonSocial,
                lineas = l.Lineas.Count()
            })
            .ToListAsync();

        var contexto = new
        {
            empresa = NombreEmpresa,
            totalArticulosActivos = totalArticulos,
            articulosConStockBajo = stockBajo,
            ventasHoy = ventasHoy,
            ventasUltimos30Dias = ventasMes,
            lotesVencenEn7Dias = vencenSemana,
            sucursales,
            listasCompraRecientes = resumenListasCompra,
            notaCompras = "Las `listasCompraRecientes` son listas de precio de **mayoristas** subidas a Compras (Excel, PDF, imagen o texto); no confundir con listas de precio al público. Para el detalle de ítems y bonificaciones, usá el módulo de tarifas de proveedor; si el usuario pide un precio de compra, cruzá con la lista más reciente de ese proveedor en este resumen o indicá qué vincular.",
            articulosCoincidentesConDescripcion
                = articulosCoincidentes
        };

        var contextoJson = JsonSerializer.Serialize(contexto, _json);

        // Memoria del hilo: cantidad ampliable en appsettings (cada 2 msg ≈ 1 turno)
        var maxHist = MaxMensajesHistorialChat();
        var pasado = (historial ?? Array.Empty<AiChatMensaje>())
            .Where(m => !string.IsNullOrWhiteSpace(m.Contenido))
            .TakeLast(maxHist)
            .ToList();

        var quisoCompararMercado = SujiereBusquedaCompetitiva(pregunta);
        var buscarEnWebEfectivo = buscarEnWeb || quisoCompararMercado;
        var bloqueWeb = "";
        if (buscarEnWebEfectivo)
        {
            var toksQ = ExtraerTerminosBusqueda(pregunta);
            string queryBus;
            if (buscarEnWeb)
            {
                queryBus = toksQ.Count > 0
                    ? string.Join(" ", toksQ.Take(5)) + " Argentina supermercado retail 2025 noticias"
                    : (pregunta.Length > 400 ? pregunta[..400] : pregunta);
            }
            else
            {
                queryBus = toksQ.Count > 0
                    ? string.Join(" ", toksQ.Take(4)) + " precio supermercado Argentina"
                    : pregunta;
            }

            var resWeb = await webSearch.BuscarResumenWebAsync(queryBus);
            if (string.IsNullOrWhiteSpace(resWeb) && !string.Equals(queryBus, pregunta, StringComparison.Ordinal))
                resWeb = await webSearch.BuscarResumenWebAsync(pregunta);
            if (string.IsNullOrWhiteSpace(resWeb) && buscarEnWeb)
                resWeb = await webSearch.BuscarResumenWebAsync("supermercado Argentina noticias retail inflación 2025");

            if (string.IsNullOrWhiteSpace(resWeb))
                bloqueWeb = """

                  Búsqueda en internet: no se obtuvieron resultados (DuckDuckGo sin clave a menudo es limitado). Para comparar con competidores, noticias y referencias: configurá en la API `WebSearch:TavilyApiKey` o `WebSearch:BingApiKey` en appsettings. Los precios de **tu** local siempre en el JSON del POS; la web aporta contexto, no cifra precisa de caja.
                  """;
            else
                bloqueWeb = $"""

                  Búsqueda en internet: activa (checkbox o intención de competencia, mercado, leyes, noticias, inflación, etc.). Usá el resumen para: comparar cadenas, marcas, tendencias, contexto reglamentario o prensa. No reemplazá los datos de ventas/stock de la base. Cruzá con articulosCoincidentesConDescripcion si aplica. Los PVP reales de tu comercio están en ese JSON, no en la web.
                  --- RESUMEN BÚSQUEDA PÚBLICA ---
                  {resWeb}
                  --- FIN RESUMEN ---
                  """;

            if (quisoCompararMercado && (articulosCoincidentes is null || articulosCoincidentes.Count == 0))
                bloqueWeb += "\nPista: no hay filas en articulosCoincidentesConDescripcion; pedile que cite productos o marcas para vincular precios con el inventario.\n";
        }

        var systemContent = $"""
            Sos el asistente inteligente de {NombreEmpresa}, un sistema POS de supermercado.
            Tenés acceso al siguiente resumen del estado actual del negocio (actualizado en este turno):
            {contextoJson}
            {bloqueWeb}
            Incluí `listasCompraRecientes` y `notaCompras` cuando hable de pedidos, precios a proveedor, tarifas mayoristas o ofertas por volumen.
            Respondé de forma precisa, útil y en español. Si hace falta dato de base que no tenés, decilo y sugerí qué abrir en el sistema.
            El historial de chat que recibís es **este hilo** (esta conversación): conservá continuidad, reglas o recomendaciones que el usuario haya fijado en turnos anteriores (criterios de márgenes, prioridades, proveedores, stock de góndola, acuerdos), y conectalos con la pregunta actual.
            Si recibiste un resumen de internet, podés usarlo para: competencia, referencias, noticias del sector, inflación, normas generales, tendencias. No atribuyas a la base local datos que solo vengan de la web.
            """;

        var texto = await LlamarDeepSeekConHistorialAsync(systemContent, pasado, pregunta);

        return new AiRespuesta
        {
            Exito = !string.IsNullOrWhiteSpace(texto),
            Texto = string.IsNullOrWhiteSpace(texto) ? (texto ?? "No se pudo obtener respuesta de la IA. Revisá la clave y el saldo de DeepSeek, o reintentá.") : texto!,
            Error = string.IsNullOrWhiteSpace(texto) ? "Respuesta vacía o error al llamar a la API de DeepSeek." : null,
            BusquedaWebAplicada = buscarEnWebEfectivo
        };
    }

    // ─── Llamada interna a DeepSeek ───────────────────────────────────────────

    private async Task<string?> LlamarDeepSeekAsync(string userMessage, int maxOutTokens = 2000)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.LogWarning("DeepSeek: ApiKey no configurada. Configure DeepSeek:ApiKey en appsettings.");
            return "⚠️ La IA no está configurada. Agregá tu API Key de DeepSeek en appsettings.json (DeepSeek:ApiKey).";
        }

        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(60);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var maxTok = Math.Clamp(maxOutTokens, 500, 8192);
            var body = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = $"Sos un asistente experto en gestión de supermercados y puntos de venta para {NombreEmpresa}. Siempre respondés en español argentino, de forma clara, concisa y profesional." },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.3,
                max_tokens = maxTok
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{BaseUrl}/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogError("DeepSeek error {Status}: {Body}", response.StatusCode, err);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al llamar a DeepSeek API");
            return null;
        }
    }

    /// <summary>System + turnos anteriores (user/assistant) + mensaje actual del usuario.</summary>
    private async Task<string?> LlamarDeepSeekConHistorialAsync(string systemContent, IReadOnlyList<AiChatMensaje> historial, string preguntaActual)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.LogWarning("DeepSeek: ApiKey no configurada. Configure DeepSeek:ApiKey en appsettings.");
            return "⚠️ La IA no está configurada. Agregá tu API Key de DeepSeek en appsettings.json (DeepSeek:ApiKey).";
        }

        var messages = new List<object>
        {
            new { role = "system", content = systemContent }
        };

        foreach (var m in historial)
        {
            var role = m.Rol.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            messages.Add(new { role, content = m.Contenido });
        }

        messages.Add(new { role = "user", content = preguntaActual });

        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(90);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var body = new
            {
                model = Model,
                messages = messages,
                temperature = 0.35,
                max_tokens = 4096
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{BaseUrl}/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogError("DeepSeek error {Status}: {Body}", response.StatusCode, err);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.GetProperty("choices").GetArrayLength() == 0)
                return "La API de DeepSeek devolvió una lista vacía de respuestas. Reintentá.";

            var contentEl = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
            if (contentEl.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return "La API de DeepSeek no devolvió texto de mensaje. Revisá el saldo o reintentá.";
            var raw = contentEl.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return "La API de DeepSeek devolvió texto vacío. Revisá el saldo de la cuenta, la clave, o reintentá en unos minutos.";
            return raw.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al llamar a DeepSeek API (con historial)");
            return null;
        }
    }

    // ─── Consulta libre: artículos y detección de "comparar con mercado" ───────

    private static readonly HashSet<string> _stopPregunta = new(StringComparer.OrdinalIgnoreCase)
    {
        "con", "del", "las", "los", "una", "uno", "por", "que", "como", "cual", "cual", "cuyo", "this", "the", "and", "or",
        "que", "son", "mis", "del", "esta", "este", "esto", "hay", "sea", "han", "tan", "mas", "cada", "hace", "toda", "todos", "cualquier"
    };

    private static bool SujiereBusquedaCompetitiva(string? p)
    {
        if (string.IsNullOrWhiteSpace(p)) return false;
        ReadOnlySpan<string> clave =
        [
            "competid", "otras cadenas", "otros super", "otros comerci", "mercado public", "cadenas de super", "jumbo", "coto ", "coto digital", "walmart", "vital", "carrefour", "vea", "farma", "chino", "mayorista", "en internet", "en la web", "en google", "noticia", "noticias", "inflac", "ipc", "indec", "afip", " retenci", " ley 17", "nueva ley", "came", "alimentos hoy", "a cuanto vende", "a cuánto vende", "a cuanto cobr", "a cuánto cobr", "precio en la calle", "precio de la competencia", "rango de precio", "cuánto cuesta hoy", "remercar", "benchmark", "referencia externa", "categoría lider", "coto digital", "rappi", "pedidos ya", "más caro", "más barato", "disco ", "coto y"
        ];
        foreach (var s in clave)
        {
            if (p.Contains(s, StringComparison.OrdinalIgnoreCase)) return true;
        }
        if (p.Contains("comparar", StringComparison.OrdinalIgnoreCase)
            && (p.Contains("mercado", StringComparison.OrdinalIgnoreCase)
                || p.Contains("cadenas", StringComparison.OrdinalIgnoreCase)
                || p.Contains("otro", StringComparison.OrdinalIgnoreCase)
                || p.Contains("otros", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (p.Contains("internet", StringComparison.OrdinalIgnoreCase) || p.Contains("web", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static List<string> ExtraerTerminosBusqueda(string p)
    {
        var toks = p
            .Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '\n', '\r', '«', '»' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('\'', '"', '(', ')', '¿', '¡'))
            .Where(w => w.Length >= 3 && !_stopPregunta.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        return toks;
    }

    private sealed record ArticuloResumenIa(int Id, string Descripcion, decimal PrecioVenta, decimal PrecioOferta, decimal StockActual, string CodigoBarras);

    private async Task<List<ArticuloResumenIa>?> CargarArticulosCoincidentesAsync(string pregunta)
    {
        var toks = ExtraerTerminosBusqueda(pregunta);
        if (toks.Count == 0) return null;
        IQueryable<Articulo> q = db.Articulos.AsNoTracking().Where(a => a.Activo);
        if (toks.Count == 1)
            q = q.Where(a => a.Descripcion.ToLower().Contains(toks[0].ToLower()));
        else if (toks.Count == 2)
        {
            var a0 = toks[0].ToLower();
            var a1 = toks[1].ToLower();
            q = q.Where(b => b.Descripcion.ToLower().Contains(a0) || b.Descripcion.ToLower().Contains(a1));
        }
        else
        {
            var a0 = toks[0].ToLower();
            var a1 = toks[1].ToLower();
            var a2 = toks[2].ToLower();
            q = q.Where(b => b.Descripcion.ToLower().Contains(a0) || b.Descripcion.ToLower().Contains(a1) || b.Descripcion.ToLower().Contains(a2));
        }

        var list = await q
            .OrderBy(x => x.Descripcion.Length)
            .Take(20)
            .Select(x => new ArticuloResumenIa(x.Id, x.Descripcion, x.PrecioVenta, x.PrecioOferta, x.StockActual, x.CodigoBarras))
            .ToListAsync();
        return list.Count == 0 ? null : list;
    }

    private static void AplicarVelocidadCobertura(AiSugerenciaCompra s, decimal vendido, int diasD)
    {
        s.VelocidadVentaDiaria = Math.Round(vendido / diasD, 4, MidpointRounding.AwayFromZero);
        if (s.VelocidadVentaDiaria > 0.0001m)
        {
            var c = s.StockActual / s.VelocidadVentaDiaria;
            s.CoberturaDiasAproximada = c > 10_000 ? 10_000 : (int)decimal.Floor(c);
        }
        else
            s.CoberturaDiasAproximada = null;
    }

    /// <summary>Completa con artículos que venden en el periodo, stock por encima del mínimo y bajo el máximo (hasta llenar el tope).</summary>
    private async Task CompletarSugerenciasRotacionAsync(
        List<AiSugerenciaCompra> sugerencias, int tope, DateTime fechaDesde, int diasD)
    {
        if (sugerencias.Count >= tope) return;
        var ya = sugerencias.Select(s => s.IdArticulo).ToHashSet();
        var cupo = tope - sugerencias.Count;
        if (cupo <= 0) return;

        var topV = await db.ComprobantesDetalle
            .AsNoTracking()
            .Where(d => d.Comprobante != null
                && d.Comprobante.Fecha >= fechaDesde
                && d.Comprobante.Estado != EstadoComprobante.Anulado)
            .GroupBy(d => d.IdArticulo)
            .Select(g => new { g.Key, Q = g.Sum(d => d.Cantidad) })
            .Where(x => x.Q > 0)
            .OrderByDescending(x => x.Q)
            .Take(2000)
            .ToListAsync();

        if (topV.Count == 0) return;

        var idsTodo = topV.Select(t => t.Key).ToList();
        var arts = await db.Articulos
            .AsNoTracking()
            .Where(a => a.Activo
                && idsTodo.Contains(a.Id)
                && !ya.Contains(a.Id)
                && a.StockActual > a.StockMinimo
                && a.StockActual < a.StockMaximo)
            .Include(a => a.Proveedor)
            .ToListAsync();
        if (arts.Count == 0) return;

        var qMap = topV.ToDictionary(t => t.Key, t => t.Q);
        var artsPorId = arts.ToDictionary(a => a.Id);
        var orden = topV.Select(t => t.Key).Where(artsPorId.ContainsKey).ToList();
        var sems = (decimal)config.GetValue("DeepSeek:RotacionCompraSemanasTope", 3);
        if (sems < 0.5m) sems = 3m;

        foreach (var id in orden)
        {
            if (sugerencias.Count >= tope) break;
            if (!artsPorId.TryGetValue(id, out var a)) continue;
            var vendido = qMap.GetValueOrDefault(id, 0);
            var vel = vendido / Math.Max(1, diasD);
            var capVel = (decimal)Math.Ceiling(vel * (sems * 7m));
            var capEspacio = a.StockMaximo - a.StockActual;
            var cant = Math.Max(0, capEspacio);
            if (vel > 0) cant = Math.Min(cant, capVel);
            if (cant <= 0) continue;

            var s = new AiSugerenciaCompra
            {
                IdArticulo = a.Id,
                Descripcion = a.Descripcion,
                CodigoBarras = a.CodigoBarras,
                StockActual = a.StockActual,
                StockMinimo = a.StockMinimo,
                StockMaximo = a.StockMaximo,
                CantidadSugerida = Math.Round(cant, 0, MidpointRounding.AwayFromZero),
                CantidadVendida30Dias = vendido,
                IdProveedor = a.IdProveedor,
                Proveedor = a.Proveedor?.RazonSocial ?? $"Proveedor #{a.IdProveedor}",
                PrecioCosto = a.PrecioCosto,
                TotalEstimado = Math.Round(cant, 0) * a.PrecioCosto,
                AlicuotaIva = a.AlicuotaIva,
                Prioridad = "Baja",
                OrigenSugerencia = "Rotación"
            };
            AplicarVelocidadCobertura(s, vendido, diasD);
            sugerencias.Add(s);
        }
    }

    /// <summary>Relaciona sugerencias con la última lista de precio de compra por proveedor (línea ya vinculada a artículo).</summary>
    private async Task RellenarTarifasCompraEnSugerenciasAsync(List<AiSugerenciaCompra> sugerencias)
    {
        if (sugerencias.Count == 0) return;
        var provIds = sugerencias.Select(s => s.IdProveedor).Distinct().ToList();
        var artIds = sugerencias.Select(s => s.IdArticulo).ToHashSet();

        var candidatos = await db.ListasPrecioProveedor
            .AsNoTracking()
            .Where(l => l.Activo && provIds.Contains(l.IdProveedor))
            .Select(l => new { l.Id, l.IdProveedor, l.Nombre, l.FechaCargaUtc })
            .ToListAsync();

        var bestPerProv = candidatos
            .GroupBy(c => c.IdProveedor)
            .Select(g => g.OrderByDescending(x => x.FechaCargaUtc).First())
            .ToList();
        if (bestPerProv.Count == 0) return;

        var listaIds = bestPerProv.Select(b => b.Id).ToList();
        var lineas = await db.ListasPrecioProveedorLineas
            .AsNoTracking()
            .Where(x => listaIds.Contains(x.IdLista) && x.IdArticulo != null && artIds.Contains(x.IdArticulo.Value))
            .Select(x => new
            {
                x.IdLista,
                IdArticulo = x.IdArticulo!.Value,
                x.PrecioUnitario,
                x.BonificacionesJson
            })
            .ToListAsync();

        var lineaPorProvArt = new Dictionary<(int IdProveedor, int IdArticulo), (decimal P, string? Bon, string Nombre, DateTime F)>();
        foreach (var b in bestPerProv)
        {
            foreach (var ln in lineas.Where(l => l.IdLista == b.Id))
            {
                var key = (b.IdProveedor, ln.IdArticulo);
                lineaPorProvArt[key] = (ln.PrecioUnitario, ResumirBonifJson(ln.BonificacionesJson), b.Nombre, b.FechaCargaUtc);
            }
        }

        foreach (var s in sugerencias)
        {
            if (!lineaPorProvArt.TryGetValue((s.IdProveedor, s.IdArticulo), out var t)) continue;
            s.PrecioListaCompraReciente = t.P;
            s.NombreTarifaCompra = t.Nombre;
            s.FechaTarifaCompra = t.F;
            s.BonifTarifaCompra = t.Bon;
        }
    }

    private static string? ResumirBonifJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return null;
        return json.Length > 200 ? string.Concat(json.AsSpan(0, 197), "...") : json;
    }
}
