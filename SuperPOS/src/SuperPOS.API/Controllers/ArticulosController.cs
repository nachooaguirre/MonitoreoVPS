using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.API.Hubs;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticulosController(SuperPOSDbContext db, IHubContext<PosHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? buscar,
        [FromQuery] int? idDepartamento,
        [FromQuery] int? idFamilia,
        [FromQuery] int? idMarca,
        [FromQuery] int? idProveedor,
        [FromQuery] bool? soloConStock,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] bool aplicarOfertas = false)
    {
        var q = db.Articulos
            .Include(a => a.Departamento)
            .Include(a => a.Familia)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .AsQueryable();

        if (!incluirInactivos)
            q = q.Where(a => a.Activo);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim();
            var tl = t.ToLower();
            q = q.Where(a => a.Descripcion.ToLower().Contains(tl)
                           || a.CodigoBarras.Contains(t)
                           || a.CodigoInterno.Contains(t));
        }
        if (idDepartamento.HasValue) q = q.Where(a => a.IdDepartamento == idDepartamento);
        if (idFamilia is > 0) q = q.Where(a => a.IdFamilia == idFamilia);
        if (idMarca.HasValue) q = q.Where(a => a.IdMarca == idMarca);
        if (idProveedor.HasValue) q = q.Where(a => a.IdProveedor == idProveedor.Value);
        if (soloConStock == true) q = q.Where(a => a.StockActual > 0);

        var total = await q.CountAsync();
        var items = await q.OrderBy(a => a.Descripcion).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        if (aplicarOfertas)
        {
            var now = DateTime.UtcNow;
            var artIds = items.Select(a => a.Id).ToList();
            var ofertas = await db.Ofertas
                .Where(o => artIds.Contains(o.IdArticulo) && o.Activa && o.FechaDesde <= now && o.FechaHasta >= now)
                .ToListAsync();

            foreach (var art in items)
            {
                var oferta = ofertas.FirstOrDefault(o => o.IdArticulo == art.Id);
                if (oferta != null && (oferta.LimiteStock == null || oferta.CantidadVendida < oferta.LimiteStock))
                {
                    art.PrecioVenta = oferta.PrecioOferta;
                }
            }
        }

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("buscarPorCodigoBarras/{codigo}")]
    public async Task<IActionResult> BuscarPorCodigo(string codigo)
    {
        var art = await db.Articulos
            .Include(a => a.Departamento)
            .FirstOrDefaultAsync(a => a.Activo && (a.CodigoBarras == codigo || a.CodigoInterno == codigo));

        if (art is null)
        {
            art = await db.Articulos
                .Include(a => a.Departamento)
                .Include(a => a.CodigosBarras)
                .FirstOrDefaultAsync(a => a.Activo && a.CodigosBarras.Any(cb => cb.CodigoBarras == codigo));
        }

        if (art is not null)
        {
            var now = DateTime.UtcNow;
            var oferta = await db.Ofertas
                .Where(o => o.IdArticulo == art.Id && o.Activa && o.FechaDesde <= now && o.FechaHasta >= now)
                .FirstOrDefaultAsync();

            if (oferta != null && (oferta.LimiteStock == null || oferta.CantidadVendida < oferta.LimiteStock))
            {
                art.PrecioVenta = oferta.PrecioOferta;
            }
        }

        return art is null ? NotFound() : Ok(art);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var art = await db.Articulos
            .Include(a => a.Departamento)
            .Include(a => a.Familia)
            .Include(a => a.Marca)
            .Include(a => a.Proveedor)
            .Include(a => a.CodigosBarras)
            .FirstOrDefaultAsync(a => a.Id == id);
        return art is null ? NotFound() : Ok(art);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Articulo articulo, [FromQuery] int? idUsuario = null, [FromQuery] int? idSucursal = null)
    {
        articulo.FechaAlta = DateTime.UtcNow;
        db.Articulos.Add(articulo);
        await db.SaveChangesAsync();

        var totalInicial = articulo.StockActual + articulo.StockDeposito;
        if (totalInicial != 0)
        {
            var central = await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db) ?? 1;
            await StockSucursalHelper.FijarCantidadAsync(db, articulo.Id, central, totalInicial);
            await db.SaveChangesAsync();
        }

        // Registrar auditoría de Alta
        var histAlta = new HistorialPrecio
        {
            IdArticulo = articulo.Id,
            Fecha = DateTime.UtcNow,
            IdUsuario = idUsuario,
            IdSucursal = idSucursal,
            PrecioAnterior = 0,
            PrecioNuevo = 0,
            Campo = "A"
        };
        db.HistorialPrecios.Add(histAlta);

        // Registrar costo inicial si es mayor a 0
        if (articulo.PrecioCosto > 0)
        {
            db.HistorialPrecios.Add(new HistorialPrecio
            {
                IdArticulo = articulo.Id,
                Fecha = DateTime.UtcNow,
                IdUsuario = idUsuario,
                IdSucursal = idSucursal,
                PrecioAnterior = 0,
                PrecioNuevo = articulo.PrecioCosto,
                Campo = "C"
            });
        }

        // Registrar precio de venta inicial si es mayor a 0
        if (articulo.PrecioVenta > 0)
        {
            db.HistorialPrecios.Add(new HistorialPrecio
            {
                IdArticulo = articulo.Id,
                Fecha = DateTime.UtcNow,
                IdUsuario = idUsuario,
                IdSucursal = idSucursal,
                PrecioAnterior = 0,
                PrecioNuevo = articulo.PrecioVenta,
                Campo = "V"
            });
        }

        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = articulo.Id }, articulo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Articulo articulo, [FromQuery] int? idUsuario = null, [FromQuery] int? idSucursal = null)
    {
        if (id != articulo.Id) return BadRequest();

        var actual = await db.Articulos.FirstOrDefaultAsync(a => a.Id == id);
        if (actual is null) return NotFound();

        // Comparar precios y registrar en historial antes de modificar actual
        var logs = new List<HistorialPrecio>();
        if (actual.PrecioCosto != articulo.PrecioCosto)
        {
            logs.Add(new HistorialPrecio
            {
                IdArticulo = articulo.Id,
                Fecha = DateTime.UtcNow,
                IdUsuario = idUsuario,
                IdSucursal = idSucursal,
                PrecioAnterior = actual.PrecioCosto,
                PrecioNuevo = articulo.PrecioCosto,
                Campo = "C"
            });
        }

        if (actual.PrecioVenta != articulo.PrecioVenta)
        {
            logs.Add(new HistorialPrecio
            {
                IdArticulo = articulo.Id,
                Fecha = DateTime.UtcNow,
                IdUsuario = idUsuario,
                IdSucursal = idSucursal,
                PrecioAnterior = actual.PrecioVenta,
                PrecioNuevo = articulo.PrecioVenta,
                Campo = "V"
            });
        }

        // Si cambió el stock en el editor o si no tiene registros de sucursal, sincronizar la sucursal central
        var tieneStockRegistrado = await db.ArticulosStockPorSucursal.AnyAsync(x => x.IdArticulo == articulo.Id);
        if (actual.StockActual != articulo.StockActual || !tieneStockRegistrado)
        {
            var central = await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db) ?? 1;
            await StockSucursalHelper.FijarCantidadAsync(db, articulo.Id, central, articulo.StockActual);
        }

        // Actualizar valores del objeto trackeado
        db.Entry(actual).CurrentValues.SetValues(articulo);
        actual.FechaModificacion = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (logs.Count > 0)
        {
            db.HistorialPrecios.AddRange(logs);
            await db.SaveChangesAsync();
        }

        // Avisa a las terminales (vía SignalR) que este artículo cambió, para que la que tenga
        // conectada la balanza Kretz en su red local le transmita el nuevo precio/PLU.
        if (logs.Any(l => l.Campo == "V"))
            await hub.Clients.All.SendAsync("ArticuloActualizado", articulo.Id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var art = await db.Articulos.FindAsync(id);
        if (art is null) return NotFound();
        art.Activo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("departamentos")]
    public async Task<IActionResult> GetDepartamentos() =>
        Ok(await db.Departamentos.Where(d => d.Activo).OrderBy(d => d.Nombre).ToListAsync());

    [HttpGet("familias")]
    public async Task<IActionResult> GetFamilias([FromQuery] int? idDepartamento)
    {
        var q = db.Familias.Where(f => f.Activo).AsQueryable();
        if (idDepartamento.HasValue) q = q.Where(f => f.IdDepartamento == idDepartamento);
        return Ok(await q.OrderBy(f => f.Nombre).ToListAsync());
    }

    [HttpGet("marcas")]
    public async Task<IActionResult> GetMarcas() =>
        Ok(await db.Marcas.Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync());

    [HttpPost("precios/conteo-lote")]
    public async Task<IActionResult> GetConteoLote([FromBody] ActualizacionPreciosFiltrosDto filtros)
    {
        var q = db.Articulos.Where(a => a.Activo).AsQueryable();
        if (filtros.IdDepartamento.HasValue && filtros.IdDepartamento.Value > 0)
            q = q.Where(a => a.IdDepartamento == filtros.IdDepartamento.Value);
        if (filtros.IdFamilia.HasValue && filtros.IdFamilia.Value > 0)
            q = q.Where(a => a.IdFamilia == filtros.IdFamilia.Value);
        if (filtros.IdMarca.HasValue && filtros.IdMarca.Value > 0)
            q = q.Where(a => a.IdMarca == filtros.IdMarca.Value);
        if (filtros.IdProveedor.HasValue && filtros.IdProveedor.Value > 0)
            q = q.Where(a => a.IdProveedor == filtros.IdProveedor.Value);

        var count = await q.CountAsync();
        return Ok(new { count });
    }

    [HttpPut("precios/actualizar-lote")]
    public async Task<IActionResult> ActualizarLote([FromBody] ActualizacionPreciosLoteDto dto)
    {
        if (dto == null) return BadRequest("Datos inválidos.");
        if (string.IsNullOrWhiteSpace(dto.Campo) || string.IsNullOrWhiteSpace(dto.Metodo))
            return BadRequest("Campos de actualización inválidos.");

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var q = db.Articulos.Where(a => a.Activo).AsQueryable();
            if (dto.IdDepartamento.HasValue && dto.IdDepartamento.Value > 0)
                q = q.Where(a => a.IdDepartamento == dto.IdDepartamento.Value);
            if (dto.IdFamilia.HasValue && dto.IdFamilia.Value > 0)
                q = q.Where(a => a.IdFamilia == dto.IdFamilia.Value);
            if (dto.IdMarca.HasValue && dto.IdMarca.Value > 0)
                q = q.Where(a => a.IdMarca == dto.IdMarca.Value);
            if (dto.IdProveedor.HasValue && dto.IdProveedor.Value > 0)
                q = q.Where(a => a.IdProveedor == dto.IdProveedor.Value);

            var articulos = await q.ToListAsync();
            if (articulos.Count == 0) return Ok(new { count = 0 });

            foreach (var a in articulos)
            {
                var precioCostoAnterior = a.PrecioCosto;
                var precioVentaAnterior = a.PrecioVenta;

                if (dto.Campo == "Costo")
                {
                    if (dto.Metodo == "Porcentaje")
                        a.PrecioCosto = a.PrecioCosto * (1 + dto.Valor / 100);
                    else if (dto.Metodo == "Fijo")
                        a.PrecioCosto = a.PrecioCosto + dto.Valor;
                    
                    RecalcularPreciosArticulo(a, "Costo");
                }
                else if (dto.Campo == "Margen")
                {
                    if (dto.Metodo == "Porcentaje")
                        a.MargenGanancia = a.MargenGanancia * (1 + dto.Valor / 100);
                    else if (dto.Metodo == "Fijo")
                        a.MargenGanancia = a.MargenGanancia + dto.Valor;
                    
                    RecalcularPreciosArticulo(a, "Margen");
                }
                else if (dto.Campo == "Venta")
                {
                    if (dto.Metodo == "Porcentaje")
                        a.PrecioVenta = a.PrecioVenta * (1 + dto.Valor / 100);
                    else if (dto.Metodo == "Fijo")
                        a.PrecioVenta = a.PrecioVenta + dto.Valor;
                    
                    RecalcularPreciosArticulo(a, "Venta");
                }
                
                a.FechaModificacion = DateTime.UtcNow;

                // Registrar cambios de precios/costos si ocurrieron
                if (a.PrecioCosto != precioCostoAnterior)
                {
                    db.HistorialPrecios.Add(new HistorialPrecio
                    {
                        IdArticulo = a.Id,
                        Fecha = DateTime.UtcNow,
                        IdUsuario = dto.IdUsuario,
                        IdSucursal = dto.IdSucursal,
                        PrecioAnterior = precioCostoAnterior,
                        PrecioNuevo = a.PrecioCosto,
                        Campo = "C"
                    });
                }

                if (a.PrecioVenta != precioVentaAnterior)
                {
                    db.HistorialPrecios.Add(new HistorialPrecio
                    {
                        IdArticulo = a.Id,
                        Fecha = DateTime.UtcNow,
                        IdUsuario = dto.IdUsuario,
                        IdSucursal = dto.IdSucursal,
                        PrecioAnterior = precioVentaAnterior,
                        PrecioNuevo = a.PrecioVenta,
                        Campo = "V"
                    });
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { count = articulos.Count });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Error al actualizar precios en lote: {ex.Message}");
        }
    }

    private void RecalcularPreciosArticulo(Articulo a, string campoCambiado)
    {
        decimal costoNeto = a.PrecioCosto;
        costoNeto -= costoNeto * (a.Bonificacion1 / 100);
        costoNeto -= costoNeto * (a.Bonificacion2 / 100);
        costoNeto -= costoNeto * (a.Bonificacion3 / 100);
        costoNeto -= costoNeto * (a.Bonificacion4 / 100);
        costoNeto -= costoNeto * (a.Bonificacion5 / 100);
        costoNeto += costoNeto * (a.Recargo1 / 100);

        if (campoCambiado == "Costo" || campoCambiado == "Margen")
        {
            decimal precioSinIva = costoNeto * (1 + a.MargenGanancia / 100);
            decimal precioConIva = a.AplicaIva ? precioSinIva * (1 + a.AlicuotaIva / 100) : precioSinIva;
            a.PrecioVenta = precioConIva + a.ImpuestoInterno;
        }
        else if (campoCambiado == "Venta")
        {
            if (costoNeto > 0)
            {
                decimal precioConIva = a.PrecioVenta - a.ImpuestoInterno;
                decimal precioSinIva = a.AplicaIva ? precioConIva / (1 + a.AlicuotaIva / 100) : precioConIva;
                a.MargenGanancia = ((precioSinIva / costoNeto) - 1) * 100;
            }
        }
    }

    [HttpGet("{id}/historial-precios")]
    public async Task<IActionResult> GetHistorialPrecios(int id)
    {
        var list = await db.HistorialPrecios
            .Include(h => h.Usuario)
            .Include(h => h.Sucursal)
            .Where(h => h.IdArticulo == id)
            .OrderByDescending(h => h.Fecha)
            .Select(h => new
            {
                h.Id,
                h.IdArticulo,
                h.Fecha,
                h.IdUsuario,
                UsuarioNombre = h.Usuario != null ? h.Usuario.NombreCompleto : "Sistema",
                h.IdSucursal,
                SucursalNombre = h.Sucursal != null ? h.Sucursal.Nombre : "Casa Central",
                h.PrecioAnterior,
                h.PrecioNuevo,
                h.Campo
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPut("{id}/ajustar-stock")]
    public async Task<IActionResult> AjustarStock(int id, [FromQuery] decimal delta, [FromQuery] int? idSucursal = null,
        [FromQuery] string? motivo = null, [FromQuery] int? idUsuario = null)
    {
        var art = await db.Articulos.FindAsync(id);
        if (art is null) return NotFound();

        var idSuc = idSucursal ?? await StockSucursalHelper.ObtenerIdSucursalCentralAsync(db) ?? 1;

        try
        {
            // AplicarMovimientoAsync ya recalcula y fija Articulo.StockActual/StockDeposito (total real
            // entre todas las sucursales) vía SincronizarCamposArticuloAsync — no reasignar acá con una
            // lectura no trackeada de una sola sucursal, que pisaba ese valor correcto con uno obsoleto.
            await StockSucursalHelper.AplicarMovimientoAsync(db, id, idSuc, delta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        art.FechaModificacion = DateTime.UtcNow;

        if (delta != 0)
        {
            var sucNombre = await db.Sucursales.Where(s => s.Id == idSuc).Select(s => s.Nombre).FirstOrDefaultAsync();
            db.TrazabilidadEventos.Add(new TrazabilidadEvento
            {
                Fecha = DateTime.UtcNow,
                IdArticulo = id,
                Cantidad = delta,
                Tipo = string.IsNullOrWhiteSpace(motivo) ? TipoTrazabilidadEvento.AjusteInventario : TipoTrazabilidadEvento.MovimientoInterno,
                Ubicacion = motivo ?? sucNombre ?? $"Sucursal {idSuc}",
                IdUsuario = idUsuario is > 0 ? idUsuario : null
            });
        }

        await db.SaveChangesAsync();

        return Ok(new { id = art.Id, stockActual = art.StockActual });
    }

    /// <summary>Stock y mínimos (depósito + góndola) de un artículo, por cada sucursal.</summary>
    [HttpGet("{id}/stock-sucursales")]
    public async Task<IActionResult> GetStockSucursales(int id)
    {
        var sucursales = await db.Sucursales.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Id).ToListAsync();
        var stocks = await db.ArticulosStockPorSucursal.AsNoTracking().Where(x => x.IdArticulo == id).ToListAsync();

        var items = sucursales.Select(s =>
        {
            var row = stocks.FirstOrDefault(x => x.IdSucursal == s.Id);
            return new
            {
                s.Id,
                SucursalNombre = s.Nombre,
                Cantidad = row?.Cantidad ?? 0,
                StockMinimo = row?.StockMinimo ?? 0,
                StockMinimoGondola = row?.StockMinimoGondola ?? 0
            };
        });
        return Ok(items);
    }

    /// <summary>Fija los mínimos de reposición (depósito y góndola) de un artículo para una sucursal puntual.</summary>
    [HttpPut("{id}/stock-sucursales/{idSucursal}/minimos")]
    public async Task<IActionResult> SetStockMinimoSucursal(int id, int idSucursal, [FromBody] StockMinimoSucursalRequest req)
    {
        var row = await db.ArticulosStockPorSucursal.FirstOrDefaultAsync(x => x.IdArticulo == id && x.IdSucursal == idSucursal);
        if (row is null)
        {
            row = new ArticuloStockSucursal { IdArticulo = id, IdSucursal = idSucursal };
            db.ArticulosStockPorSucursal.Add(row);
        }
        row.StockMinimo = req.StockMinimo;
        row.StockMinimoGondola = req.StockMinimoGondola;

        await db.SaveChangesAsync();
        return Ok(new { row.IdArticulo, row.IdSucursal, row.StockMinimo, row.StockMinimoGondola });
    }
}

public class StockMinimoSucursalRequest
{
    public decimal StockMinimo { get; set; }
    public decimal StockMinimoGondola { get; set; }
}

public class ActualizacionPreciosFiltrosDto
{
    public int? IdDepartamento { get; set; }
    public int? IdFamilia { get; set; }
    public int? IdMarca { get; set; }
    public int? IdProveedor { get; set; }
}

public class ActualizacionPreciosLoteDto
{
    public int? IdDepartamento { get; set; }
    public int? IdFamilia { get; set; }
    public int? IdMarca { get; set; }
    public int? IdProveedor { get; set; }
    public string Campo { get; set; } = string.Empty; // "Costo", "Margen", "Venta"
    public string Metodo { get; set; } = string.Empty; // "Porcentaje", "Fijo"
    public decimal Valor { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdSucursal { get; set; }
}
