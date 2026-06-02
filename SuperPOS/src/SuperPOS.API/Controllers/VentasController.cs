using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SuperPOS.AFIP;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.API.Hubs;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController(SuperPOSDbContext db, IHubContext<PosHub> hub, AfipService afip, IConfiguration _config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = db.Comprobantes
            .Include(c => c.Cliente)
            .Include(c => c.TipoComprobante)
            .AsQueryable();

        if (desde.HasValue) q = q.Where(c => c.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(c => c.Fecha <= hasta.Value.ToUtc().AddDays(1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.Fecha).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var cbte = await db.Comprobantes
            .Include(c => c.Cliente)
            .Include(c => c.TipoComprobante)
            .Include(c => c.Detalles).ThenInclude(d => d.Articulo)
            .Include(c => c.Pagos).ThenInclude(p => p.MedioPago)
            .FirstOrDefaultAsync(c => c.Id == id);
        return cbte is null ? NotFound() : Ok(cbte);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] Comprobante cbte)
    {
        cbte.Fecha = DateTime.UtcNow;
        cbte.Estado = EstadoComprobante.Emitido;

        // Calcular número correlativo
        var ultimo = await db.Comprobantes
            .Where(c => c.PuntoVenta == cbte.PuntoVenta && c.IdTipoComprobante == cbte.IdTipoComprobante && c.Letra == cbte.Letra)
            .MaxAsync(c => (long?)c.Numero) ?? 0;
        cbte.Numero = ultimo + 1;

        // Stock por sucursal (caja / local) + trazabilidad
        var eventos = new List<TrazabilidadEvento>();
        foreach (var det in cbte.Detalles)
        {
            await StockSucursalHelper.AplicarMovimientoAsync(db, det.IdArticulo, cbte.IdSucursal, -det.Cantidad, permitirNegativo: true);
            var art = await db.Articulos.FindAsync(det.IdArticulo);
            if (art != null)
            {
                art.CantidadVendida += det.Cantidad;
                art.UltimaVenta = DateTime.UtcNow;
            }
        }

        db.Comprobantes.Add(cbte);
        await db.SaveChangesAsync();

        // Trazabilidad de venta (ya con IDs asignados)
        foreach (var det in cbte.Detalles)
        {
            eventos.Add(new TrazabilidadEvento
            {
                Fecha = cbte.Fecha,
                IdArticulo = det.IdArticulo,
                Cantidad = -det.Cantidad,
                Tipo = TipoTrazabilidadEvento.VentaCaja,
                Ubicacion = $"Caja {cbte.IdCaja}",
                IdUsuario = cbte.IdUsuario > 0 ? cbte.IdUsuario : null,
                IdComprobante = cbte.Id,
                IdComprobanteDetalle = det.Id,
                LoteNro = det.NroLote,
                NroSerie = det.NroSerie,
                FechaVencimiento = det.FechaVencimiento
            });
        }
        if (eventos.Count > 0)
        {
            db.TrazabilidadEventos.AddRange(eventos);
            await db.SaveChangesAsync();
        }

        // Solicitar CAE a AFIP si el tipo de comprobante lo requiere
        var tipoCbte = await db.TiposComprobante.FindAsync(cbte.IdTipoComprobante);
        if (tipoCbte?.RequiereCAE == true)
        {
            try
            {
                // Determinar datos del receptor para AFIP
                var cliente     = cbte.IdCliente > 0 ? await db.Clientes.FindAsync(cbte.IdCliente) : null;
                var cfg         = await db.ConfiguracionEmpresa.FirstOrDefaultAsync();
                var (tipoDoc, nroDoc) = ObtenerDatosDocumentoAfip(cliente);

                var neto = cbte.SubTotal - cbte.TotalDescuento;
                var solicitud = new SolicitudCAE
                {
                    PuntoVenta      = cbte.PuntoVenta,
                    TipoComprobante = AfipService.ObtenerTipoComprobanteAfip(cbte.Letra, TipoComprobanteAfip.Factura),
                    NroComprobante  = cbte.Numero,
                    Fecha           = cbte.Fecha,
                    Concepto        = 1,    // Productos (supermercado siempre vende productos)
                    TipoDocCliente  = tipoDoc,
                    NroDocCliente   = nroDoc,
                    ImporteNeto     = neto,
                    ImporteIva      = cbte.TotalIva21 + cbte.TotalIva105,
                    ImporteTotal    = cbte.Total,
                    Ivas            = ObtenerIvas(cbte)
                };

                var resultado = await afip.SolicitarCAEAsync(solicitud);
                if (resultado.Exito)
                {
                    cbte.CAE            = long.TryParse(resultado.CAE, out var caeNum) ? caeNum : null;
                    cbte.CAEVencimiento = resultado.FechaVencimientoCAE;
                    cbte.QrAfip         = resultado.GenerarQRAfip(
                        cfg?.Cuit ?? _config["Afip:CUIT"] ?? "",
                        cbte.PuntoVenta, solicitud.TipoComprobante, cbte.Fecha, cbte.Total,
                        tipoDoc, nroDoc);
                    await db.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine($"[AFIP] CAE rechazado: {resultado.Error}");
                }
            }
            catch (Exception ex)
            {
                // No interrumpir la venta si AFIP falla — se puede reintentar después
                Console.WriteLine($"[AFIP] Error al solicitar CAE: {ex.Message}");
            }
        }

        await hub.Clients.All.SendAsync("VentaRealizada", cbte.IdCaja, cbte.Total, cbte.Fecha);

        return CreatedAtAction(nameof(GetById), new { id = cbte.Id }, new
        {
            cbte.Id,
            cbte.Numero,
            cbte.Letra,
            cbte.PuntoVenta,
            cbte.Fecha,
            cbte.Total,
            cbte.CAE,
            cbte.CAEVencimiento,
            cbte.QrAfip,
            cbte.Estado
        });
    }

    /// <summary>
    /// Determina tipo de documento y número del receptor para AFIP.
    /// 80=CUIT, 96=DNI, 99=Consumidor Final
    /// </summary>
    private static (int tipoDoc, long nroDoc) ObtenerDatosDocumentoAfip(Cliente? cliente)
    {
        if (cliente is null || string.IsNullOrWhiteSpace(cliente.Cuit) || cliente.CondicionIva == 5)
            return (99, 0);  // Consumidor Final

        // Limpiar CUIT (quitar guiones)
        var cuitLimpio = cliente.Cuit.Replace("-", "").Replace(" ", "");
        if (cuitLimpio.Length == 11 && long.TryParse(cuitLimpio, out var cuit))
            return (80, cuit);  // CUIT

        // Si tiene DNI (8 dígitos)
        if (cuitLimpio.Length == 8 && long.TryParse(cuitLimpio, out var dni))
            return (96, dni);   // DNI

        return (99, 0);  // default Consumidor Final
    }

    private static List<AfipIva> ObtenerIvas(Comprobante cbte)
    {
        var lista = new List<AfipIva>();
        var neto = cbte.SubTotal - cbte.TotalDescuento;
        if (cbte.TotalIva21 > 0) lista.Add(new AfipIva { IdIva = 5, BaseImponible = neto, Importe = cbte.TotalIva21 });
        if (cbte.TotalIva105 > 0) lista.Add(new AfipIva { IdIva = 4, BaseImponible = neto, Importe = cbte.TotalIva105 });
        return lista;
    }

    /// <summary>Verifica el estado de los servidores AFIP.</summary>
    [HttpGet("afip/estado")]
    public async Task<IActionResult> EstadoAfip()
    {
        var status = await afip.FEDummyAsync();
        return Ok(status);
    }

    /// <summary>Reintenta solicitar CAE para un comprobante que no lo tiene aún.</summary>
    [HttpPost("{id}/solicitar-cae")]
    public async Task<IActionResult> SolicitarCAE(long id)
    {
        var cbte = await db.Comprobantes
            .Include(c => c.TipoComprobante)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cbte is null) return NotFound();
        if (cbte.CAE.HasValue) return BadRequest("Este comprobante ya tiene CAE asignado.");

        var tipoCbte = cbte.TipoComprobante;
        if (tipoCbte?.RequiereCAE != true)
            return BadRequest("Este tipo de comprobante no requiere CAE.");

        var cliente        = cbte.IdCliente > 0 ? await db.Clientes.FindAsync(cbte.IdCliente) : null;
        var cfg            = await db.ConfiguracionEmpresa.FirstOrDefaultAsync();
        var (tipoDoc, nroDoc) = ObtenerDatosDocumentoAfip(cliente);
        var neto = cbte.SubTotal - cbte.TotalDescuento;

        var solicitud = new SolicitudCAE
        {
            PuntoVenta      = cbte.PuntoVenta,
            TipoComprobante = AfipService.ObtenerTipoComprobanteAfip(cbte.Letra, TipoComprobanteAfip.Factura),
            NroComprobante  = cbte.Numero,
            Fecha           = cbte.Fecha,
            Concepto        = 1,
            TipoDocCliente  = tipoDoc,
            NroDocCliente   = nroDoc,
            ImporteNeto     = neto,
            ImporteIva      = cbte.TotalIva21 + cbte.TotalIva105,
            ImporteTotal    = cbte.Total,
            Ivas            = ObtenerIvas(cbte)
        };

        var resultado = await afip.SolicitarCAEAsync(solicitud);

        if (resultado.Exito)
        {
            cbte.CAE            = long.TryParse(resultado.CAE, out var caeNum) ? caeNum : null;
            cbte.CAEVencimiento = resultado.FechaVencimientoCAE;
            cbte.QrAfip         = resultado.GenerarQRAfip(
                cfg?.Cuit ?? _config["Afip:CUIT"] ?? "",
                cbte.PuntoVenta, solicitud.TipoComprobante, cbte.Fecha, cbte.Total, tipoDoc, nroDoc);
            await db.SaveChangesAsync();
            return Ok(new { cbte.CAE, cbte.CAEVencimiento, cbte.QrAfip, Observaciones = resultado.Observaciones });
        }

        return BadRequest(new { Error = resultado.Error });
    }

    [HttpPost("{id}/anular")]
    public async Task<IActionResult> Anular(long id, [FromQuery] int idUsuario)
    {
        var cbte = await db.Comprobantes.Include(c => c.Detalles).FirstOrDefaultAsync(c => c.Id == id);
        if (cbte is null) return NotFound();
        if (cbte.Estado == EstadoComprobante.Anulado) return BadRequest("Ya está anulado");

        cbte.Estado = EstadoComprobante.Anulado;
        cbte.FechaAnulacion = DateTime.UtcNow;
        cbte.IdUsuarioAnulacion = idUsuario;

        foreach (var det in cbte.Detalles)
            await StockSucursalHelper.AplicarMovimientoAsync(db, det.IdArticulo, cbte.IdSucursal, det.Cantidad, permitirNegativo: true);

        foreach (var det in cbte.Detalles)
        {
            var art = await db.Articulos.FindAsync(det.IdArticulo);
            if (art != null)
            {
                art.CantidadVendida -= det.Cantidad;
                if (art.CantidadVendida < 0) art.CantidadVendida = 0;
            }
        }

        foreach (var det in cbte.Detalles)
        {
            db.TrazabilidadEventos.Add(new TrazabilidadEvento
            {
                Fecha = DateTime.UtcNow,
                Tipo = TipoTrazabilidadEvento.AnulacionVenta,
                Cantidad = det.Cantidad,
                IdArticulo = det.IdArticulo,
                Ubicacion = $"Caja {cbte.IdCaja}",
                IdUsuario = idUsuario > 0 ? idUsuario : null,
                IdComprobante = cbte.Id,
                IdComprobanteDetalle = det.Id,
                LoteNro = det.NroLote,
                NroSerie = det.NroSerie,
                FechaVencimiento = det.FechaVencimiento
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }
}
