using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportesController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("ventas-dia")]
    public async Task<IActionResult> VentasDia([FromQuery] DateTime? fecha, [FromQuery] int? idSucursal)
    {
        var dia = (fecha?.ToUtc() ?? DateTime.UtcNow).Date;
        var desde = DateTime.SpecifyKind(dia, DateTimeKind.Utc);
        var hasta = DateTime.SpecifyKind(dia.AddDays(1), DateTimeKind.Utc);

        var permitidas = await SucursalScopeHelper.ObtenerPermitidasAsync(User, db);
        if (idSucursal.HasValue && permitidas != null && !permitidas.Contains(idSucursal.Value))
            return Forbid();

        var q = db.Comprobantes.Where(c => c.Fecha >= desde && c.Fecha < hasta && c.Estado != EstadoComprobante.Anulado);
        if (idSucursal.HasValue) q = q.Where(c => c.IdSucursal == idSucursal.Value);
        else if (permitidas != null) q = q.Where(c => permitidas.Contains(c.IdSucursal));

        var comprobantes = await q
            .Include(c => c.Pagos).ThenInclude(p => p.MedioPago)
            .ToListAsync();

        var total = comprobantes.Sum(c => c.Total);
        var cantVentas = comprobantes.Count;
        var ticketPromedio = cantVentas > 0 ? total / cantVentas : 0;

        // Agrupar por medio de pago
        var pagosPorMedio = comprobantes
            .SelectMany(c => c.Pagos)
            .GroupBy(p => p.MedioPago?.Nombre ?? "Efectivo")
            .Select(g => new { medioPago = g.Key, total = g.Sum(p => p.Importe) })
            .ToList();

        return Ok(new
        {
            fecha = dia,
            cantVentas, total, ticketPromedio,
            iva = comprobantes.Sum(c => c.TotalIva21 + c.TotalIva105),
            pagosPorMedio
        });
    }

    [HttpGet("ventas-periodo")]
    public async Task<IActionResult> VentasPeriodo([FromQuery] DateTime desde, [FromQuery] DateTime hasta,
        [FromQuery] string? agrupar = "dia")
    {
        var desdeUtc = DateTime.SpecifyKind(desde.ToUtc().Date, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.ToUtc().Date.AddDays(1), DateTimeKind.Utc);

        var comprobantes = await db.Comprobantes
            .Where(c => c.Fecha >= desdeUtc && c.Fecha < hastaUtc && c.Estado != EstadoComprobante.Anulado)
            .Select(c => new { c.Fecha, c.Total, IvaTotal = c.TotalIva21 + c.TotalIva105 })
            .ToListAsync();

        IEnumerable<object> resultado;
        if (agrupar == "mes")
        {
            resultado = comprobantes
                .GroupBy(c => new { c.Fecha.Year, c.Fecha.Month })
                .Select(g => new
                {
                    periodo = $"{g.Key.Year}-{g.Key.Month:D2}",
                    cantVentas = g.Count(),
                    total = g.Sum(x => x.Total),
                    iva = g.Sum(x => x.IvaTotal)
                })
                .OrderBy(x => x.periodo)
                .Cast<object>();
        }
        else
        {
            resultado = comprobantes
                .GroupBy(c => c.Fecha.Date)
                .Select(g => new
                {
                    periodo = g.Key.ToString("yyyy-MM-dd"),
                    cantVentas = g.Count(),
                    total = g.Sum(x => x.Total),
                    iva = g.Sum(x => x.IvaTotal)
                })
                .OrderBy(x => x.periodo)
                .Cast<object>();
        }

        return Ok(new
        {
            desde, hasta,
            totalPeriodo = comprobantes.Sum(c => c.Total),
            cantTotal = comprobantes.Count,
            detalle = resultado
        });
    }

    [HttpGet("ranking-productos")]
    public async Task<IActionResult> RankingProductos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int top = 20)
    {
        var desdeUtc = DateTime.SpecifyKind((desde?.ToUtc() ?? DateTime.UtcNow.AddDays(-30)).Date, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind((hasta?.ToUtc() ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc);

        var ranking = await db.ComprobantesDetalle
            .Include(d => d.Comprobante)
            .Include(d => d.Articulo)
            .Where(d => d.Comprobante!.Fecha >= desdeUtc && d.Comprobante!.Fecha < hastaUtc
                     && d.Comprobante!.Estado != EstadoComprobante.Anulado)
            .GroupBy(d => new { d.IdArticulo, d.Articulo!.Descripcion })
            .Select(g => new
            {
                idArticulo   = g.Key.IdArticulo,
                descripcion  = g.Key.Descripcion,
                cantVendida  = g.Sum(x => x.Cantidad),
                totalVendido = g.Sum(x => x.SubTotal)
            })
            .OrderByDescending(x => x.cantVendida)
            .Take(top)
            .ToListAsync();

        return Ok(ranking);
    }

    [HttpGet("stock-bajo-minimo")]
    public async Task<IActionResult> StockBajoMinimo()
    {
        var articulos = await db.Articulos
            .Where(a => a.Activo && a.StockMinimo > 0 && a.StockActual <= a.StockMinimo)
            .OrderBy(a => a.Descripcion)
            .Select(a => new
            {
                a.Id, a.CodigoBarras, a.Descripcion, a.PrecioCosto, a.PrecioVenta,
                a.StockActual, a.StockMinimo, a.StockMaximo,
                unidadesAReponer = Math.Max(0, a.StockMaximo - a.StockActual)
            })
            .ToListAsync();
        return Ok(new { total = articulos.Count, articulos });
    }

    [HttpGet("margen-por-departamento")]
    public async Task<IActionResult> MargenPorDepartamento()
    {
        var data = await db.Articulos
            .Where(a => a.Activo && a.PrecioCosto > 0)
            .Include(a => a.Departamento)
            .GroupBy(a => new { a.IdDepartamento, a.Departamento!.Nombre })
            .Select(g => new
            {
                idDepartamento = g.Key.IdDepartamento,
                departamento   = g.Key.Nombre,
                cantArticulos  = g.Count(),
                precioPromedioVenta = g.Average(a => a.PrecioVenta),
                precioPomedioCosto  = g.Average(a => a.PrecioCosto),
                margenPromedio = g.Average(a => a.MargenGanancia)
            })
            .OrderByDescending(x => x.margenPromedio)
            .ToListAsync();
        return Ok(data);
    }

    /// <summary>
    /// Compras vs ventas y margen real por proveedor en un período. Sin idProveedor
    /// devuelve el ranking de todos los proveedores con actividad en el período.
    /// </summary>
    [HttpGet("rentabilidad-proveedor")]
    public async Task<IActionResult> RentabilidadProveedor(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] int? idProveedor)
    {
        var desdeUtc = DateTime.SpecifyKind(desde.ToUtc().Date, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.ToUtc().Date.AddDays(1), DateTimeKind.Utc);

        var comprasQ = db.Compras
            .Where(c => c.Fecha >= desdeUtc && c.Fecha < hastaUtc && c.Estado != EstadoCompra.Anulada);
        if (idProveedor.HasValue) comprasQ = comprasQ.Where(c => c.IdProveedor == idProveedor.Value);

        var compras = await comprasQ
            .GroupBy(c => c.IdProveedor)
            .Select(g => new { IdProveedor = g.Key, TotalComprado = g.Sum(c => c.Total) })
            .ToListAsync();

        var ventasQ = db.ComprobantesDetalle
            .Where(d => d.Comprobante!.Fecha >= desdeUtc && d.Comprobante.Fecha < hastaUtc
                     && d.Comprobante.Estado != EstadoComprobante.Anulado
                     && d.Articulo != null);
        if (idProveedor.HasValue) ventasQ = ventasQ.Where(d => d.Articulo!.IdProveedor == idProveedor.Value);

        var ventas = await ventasQ
            .GroupBy(d => d.Articulo!.IdProveedor)
            .Select(g => new
            {
                IdProveedor = g.Key,
                TotalVendido = g.Sum(d => d.SubTotal),
                CostoVendido = g.Sum(d => d.Cantidad * d.Articulo!.PrecioCosto)
            })
            .ToListAsync();

        var proveedorIds = compras.Select(c => c.IdProveedor)
            .Union(ventas.Select(v => v.IdProveedor))
            .ToList();
        var nombres = await db.Proveedores
            .Where(p => proveedorIds.Contains(p.Id))
            .Select(p => new { p.Id, p.RazonSocial })
            .ToDictionaryAsync(p => p.Id, p => p.RazonSocial);

        var resultado = proveedorIds.Select(id =>
        {
            var compra = compras.FirstOrDefault(c => c.IdProveedor == id);
            var venta = ventas.FirstOrDefault(v => v.IdProveedor == id);
            var totalVendido = venta?.TotalVendido ?? 0;
            var costoVendido = venta?.CostoVendido ?? 0;
            return new
            {
                idProveedor = id,
                proveedor = nombres.GetValueOrDefault(id, "(desconocido)"),
                totalComprado = compra?.TotalComprado ?? 0,
                totalVendido,
                costoVendido,
                margenReal = totalVendido - costoVendido
            };
        })
        .OrderByDescending(r => r.margenReal)
        .ToList();

        return Ok(new { desde, hasta, proveedores = resultado });
    }

    /// <summary>Libro IVA Ventas: comprobantes en período con desglose de alícuotas.</summary>
    [HttpGet("libro-iva-ventas")]
    public async Task<IActionResult> LibroIvaVentas([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var desdeUtc = DateTime.SpecifyKind(desde.ToUtc().Date, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.ToUtc().Date.AddDays(1), DateTimeKind.Utc);

        var comprobantes = await db.Comprobantes
            .Where(c => c.Fecha >= desdeUtc && c.Fecha < hastaUtc && c.Estado != EstadoComprobante.Anulado)
            .Include(c => c.Cliente)
            .Include(c => c.TipoComprobante)
            .OrderBy(c => c.Fecha).ThenBy(c => c.Numero)
            .Select(c => new
            {
                c.Fecha,
                Tipo   = c.TipoComprobante != null ? c.TipoComprobante.Abreviatura : "",
                Letra  = c.Letra.ToString(),
                c.PuntoVenta,
                c.Numero,
                ClienteRazonSocial = c.Cliente != null ? c.Cliente.RazonSocial : "Consumidor Final",
                ClienteCuit        = c.Cliente != null ? c.Cliente.Cuit : "",
                c.SubTotal,
                c.TotalDescuento,
                c.TotalIva21,
                c.TotalIva105,
                c.TotalIva0,
                c.Total,
                c.CAE,
                c.CAEVencimiento
            })
            .ToListAsync();

        return Ok(new
        {
            desde, hasta,
            totalFacturado   = comprobantes.Sum(c => c.Total),
            totalIva21       = comprobantes.Sum(c => c.TotalIva21),
            totalIva105      = comprobantes.Sum(c => c.TotalIva105),
            cantComprobantes = comprobantes.Count,
            comprobantes
        });
    }

    /// <summary>Rendición de cajero: ventas, anulaciones y medios de pago de un usuario en un período.</summary>
    [HttpGet("rendicion-cajero")]
    public async Task<IActionResult> RendicionCajero([FromQuery] int idUsuario, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var desdeUtc = DateTime.SpecifyKind((desde?.ToUtc() ?? DateTime.UtcNow.Date), DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind((hasta?.ToUtc() ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc);

        var comprobantes = await db.Comprobantes
            .Where(c => c.IdUsuario == idUsuario && c.Fecha >= desdeUtc && c.Fecha < hastaUtc)
            .Include(c => c.Pagos).ThenInclude(p => p.MedioPago)
            .ToListAsync();

        var activas  = comprobantes.Where(c => c.Estado != EstadoComprobante.Anulado).ToList();
        var anuladas = comprobantes.Where(c => c.Estado == EstadoComprobante.Anulado).ToList();
        var pagosPorMedio = activas
            .SelectMany(c => c.Pagos)
            .GroupBy(p => p.MedioPago?.Nombre ?? "Efectivo")
            .Select(g => new { medioPago = g.Key, total = g.Sum(p => p.Importe), cantidad = g.Count() })
            .ToList();

        return Ok(new
        {
            idUsuario,
            desde = desdeUtc, hasta = hastaUtc,
            cantVentas       = activas.Count,
            totalVentas      = activas.Sum(c => c.Total),
            cantAnulaciones  = anuladas.Count,
            totalAnulaciones = anuladas.Sum(c => c.Total),
            ticketPromedio   = activas.Count > 0 ? activas.Sum(c => c.Total) / activas.Count : 0,
            pagosPorMedio
        });
    }

    /// <summary>Liquidación de tarjetas: ventas cobradas con tarjeta en el período.</summary>
    [HttpGet("liquidacion-tarjetas")]
    public async Task<IActionResult> LiquidacionTarjetas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var desdeUtc = DateTime.SpecifyKind((desde?.ToUtc() ?? DateTime.UtcNow.AddDays(-30)).Date, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind((hasta?.ToUtc() ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc);
        var tiposTarjeta = new[] { TipoMedioPago.TarjetaCredito, TipoMedioPago.TarjetaDebito };

        var pagos = await db.ComprobantesPago
            .Include(p => p.MedioPago)
            .Include(p => p.Comprobante)
            .Where(p => p.Comprobante!.Fecha >= desdeUtc && p.Comprobante.Fecha < hastaUtc
                     && p.Comprobante.Estado != EstadoComprobante.Anulado
                     && p.MedioPago != null && tiposTarjeta.Contains(p.MedioPago.Tipo))
            .GroupBy(p => new { p.MedioPago!.Nombre, p.MedioPago.CodigoAfip, p.MedioPago.Tipo })
            .Select(g => new
            {
                tarjeta     = g.Key.Nombre,
                codigoAfip  = g.Key.CodigoAfip,
                tipoTarjeta = g.Key.Tipo.ToString(),
                cantCupones = g.Count(),
                totalBruto  = g.Sum(p => p.Importe)
            })
            .OrderByDescending(x => x.totalBruto)
            .ToListAsync();

        return Ok(new
        {
            desde = desdeUtc, hasta = hastaUtc,
            totalTarjetas = pagos.Sum(p => p.totalBruto),
            tarjetas = pagos
        });
    }

    /// <summary>Dashboard gerencial: resumen ejecutivo para el dueño.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var hoy    = DateTime.UtcNow.Date;
        var mes    = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mañana = DateTime.SpecifyKind(hoy.AddDays(1), DateTimeKind.Utc);
        var ayer   = DateTime.SpecifyKind(hoy.AddDays(-1), DateTimeKind.Utc);
        var hoyUtc = DateTime.SpecifyKind(hoy, DateTimeKind.Utc);

        var ventasHoy  = await db.Comprobantes.Where(c => c.Fecha >= hoyUtc && c.Fecha < mañana && c.Estado != EstadoComprobante.Anulado).SumAsync(c => (decimal?)c.Total) ?? 0;
        var ventasAyer = await db.Comprobantes.Where(c => c.Fecha >= ayer && c.Fecha < hoyUtc && c.Estado != EstadoComprobante.Anulado).SumAsync(c => (decimal?)c.Total) ?? 0;
        // Mes calendario hasta hoy (no todo el mes futuro)
        var ventasMes  = await db.Comprobantes.Where(c => c.Fecha >= mes && c.Fecha < mañana && c.Estado != EstadoComprobante.Anulado).SumAsync(c => (decimal?)c.Total) ?? 0;

        var cantVentasHoy  = await db.Comprobantes.CountAsync(c => c.Fecha >= hoyUtc && c.Fecha < mañana && c.Estado != EstadoComprobante.Anulado);
        var artBajoMin     = await db.Articulos.CountAsync(a => a.Activo && a.StockMinimo > 0 && a.StockActual <= a.StockMinimo);
        var ocPendientes   = await db.OrdenesCompra.CountAsync(o => o.Estado == EstadoOrdenCompra.Pendiente || o.Estado == EstadoOrdenCompra.Enviada);
        var remitosHoy     = await db.Remitos.CountAsync(r => r.Fecha >= hoyUtc && r.Fecha < mañana && r.Estado == EstadoRemito.Confirmado);
        var variacion      = ventasAyer > 0 ? (ventasHoy - ventasAyer) / ventasAyer * 100 : 0;

        return Ok(new
        {
            ventasHoy,
            ventasAyer,
            variacionVsDiaAnterior = variacion,
            ventasMes,
            cantVentasHoy,
            ticketPromedioHoy   = cantVentasHoy > 0 ? ventasHoy / cantVentasHoy : 0,
            artBajoMinimo       = artBajoMin,
            ocPendientes,
            remitosRecibidosHoy = remitosHoy
        });
    }
}
