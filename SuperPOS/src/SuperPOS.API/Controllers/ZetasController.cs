using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZetasController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? idCaja, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var q = db.Zetas.Include(z => z.DetallesMedios).AsQueryable();
        if (idCaja.HasValue) q = q.Where(z => z.IdCaja == idCaja.Value);
        if (desde.HasValue) q = q.Where(z => z.FechaCierre >= desde.Value.ToUniversalTime());
        if (hasta.HasValue) q = q.Where(z => z.FechaCierre <= hasta.Value.ToUniversalTime().AddDays(1));
        return Ok(await q.OrderByDescending(z => z.FechaCierre).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var z = await db.Zetas.Include(z => z.DetallesMedios).FirstOrDefaultAsync(z => z.Id == id);
        return z is null ? NotFound() : Ok(z);
    }

    [HttpGet("ultimo/{idCaja}")]
    public async Task<IActionResult> GetUltimo(int idCaja)
    {
        var z = await db.Zetas.Where(x => x.IdCaja == idCaja).OrderByDescending(x => x.NroZeta).FirstOrDefaultAsync();
        return Ok(z);
    }

    /// <summary>
    /// Realiza el cierre de caja (Zeta). Calcula totales desde los comprobantes del turno.
    /// </summary>
    [HttpPost("cerrar")]
    public async Task<IActionResult> CerrarCaja([FromBody] CerrarCajaRequest req)
    {
        var ultimaZeta = await db.Zetas.Where(z => z.IdCaja == req.IdCaja).OrderByDescending(z => z.NroZeta).FirstOrDefaultAsync();
        var fechaDesde = ultimaZeta?.FechaCierre ?? DateTime.UtcNow.Date;
        var nroZeta = (ultimaZeta?.NroZeta ?? 0) + 1;

        // Calcular totales del período
        var comprobantes = await db.Comprobantes
            .Include(c => c.Pagos)
            .Where(c => c.IdCaja == req.IdCaja
                && c.Fecha >= fechaDesde
                && c.Estado != EstadoComprobante.Anulado)
            .ToListAsync();

        var anulados = await db.Comprobantes
            .Where(c => c.IdCaja == req.IdCaja
                && c.Fecha >= fechaDesde
                && c.Estado == EstadoComprobante.Anulado)
            .ToListAsync();

        var pagos = comprobantes.SelectMany(c => c.Pagos).ToList();

        // Totales por medio de pago
        var medios = await db.MediosPago.ToListAsync();
        var detallesMedios = medios
            .Select(mp => new
            {
                mp.Id,
                mp.Nombre,
                mp.Tipo,
                Ops = pagos.Where(p => p.IdMedioPago == mp.Id).ToList()
            })
            .Where(x => x.Ops.Count > 0)
            .Select(x => new ZetaDetalleMedio
            {
                IdMedioPago = x.Id,
                NombreMedio = x.Nombre,
                CantOperaciones = x.Ops.Count,
                Total = x.Ops.Sum(p => p.Importe)
            })
            .ToList();

        decimal totalEfectivo = detallesMedios.Where(d => d.IdMedioPago == 1).Sum(d => d.Total);
        decimal totalDebito = detallesMedios.Where(d => d.IdMedioPago == 2).Sum(d => d.Total);
        decimal totalCredito = detallesMedios.Where(d => d.IdMedioPago == 3).Sum(d => d.Total);
        decimal totalMP = detallesMedios.Where(d => d.IdMedioPago == 4).Sum(d => d.Total);
        decimal totalTransf = detallesMedios.Where(d => d.IdMedioPago == 5).Sum(d => d.Total);
        decimal totalCtaCte = detallesMedios.Where(d => d.IdMedioPago == 6).Sum(d => d.Total);
        decimal totalOtros = detallesMedios.Where(d => d.IdMedioPago > 6).Sum(d => d.Total);

        var zeta = new Zeta
        {
            IdCaja = req.IdCaja,
            IdSucursal = req.IdSucursal,
            IdUsuario = req.IdUsuario,
            NroZeta = nroZeta,
            FechaApertura = fechaDesde,
            FechaCierre = DateTime.UtcNow,
            TotalVentas = comprobantes.Sum(c => c.Total),
            TotalDescuentos = comprobantes.Sum(c => c.TotalDescuento),
            TotalIva21 = comprobantes.Sum(c => c.TotalIva21),
            TotalIva105 = comprobantes.Sum(c => c.TotalIva105),
            TotalIva0 = comprobantes.Sum(c => c.TotalIva0),
            CantidadVentas = comprobantes.Count,
            CantidadAnulaciones = anulados.Count,
            TotalAnulaciones = anulados.Sum(c => c.Total),
            TotalEfectivo = totalEfectivo,
            TotalTarjetaDebito = totalDebito,
            TotalTarjetaCredito = totalCredito,
            TotalMercadoPago = totalMP,
            TotalTransferencia = totalTransf,
            TotalCtaCte = totalCtaCte,
            TotalOtros = totalOtros,
            EfectivoDeclarado = req.EfectivoDeclarado,
            DiferenciaArqueo = req.EfectivoDeclarado - totalEfectivo,
            Observaciones = req.Observaciones,
            DetallesMedios = detallesMedios
        };

        db.Zetas.Add(zeta);
        await db.SaveChangesAsync();
        return Ok(zeta);
    }

    [HttpGet("arqueo/{idCaja}")]
    public async Task<IActionResult> GetArqueo(int idCaja)
    {
        var ultimaZeta = await db.Zetas.Where(z => z.IdCaja == idCaja).OrderByDescending(z => z.NroZeta).FirstOrDefaultAsync();
        var fechaDesde = ultimaZeta?.FechaCierre ?? DateTime.UtcNow.Date;

        var comprobantes = await db.Comprobantes
            .Include(c => c.Pagos)
            .Where(c => c.IdCaja == idCaja && c.Fecha >= fechaDesde && c.Estado != EstadoComprobante.Anulado)
            .ToListAsync();

        var pagos = comprobantes.SelectMany(c => c.Pagos).ToList();
        var medios = await db.MediosPago.Where(m => m.Activo).ToListAsync();

        var detalle = medios.Select(mp => new
        {
            mp.Id,
            mp.Nombre,
            CantOperaciones = pagos.Count(p => p.IdMedioPago == mp.Id),
            Total = pagos.Where(p => p.IdMedioPago == mp.Id).Sum(p => p.Importe)
        }).Where(x => x.CantOperaciones > 0).ToList();

        return Ok(new
        {
            FechaDesde = fechaDesde,
            FechaHasta = DateTime.UtcNow,
            NroZetaSiguiente = (ultimaZeta?.NroZeta ?? 0) + 1,
            CantidadVentas = comprobantes.Count,
            TotalVentas = comprobantes.Sum(c => c.Total),
            TotalIva21 = comprobantes.Sum(c => c.TotalIva21),
            TotalIva105 = comprobantes.Sum(c => c.TotalIva105),
            TotalDescuentos = comprobantes.Sum(c => c.TotalDescuento),
            DetallesMedios = detalle
        });
    }
}

public class CerrarCajaRequest
{
    public int IdCaja { get; set; }
    public int IdSucursal { get; set; }
    public int IdUsuario { get; set; }
    public decimal EfectivoDeclarado { get; set; }
    public string? Observaciones { get; set; }
}
