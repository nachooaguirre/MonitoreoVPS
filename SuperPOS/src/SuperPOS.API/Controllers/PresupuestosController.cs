using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PresupuestosController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? desde, 
        [FromQuery] DateTime? hasta, 
        [FromQuery] int? idCliente, 
        [FromQuery] EstadoPresupuesto? estado,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var q = db.Presupuestos
            .Include(p => p.Cliente)
            .Include(p => p.Usuario)
            .Include(p => p.Sucursal)
            .AsQueryable();

        if (desde.HasValue) q = q.Where(p => p.Fecha >= desde.Value.ToUtc());
        if (hasta.HasValue) q = q.Where(p => p.Fecha <= hasta.Value.ToUtc().AddDays(1));
        if (idCliente.HasValue) q = q.Where(p => p.IdCliente == idCliente.Value);
        if (estado.HasValue) q = q.Where(p => p.Estado == estado.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var pres = await db.Presupuestos
            .Include(p => p.Cliente)
            .Include(p => p.Usuario)
            .Include(p => p.Sucursal)
            .Include(p => p.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(p => p.Id == id);

        return pres is null ? NotFound() : Ok(pres);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Presupuesto pres)
    {
        pres.Fecha = DateTime.UtcNow;
        pres.Estado = EstadoPresupuesto.Pendiente;

        // Calcular número correlativo por sucursal
        var ultimo = await db.Presupuestos
            .Where(p => p.IdSucursal == pres.IdSucursal)
            .MaxAsync(p => (long?)p.Numero) ?? 0;
        pres.Numero = ultimo + 1;

        db.Presupuestos.Add(pres);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pres.Id }, pres);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] Presupuesto pres)
    {
        if (id != pres.Id) return BadRequest();

        var existente = await db.Presupuestos
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existente == null) return NotFound();
        if (existente.Estado == EstadoPresupuesto.Facturado)
        {
            return BadRequest(new { error = "No se puede modificar un presupuesto que ya ha sido facturado." });
        }

        // Actualizar propiedades básicas
        existente.IdCliente = pres.IdCliente;
        existente.IdUsuario = pres.IdUsuario;
        existente.IdSucursal = pres.IdSucursal;
        existente.PlazoValidezDias = pres.PlazoValidezDias;
        existente.Contacto = pres.Contacto;
        existente.Detalle = pres.Detalle;
        existente.Observacion = pres.Observacion;
        existente.FormaPago = pres.FormaPago;
        existente.SubTotal = pres.SubTotal;
        existente.Total = pres.Total;
        existente.Estado = pres.Estado;

        // Reemplazar detalles
        db.PresupuestosDetalle.RemoveRange(existente.Detalles);
        foreach (var det in pres.Detalles)
        {
            existente.Detalles.Add(new PresupuestoDetalle
            {
                IdArticulo = det.IdArticulo,
                ItemNro = det.ItemNro,
                Descripcion = det.Descripcion,
                Costo = det.Costo,
                Cantidad = det.Cantidad,
                Precio = det.Precio,
                Margen = det.Margen
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var pres = await db.Presupuestos.FindAsync(id);
        if (pres == null) return NotFound();

        if (pres.Estado == EstadoPresupuesto.Facturado)
        {
            return BadRequest(new { error = "No se puede eliminar un presupuesto que ya ha sido facturado." });
        }

        db.Presupuestos.Remove(pres);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/facturar")]
    public async Task<IActionResult> Facturar(long id, [FromBody] FacturarPresupuestoRequest req)
    {
        var pres = await db.Presupuestos
            .Include(p => p.Cliente)
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pres == null) return NotFound();
        if (pres.Estado == EstadoPresupuesto.Facturado)
        {
            return BadRequest(new { error = "Este presupuesto ya fue facturado." });
        }

        // Obtener la caja y medio de pago
        int idCaja = req.IdCaja > 0 ? req.IdCaja : 1;
        int idMedioPago = req.IdMedioPago > 0 ? req.IdMedioPago : 1;
        
        // Determinar tipo de comprobante y letra
        int idTipoCbte = req.IdTipoComprobante > 0 ? req.IdTipoComprobante : 2; // Factura B por defecto
        char letra = !string.IsNullOrWhiteSpace(req.Letra) ? req.Letra[0] : 'B';
        
        // Si el cliente es Responsable Inscripto y no se especificó tipo, usar Factura A (Id=1)
        if (req.IdTipoComprobante == 0 && pres.Cliente != null && pres.Cliente.CondicionIva == 1) // 1 = RI
        {
            idTipoCbte = 1; // Factura A
            letra = 'A';
        }

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Crear el Comprobante de venta
            var cbte = new Comprobante
            {
                Fecha = DateTime.UtcNow,
                IdCliente = pres.IdCliente,
                IdUsuario = pres.IdUsuario > 0 ? pres.IdUsuario : 1,
                IdSucursal = pres.IdSucursal,
                IdCaja = idCaja,
                IdTipoComprobante = idTipoCbte,
                Letra = letra,
                PuntoVenta = req.PuntoVenta > 0 ? req.PuntoVenta : 1,
                Estado = EstadoComprobante.Emitido,
                SubTotal = pres.SubTotal,
                TotalDescuento = 0m,
                TotalIva21 = 0m,  // Se puede refinar calculando el IVA real de cada artículo
                TotalIva105 = 0m,
                TotalIva0 = 0m,
                Total = pres.Total
            };

            // Calcular número correlativo de comprobante
            var ultimo = await db.Comprobantes
                .Where(c => c.PuntoVenta == cbte.PuntoVenta && c.IdTipoComprobante == cbte.IdTipoComprobante && c.Letra == cbte.Letra)
                .MaxAsync(c => (long?)c.Numero) ?? 0;
            cbte.Numero = ultimo + 1;

            // 2. Mapear detalles e impactar stock
            var eventos = new List<TrazabilidadEvento>();
            foreach (var det in pres.Detalles)
            {
                // Consultar artículo para calcular IVA
                var art = await db.Articulos.FindAsync(det.IdArticulo);
                decimal alicuotaIva = art?.AlicuotaIva ?? 21m;
                
                // Calcular IVA e importe neto
                decimal precioSinIva = det.Precio;
                decimal montoIva = 0m;
                if (alicuotaIva > 0)
                {
                    // Si el precio es con IVA
                    precioSinIva = det.Precio / (1 + (alicuotaIva / 100m));
                    montoIva = det.Precio - precioSinIva;
                }

                // Sumar al total de IVAs del comprobante
                if (alicuotaIva == 21m) cbte.TotalIva21 += (montoIva * det.Cantidad);
                else if (alicuotaIva == 10.5m) cbte.TotalIva105 += (montoIva * det.Cantidad);
                else if (alicuotaIva == 0m) cbte.TotalIva0 += (montoIva * det.Cantidad);

                var cbteDet = new ComprobanteDetalle
                {
                    IdArticulo = det.IdArticulo,
                    Descripcion = det.Descripcion,
                    Cantidad = det.Cantidad,
                    PrecioUnitario = det.Precio,
                    PrecioUnitarioSinIva = precioSinIva,
                    AlicuotaIva = alicuotaIva,
                    MontoIva = montoIva * det.Cantidad,
                    PorcentajeDescuento = 0m,
                    MontoDescuento = 0m,
                    SubTotal = det.Precio * det.Cantidad
                };
                cbte.Detalles.Add(cbteDet);

                // Aplicar movimiento de stock
                await StockSucursalHelper.AplicarMovimientoAsync(db, det.IdArticulo, pres.IdSucursal, -det.Cantidad);

                // Actualizar contadores del artículo
                if (art != null)
                {
                    art.CantidadVendida += det.Cantidad;
                    art.UltimaVenta = DateTime.UtcNow;
                }
            }

            // 3. Crear el Pago asociado
            cbte.Pagos.Add(new ComprobantePago
            {
                IdMedioPago = idMedioPago,
                Importe = cbte.Total,
                Vuelto = 0m
            });

            db.Comprobantes.Add(cbte);
            await db.SaveChangesAsync();

            // 4. Crear trazabilidad de venta
            foreach (var det in cbte.Detalles)
            {
                eventos.Add(new TrazabilidadEvento
                {
                    Fecha = cbte.Fecha,
                    IdArticulo = det.IdArticulo,
                    Cantidad = -det.Cantidad,
                    Tipo = TipoTrazabilidadEvento.VentaCaja,
                    Ubicacion = $"Venta Presupuesto #{pres.Numero}",
                    IdUsuario = cbte.IdUsuario > 0 ? cbte.IdUsuario : null,
                    IdComprobante = cbte.Id,
                    IdComprobanteDetalle = det.Id
                });
            }
            if (eventos.Count > 0)
            {
                db.TrazabilidadEventos.AddRange(eventos);
            }

            // 5. Marcar presupuesto como facturado
            pres.Estado = EstadoPresupuesto.Facturado;
            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new { success = true, comprobanteId = cbte.Id, numeroComprobante = cbte.Numero, total = cbte.Total });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Ocurrió un error al facturar el presupuesto: " + ex.Message });
        }
    }
}

public class FacturarPresupuestoRequest
{
    public int IdCaja { get; set; }
    public int IdMedioPago { get; set; }
    public int IdTipoComprobante { get; set; }
    public string? Letra { get; set; }
    public int PuntoVenta { get; set; }
}
