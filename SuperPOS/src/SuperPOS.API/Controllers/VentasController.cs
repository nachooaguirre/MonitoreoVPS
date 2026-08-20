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
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? idSucursal, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var permitidas = await SucursalScopeHelper.ObtenerPermitidasAsync(User, db);
        if (idSucursal.HasValue && permitidas != null && !permitidas.Contains(idSucursal.Value))
            return Forbid();

        var q = db.Comprobantes
            .Include(c => c.Cliente)
            .Include(c => c.TipoComprobante)
            .AsQueryable();

        if (desde.HasValue) q = q.Where(c => c.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(c => c.Fecha <= hasta.Value.ToUtc().AddDays(1));
        if (idSucursal.HasValue) q = q.Where(c => c.IdSucursal == idSucursal.Value);
        else if (permitidas != null) q = q.Where(c => permitidas.Contains(c.IdSucursal));

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

        // Stock por sucursal (caja / local) + trazabilidad
        var eventos = new List<TrazabilidadEvento>();
        var now = DateTime.UtcNow;

        // La numeración de comprobante (PuntoVenta+Tipo+Letra) debe ser estrictamente correlativa
        // y sin huecos/duplicados ante AFIP. Con dos cajas vendiendo al mismo instante, un simple
        // "MAX+1" puede calcular el mismo número dos veces. pg_advisory_xact_lock serializa —solo—
        // las ventas que comparten esa misma clave (otra caja/letra sigue vendiendo en paralelo sin
        // esperar), y el lock se libera solo al terminar esta transacción (no bloquea la llamada a AFIP,
        // que ocurre después, fuera de esta transacción).
        var lockKey = $"cbte:{cbte.IdSucursal}:{cbte.PuntoVenta}:{cbte.IdTipoComprobante}:{cbte.Letra}";
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");

            var ultimo = await db.Comprobantes
                .Where(c => c.PuntoVenta == cbte.PuntoVenta && c.IdTipoComprobante == cbte.IdTipoComprobante && c.Letra == cbte.Letra)
                .MaxAsync(c => (long?)c.Numero) ?? 0;
            cbte.Numero = ultimo + 1;

            foreach (var det in cbte.Detalles)
            {
                await StockSucursalHelper.AplicarMovimientoAsync(db, det.IdArticulo, cbte.IdSucursal, -det.Cantidad, permitirNegativo: true);
                var art = await db.Articulos.FindAsync(det.IdArticulo);
                if (art != null)
                {
                    art.CantidadVendida += det.Cantidad;
                    art.UltimaVenta = DateTime.UtcNow;
                }

                // Incrementar cantidad vendida en la oferta activa si existe
                var oferta = await db.Ofertas
                    .Where(o => o.IdArticulo == det.IdArticulo && o.Activa && o.FechaDesde <= now && o.FechaHasta >= now)
                    .FirstOrDefaultAsync();
                if (oferta != null && (oferta.LimiteStock == null || oferta.CantidadVendida < oferta.LimiteStock))
                {
                    oferta.CantidadVendida += det.Cantidad;
                }
            }

            db.Comprobantes.Add(cbte);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        await AplicarPagoCtaCteAsync(cbte);

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
                    TipoComprobante = ObtenerCodigoAfip(tipoCbte, cbte.Letra, cbte.Comision),
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
                await RegistrarLogAfipAsync(cbte.Id, resultado);
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
                await RegistrarLogAfipAsync(cbte.Id, null, ex.Message);
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

    /// <summary>Si la venta se pagó (total o parcialmente) con cuenta corriente, carga el saldo al cliente.</summary>
    private async Task AplicarPagoCtaCteAsync(Comprobante cbte)
    {
        if (cbte.IdCliente is not > 0 || cbte.Pagos.Count == 0) return;

        var idsMedioPago = cbte.Pagos.Select(p => p.IdMedioPago).Distinct().ToList();
        var mediosCtaCte = await db.MediosPago
            .Where(m => idsMedioPago.Contains(m.Id) && m.Tipo == TipoMedioPago.CtaCte)
            .Select(m => m.Id)
            .ToListAsync();
        if (mediosCtaCte.Count == 0) return;

        var monto = cbte.Pagos.Where(p => mediosCtaCte.Contains(p.IdMedioPago)).Sum(p => p.Importe);
        if (monto <= 0) return;

        var cliente = await db.Clientes.FindAsync(cbte.IdCliente);
        if (cliente is null) return;

        cliente.SaldoCtaCte += monto;
        cliente.EsMoroso = cliente.SaldoCtaCte > 0 && DateTime.UtcNow > (cliente.FechaVtoCtaCte ?? DateTime.MaxValue);

        db.MovimientosCtaCte.Add(new MovimientoCtaCte
        {
            IdCliente = cbte.IdCliente.Value,
            Fecha = cbte.Fecha,
            Tipo = TipoMovimientoCte.VentaCredito,
            Concepto = $"Venta {cbte.Letra} {cbte.PuntoVenta:0000}-{cbte.Numero:00000000}",
            IdComprobante = cbte.Id,
            Debe = monto,
            Haber = 0,
            SaldoAcumulado = cliente.SaldoCtaCte,
            IdUsuario = cbte.IdUsuario > 0 ? cbte.IdUsuario : null
        });

        await db.SaveChangesAsync();
    }

    /// <summary>Revierte el saldo de cuenta corriente cargado por una venta anulada, si corresponde.</summary>
    private async Task RevertirPagoCtaCteAsync(Comprobante cbte, int idUsuario)
    {
        var mov = await db.MovimientosCtaCte
            .Where(m => m.IdComprobante == cbte.Id && m.Tipo == TipoMovimientoCte.VentaCredito)
            .FirstOrDefaultAsync();
        if (mov is null) return;

        var cliente = await db.Clientes.FindAsync(mov.IdCliente);
        if (cliente is null) return;

        cliente.SaldoCtaCte -= mov.Debe;
        if (cliente.SaldoCtaCte < 0) cliente.SaldoCtaCte = 0;
        cliente.EsMoroso = cliente.SaldoCtaCte > 0 && DateTime.UtcNow > (cliente.FechaVtoCtaCte ?? DateTime.MaxValue);

        db.MovimientosCtaCte.Add(new MovimientoCtaCte
        {
            IdCliente = mov.IdCliente,
            Fecha = DateTime.UtcNow,
            Tipo = TipoMovimientoCte.NotaCredito,
            Concepto = $"Anulación venta {cbte.Letra} {cbte.PuntoVenta:0000}-{cbte.Numero:00000000}",
            IdComprobante = cbte.Id,
            Debe = 0,
            Haber = mov.Debe,
            SaldoAcumulado = cliente.SaldoCtaCte,
            IdUsuario = idUsuario > 0 ? idUsuario : null
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

    /// <summary>
    /// Determina tipo de documento y número del receptor para AFIP cuando el comprobante es
    /// una nota a un PROVEEDOR (en vez de a un cliente).
    /// </summary>
    private static (int tipoDoc, long nroDoc) ObtenerDatosDocumentoAfipProveedor(Proveedor? proveedor)
    {
        if (proveedor is null || string.IsNullOrWhiteSpace(proveedor.Cuit))
            return (99, 0);

        var cuitLimpio = proveedor.Cuit.Replace("-", "").Replace(" ", "");
        return cuitLimpio.Length == 11 && long.TryParse(cuitLimpio, out var cuit)
            ? (80, cuit)
            : (99, 0);
    }

    /// <summary>
    /// Código de comprobante AFIP a partir del tipo configurado (usa CodigoAfip si está seedeado;
    /// si no, lo deriva de la letra asumiendo Factura), aplicando FCE MiPyME si corresponde por comisión.
    /// </summary>
    private static int ObtenerCodigoAfip(TipoComprobante tipoCbte, char letra, decimal comision)
    {
        var codigoBase = tipoCbte.CodigoAfip ?? AfipService.ObtenerTipoComprobanteAfip(letra, TipoComprobanteAfip.Factura);
        return AfipService.AplicarFceSiCorresponde(codigoBase, comision);
    }

    /// <summary>Guarda en la bitácora el resultado (o el error) de una llamada a AFIP para un comprobante.</summary>
    private async Task RegistrarLogAfipAsync(long idComprobante, SolicitudCAEResult? resultado, string? errorExcepcion = null)
    {
        char estado;
        string? detalle;
        if (resultado is null)
        {
            estado = 'E';
            detalle = errorExcepcion;
        }
        else if (resultado.Exito)
        {
            estado = resultado.Recuperado ? 'C' : 'A';
            detalle = resultado.Observaciones;
        }
        else
        {
            estado = 'R';
            detalle = resultado.Error;
        }

        db.ComprobantesAfipLog.Add(new ComprobanteAfipLog
        {
            IdComprobante = idComprobante,
            Fecha = DateTime.UtcNow,
            Resultado = estado,
            Detalle = detalle,
            RequestXml = resultado?.RequestXml,
            ResponseXml = resultado?.ResponseXml
        });
        await db.SaveChangesAsync();
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
            TipoComprobante = ObtenerCodigoAfip(tipoCbte, cbte.Letra, cbte.Comision),
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
        await RegistrarLogAfipAsync(cbte.Id, resultado);

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

    /// <summary>Bitácora de llamadas a AFIP (request/response) para un comprobante — para soporte/depuración.</summary>
    [HttpGet("{id}/afip-log")]
    public async Task<IActionResult> GetAfipLog(long id) =>
        Ok(await db.ComprobantesAfipLog.Where(l => l.IdComprobante == id).OrderByDescending(l => l.Fecha).ToListAsync());

    /// <summary>Registra una nota de débito/crédito emitida a un PROVEEDOR (no a un cliente) reusando el mismo circuito de AFIP.</summary>
    [HttpPost("nota-proveedor")]
    public async Task<IActionResult> RegistrarNotaProveedor([FromBody] Comprobante cbte)
    {
        if (cbte.IdProveedor is not > 0)
            return BadRequest("IdProveedor es obligatorio para una nota a proveedor.");

        var tipoCbte = await db.TiposComprobante.FindAsync(cbte.IdTipoComprobante);
        if (tipoCbte is null) return BadRequest("Tipo de comprobante inválido.");

        cbte.Fecha = DateTime.UtcNow;
        cbte.Estado = EstadoComprobante.Emitido;
        cbte.IdCliente = null;

        var lockKey = $"cbte:{cbte.IdSucursal}:{cbte.PuntoVenta}:{cbte.IdTipoComprobante}:{cbte.Letra}";
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");

            var ultimo = await db.Comprobantes
                .Where(c => c.PuntoVenta == cbte.PuntoVenta && c.IdTipoComprobante == cbte.IdTipoComprobante && c.Letra == cbte.Letra)
                .MaxAsync(c => (long?)c.Numero) ?? 0;
            cbte.Numero = ultimo + 1;

            db.Comprobantes.Add(cbte);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        if (tipoCbte.RequiereCAE)
        {
            try
            {
                var proveedor = await db.Proveedores.FindAsync(cbte.IdProveedor);
                var cfg = await db.ConfiguracionEmpresa.FirstOrDefaultAsync();
                var (tipoDoc, nroDoc) = ObtenerDatosDocumentoAfipProveedor(proveedor);
                var neto = cbte.SubTotal - cbte.TotalDescuento;

                var solicitud = new SolicitudCAE
                {
                    PuntoVenta      = cbte.PuntoVenta,
                    TipoComprobante = ObtenerCodigoAfip(tipoCbte, cbte.Letra, comision: 0),
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
                await RegistrarLogAfipAsync(cbte.Id, resultado);
                if (resultado.Exito)
                {
                    cbte.CAE            = long.TryParse(resultado.CAE, out var caeNum) ? caeNum : null;
                    cbte.CAEVencimiento = resultado.FechaVencimientoCAE;
                    cbte.QrAfip         = resultado.GenerarQRAfip(
                        cfg?.Cuit ?? _config["Afip:CUIT"] ?? "",
                        cbte.PuntoVenta, solicitud.TipoComprobante, cbte.Fecha, cbte.Total, tipoDoc, nroDoc);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                await RegistrarLogAfipAsync(cbte.Id, null, ex.Message);
                Console.WriteLine($"[AFIP] Error al solicitar CAE (nota a proveedor): {ex.Message}");
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = cbte.Id }, new
        {
            cbte.Id, cbte.Numero, cbte.Letra, cbte.PuntoVenta, cbte.Fecha, cbte.Total,
            cbte.CAE, cbte.CAEVencimiento, cbte.QrAfip, cbte.Estado
        });
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

        await RevertirPagoCtaCteAsync(cbte, idUsuario);

        await db.SaveChangesAsync();
        return NoContent();
    }
}
