using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComprasController(SuperPOSDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] EstadoCompra? estado, [FromQuery] int? idProveedor, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = db.Compras.Include(c => c.Proveedor).AsQueryable();
        if (desde.HasValue) q = q.Where(c => c.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(c => c.Fecha <= hasta.Value.ToUtc().AddDays(1));
        if (estado.HasValue) q = q.Where(c => c.Estado == estado);
        if (idProveedor.HasValue) q = q.Where(c => c.IdProveedor == idProveedor.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.Fecha).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var compra = await db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(c => c.Id == id);
        return compra is null ? NotFound() : Ok(compra);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Compra compra)
    {
        compra.Fecha = compra.Fecha == default ? DateTime.UtcNow : compra.Fecha.ToUtc();
        compra.Estado = EstadoCompra.Pendiente;
        foreach (var det in compra.Detalles)
        {
            det.PrecioCostoNeto = det.PrecioCosto * (1 - det.Bonificacion / 100);
            det.SubTotal = det.Cantidad * det.PrecioCostoNeto * (1 + det.AlicuotaIva / 100);
        }
        compra.SubTotal = compra.Detalles.Sum(d => d.Cantidad * d.PrecioCostoNeto);
        compra.TotalIva = compra.Detalles.Sum(d => d.Cantidad * d.PrecioCostoNeto * (d.AlicuotaIva / 100));
        compra.Total = compra.SubTotal + compra.TotalIva;

        // Estimacion provisoria del vencimiento de pago (se recalcula al recibirla).
        var proveedor = await db.Proveedores.FindAsync(compra.IdProveedor);
        compra.FechaVencimiento = compra.Fecha.AddDays(proveedor?.DiasVencimientoPago ?? 0);

        db.Compras.Add(compra);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = compra.Id }, compra);
    }

    [HttpPost("{id}/recibir")]
    public async Task<IActionResult> Recibir(long id)
    {
        var compra = await db.Compras.Include(c => c.Detalles).FirstOrDefaultAsync(c => c.Id == id);
        if (compra is null) return NotFound();
        if (compra.Estado == EstadoCompra.Recibida) return BadRequest("Ya fue recibida");

        var idDestino = await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db) ?? 1;

        foreach (var det in compra.Detalles)
        {
            var art = await db.Articulos.FindAsync(det.IdArticulo);
            if (art is null) continue;
            await StockSucursalHelper.AplicarMovimientoAsync(db, det.IdArticulo, idDestino, det.Cantidad);
            if (det.ActualizaPrecio)
            {
                art.PrecioCosto = det.PrecioCostoNeto;
                // Recalcular precio de venta manteniendo el margen
                if (art.MargenGanancia > 0)
                    art.PrecioVenta = Math.Round(det.PrecioCostoNeto * (1 + art.AlicuotaIva / 100) * (1 + art.MargenGanancia / 100), 2);
            }
        }

        compra.Estado = EstadoCompra.Recibida;

        var proveedor = await db.Proveedores.FindAsync(compra.IdProveedor);
        if (proveedor != null)
            compra.FechaVencimiento = DateTime.UtcNow.AddDays(proveedor.DiasVencimientoPago);

        if (proveedor != null && compra.Total > 0)
        {
            proveedor.SaldoCtaCte += compra.Total;
            db.MovimientosCtaCteProveedor.Add(new MovimientoCtaCteProveedor
            {
                IdProveedor = compra.IdProveedor,
                Fecha = DateTime.UtcNow,
                Tipo = TipoMovimientoCteProveedor.CompraCredito,
                Concepto = $"Compra {compra.LetraFactura}{compra.NumeroFactura}".Trim(),
                IdCompra = compra.Id,
                Debe = compra.Total,
                Haber = 0,
                SaldoAcumulado = proveedor.SaldoCtaCte
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Compras recibidas y pendientes de pago, ordenadas por fecha de vencimiento.</summary>
    [HttpGet("calendario-pagos")]
    public async Task<IActionResult> CalendarioPagos([FromQuery] int? idProveedor)
    {
        var q = db.Compras
            .Include(c => c.Proveedor)
            .Where(c => c.Estado == EstadoCompra.Recibida && !c.Pagada && c.Conciliada);
        if (idProveedor.HasValue) q = q.Where(c => c.IdProveedor == idProveedor.Value);

        var hoy = DateTime.UtcNow.Date;
        var items = await q.OrderBy(c => c.FechaVencimiento).Select(c => new
        {
            c.Id,
            c.NumeroFactura,
            c.LetraFactura,
            c.Fecha,
            c.FechaVencimiento,
            c.Total,
            IdProveedor = c.IdProveedor,
            Proveedor = c.Proveedor!.RazonSocial,
        }).ToListAsync();

        var resultado = items.Select(c => new
        {
            c.Id,
            c.NumeroFactura,
            c.LetraFactura,
            c.Fecha,
            c.FechaVencimiento,
            c.Total,
            c.IdProveedor,
            c.Proveedor,
            DiasParaVencer = c.FechaVencimiento.HasValue ? (c.FechaVencimiento.Value.Date - hoy).Days : (int?)null,
            Vencida = c.FechaVencimiento.HasValue && c.FechaVencimiento.Value.Date < hoy
        });

        return Ok(resultado);
    }

    [HttpPost("{id}/anular")]
    public async Task<IActionResult> Anular(long id)
    {
        var compra = await db.Compras.FindAsync(id);
        if (compra is null) return NotFound();
        if (compra.Estado == EstadoCompra.Recibida) return BadRequest("No se puede anular una compra ya recibida");
        compra.Estado = EstadoCompra.Anulada;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adjunta (o reemplaza) el PDF/foto de la factura real del proveedor a una Compra ya cargada.</summary>
    [HttpPost("{id}/factura-archivo")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> SubirFacturaArchivo(long id, IFormFile file, CancellationToken ct)
    {
        var compra = await db.Compras.FindAsync([id], ct);
        if (compra is null) return NotFound();
        if (file is not { Length: > 0 }) return BadRequest(new { error = "Adjuntá un archivo." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dir = Path.Combine(env.ContentRootPath, "Data", "uploads", "facturas-compra");
        Directory.CreateDirectory(dir);

        // Si ya tenía un archivo adjunto, se reemplaza (se borra el anterior).
        if (!string.IsNullOrEmpty(compra.ArchivoFacturaRutaRelativa))
        {
            var anterior = Path.Combine(env.ContentRootPath, compra.ArchivoFacturaRutaRelativa.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(anterior)) System.IO.File.Delete(anterior);
        }

        var safeName = $"{Guid.NewGuid():N}{ext}";
        var rel = Path.Combine("Data", "uploads", "facturas-compra", safeName);
        var full = Path.Combine(env.ContentRootPath, rel);
        await using (var fs = System.IO.File.Create(full))
            await file.CopyToAsync(fs, ct);

        compra.ArchivoFacturaNombre = file.FileName;
        compra.ArchivoFacturaRutaRelativa = rel.Replace("\\", "/");
        await db.SaveChangesAsync(ct);

        return Ok(new { compra.ArchivoFacturaNombre });
    }

    [HttpGet("{id}/factura-archivo")]
    public async Task<IActionResult> DescargarFacturaArchivo(long id)
    {
        var compra = await db.Compras.FindAsync(id);
        if (compra?.ArchivoFacturaRutaRelativa is null) return NotFound();

        var full = Path.Combine(env.ContentRootPath, compra.ArchivoFacturaRutaRelativa.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!System.IO.File.Exists(full)) return NotFound();

        new FileExtensionContentTypeProvider().TryGetContentType(full, out var mime);
        return PhysicalFile(full, mime ?? "application/octet-stream", compra.ArchivoFacturaNombre ?? Path.GetFileName(full));
    }

    /// <summary>
    /// Importa en lote las facturas de compra desde el CSV que se descarga del Portal IVA de AFIP
    /// ("Mis Comprobantes" &gt; Comprobantes recibidos &gt; Descargar comprobantes,
    /// nombre de archivo tipo comprobantes_periodo_aaaamm_compras_aaaammdd_nnnn.csv).
    /// Es la misma idea que ya tenía Gecom: conciliar contra lo que el proveedor declaró ante AFIP,
    /// en vez de cargar cada factura a mano. Formato semicolon-separated con encabezado en español.
    /// </summary>
    [HttpPost("importar-portal-iva")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportarPortalIva(IFormFile file, CancellationToken ct)
    {
        if (file is not { Length: > 0 }) return BadRequest(new { error = "Adjuntá el CSV descargado del Portal IVA de AFIP." });

        using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8);
        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null) return BadRequest(new { error = "El archivo está vacío." });

        var headers = headerLine.Split(';').Select(h => h.Trim().Trim('"')).ToList();
        int Col(string nombre)
        {
            var idx = headers.FindIndex(h => string.Equals(h, nombre, StringComparison.OrdinalIgnoreCase));
            return idx;
        }

        var colFecha = Col("Fecha de Emisión");
        var colTipo = Col("Tipo de Comprobante");
        var colPtoVta = Col("Punto de Venta");
        var colNroDesde = Col("Número Desde");
        var colTipoDocEmisor = Col("Tipo Doc. Emisor");
        var colNroDocEmisor = Col("Nro. Doc. Emisor");
        var colDenomEmisor = Col("Denominación Emisor");
        var colNetoGravado = Col("Imp. Neto Gravado");
        var colNetoNoGravado = Col("Imp. Neto No Gravado");
        var colExentas = Col("Imp. Op. Exentas");
        var colOtrosTributos = Col("Otros Tributos");
        var colIva = Col("IVA");
        var colTotal = Col("Imp. Total");

        var columnasFaltantes = new[]
        {
            ("Fecha de Emisión", colFecha), ("Tipo de Comprobante", colTipo), ("Punto de Venta", colPtoVta),
            ("Número Desde", colNroDesde), ("Nro. Doc. Emisor", colNroDocEmisor), ("Imp. Total", colTotal)
        }.Where(c => c.Item2 < 0).Select(c => c.Item1).ToList();
        if (columnasFaltantes.Count > 0)
            return UnprocessableEntity(new
            {
                error = "El archivo no tiene el formato esperado del Portal IVA de AFIP.",
                columnasFaltantes,
                columnasEncontradas = headers
            });

        var cultura = CultureInfo.GetCultureInfo("es-AR");
        decimal ParseImporte(string[] campos, int col) =>
            col >= 0 && col < campos.Length && decimal.TryParse(campos[col].Trim(), NumberStyles.Number, cultura, out var v) ? v : 0;

        var errores = new List<object>();
        var creadas = new List<Compra>();
        var nroFila = 1;

        while (await reader.ReadLineAsync(ct) is { } linea)
        {
            nroFila++;
            if (string.IsNullOrWhiteSpace(linea)) continue;
            var campos = linea.Split(';').Select(c => c.Trim().Trim('"')).ToArray();

            try
            {
                var cuitEmisor = campos[colNroDocEmisor].Trim();
                var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Cuit == cuitEmisor, ct);
                if (proveedor is null)
                {
                    errores.Add(new { fila = nroFila, motivo = $"No existe un proveedor cargado con CUIT {cuitEmisor}" +
                        (colDenomEmisor >= 0 ? $" ({campos[colDenomEmisor]})" : "") });
                    continue;
                }

                if (!int.TryParse(campos[colTipo].Trim(), out var codigoAfipTipo))
                {
                    errores.Add(new { fila = nroFila, motivo = $"Tipo de comprobante inválido: '{campos[colTipo]}'" });
                    continue;
                }
                var tipoCbte = await db.TiposComprobante.FirstOrDefaultAsync(t => t.CodigoAfip == codigoAfipTipo, ct);
                if (tipoCbte is null)
                {
                    errores.Add(new { fila = nroFila, motivo = $"No se reconoce el tipo de comprobante AFIP código {codigoAfipTipo}" });
                    continue;
                }

                if (!DateTime.TryParseExact(campos[colFecha].Trim(), "dd/MM/yyyy", cultura, DateTimeStyles.None, out var fecha))
                {
                    errores.Add(new { fila = nroFila, motivo = $"Fecha inválida: '{campos[colFecha]}'" });
                    continue;
                }

                var neto = ParseImporte(campos, colNetoGravado);
                var noGravado = ParseImporte(campos, colNetoNoGravado);
                var exento = ParseImporte(campos, colExentas);
                var otrosTributos = ParseImporte(campos, colOtrosTributos);
                var iva = ParseImporte(campos, colIva);
                // El total lo recalculamos en base a la suma de importes, igual que hace Gecom, porque el
                // "Imp. Total" que provee AFIP a veces difiere levemente de la suma real de detalle+impuestos.
                var totalCalculado = neto + noGravado + exento + otrosTributos + iva;

                var yaExiste = await db.Compras.AnyAsync(c =>
                    c.IdProveedor == proveedor.Id && c.IdTipoComprobante == tipoCbte.Id &&
                    c.NumeroFactura == campos[colNroDesde].Trim() && c.PuntoVentaProveedor.ToString() == campos[colPtoVta].Trim(), ct);
                if (yaExiste)
                {
                    errores.Add(new { fila = nroFila, motivo = $"Ya existe una compra cargada para esta factura (proveedor {proveedor.RazonSocial}, {campos[colNroDesde]})" });
                    continue;
                }

                var compra = new Compra
                {
                    IdProveedor = proveedor.Id,
                    Fecha = fecha.ToUtc(),
                    NumeroFactura = campos[colNroDesde].Trim(),
                    LetraFactura = tipoCbte.Nombre.Trim().Split(' ').Last(),
                    IdTipoComprobante = tipoCbte.Id,
                    PuntoVentaProveedor = int.TryParse(campos[colPtoVta].Trim(), out var pv) ? pv : 0,
                    Estado = EstadoCompra.Pendiente,
                    SubTotal = neto + noGravado + exento,
                    TotalIva = iva,
                    ImporteNoGravado = noGravado,
                    ImporteExento = exento,
                    Total = totalCalculado,
                    Observaciones = "Importada desde Portal IVA AFIP"
                };
                var vencProveedor = proveedor.DiasVencimientoPago;
                compra.FechaVencimiento = compra.Fecha.AddDays(vencProveedor);

                db.Compras.Add(compra);
                creadas.Add(compra);
            }
            catch (Exception ex)
            {
                errores.Add(new { fila = nroFila, motivo = $"Error al procesar la fila: {ex.Message}" });
            }
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            totalFilas = nroFila - 1,
            importadas = creadas.Count,
            errores.Count,
            errores,
            compras = creadas.Select(c => new { c.Id, c.NumeroFactura, c.LetraFactura, c.Total })
        });
    }
}
