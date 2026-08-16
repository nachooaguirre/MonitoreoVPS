using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.API.Helpers;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdenesCompraController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EstadoOrdenCompra? estado, [FromQuery] int? idProveedor)
    {
        var q = db.OrdenesCompra.AsNoTracking().AsQueryable();
        if (estado.HasValue) q = q.Where(o => o.Estado == estado.Value);
        if (idProveedor.HasValue) q = q.Where(o => o.IdProveedor == idProveedor.Value);

        // Detalles.Count en proyección puede fallar según el proveedor SQL; se usa subconsulta explícita.
        var lista = await q
            .OrderByDescending(o => o.Fecha)
            .Select(o => new OrdenCompraListItemDto
            {
                Id = o.Id,
                NroOrden = o.NroOrden,
                Fecha = o.Fecha,
                Estado = o.Estado,
                Total = o.Total,
                TotalSinIva = o.TotalSinIva,
                TotalIva = o.TotalIva,
                FechaRecepcion = o.FechaRecepcion,
                ProveedorNombre = o.Proveedor != null ? o.Proveedor.RazonSocial : null,
                CantItems = o.Detalles.Count
            })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var oc = await db.OrdenesCompra
            .Include(o => o.Proveedor)
            .Include(o => o.Detalles).ThenInclude(d => d.Articulo)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (oc is null) return NotFound();

        int? nroOriginal = null;
        if (oc.IdOrdenCompraOriginal.HasValue)
        {
            nroOriginal = await db.OrdenesCompra
                .Where(o => o.Id == oc.IdOrdenCompraOriginal.Value)
                .Select(o => (int?)o.NroOrden)
                .FirstOrDefaultAsync();
        }

        return Ok(new
        {
            oc.Id,
            oc.IdProveedor,
            oc.NroOrden,
            oc.Fecha,
            oc.FechaEntregaEsperada,
            oc.Estado,
            oc.Total,
            oc.TotalSinIva,
            oc.TotalIva,
            oc.Observaciones,
            oc.FechaRecepcion,
            oc.IdOrdenCompraOriginal,
            oc.MotivoDiferencia,
            NroOrdenOriginal = nroOriginal,
            ProveedorNombre = oc.Proveedor?.RazonSocial,
            ProveedorEmail = oc.Proveedor?.Email,
            Detalles = oc.Detalles.Select(d => new
            {
                d.Id,
                d.IdArticulo,
                d.IdOrdenCompra,
                d.CantidadPedida,
                d.CantidadRecibida,
                d.PrecioCosto,
                d.AlicuotaIva,
                d.Subtotal,
                d.ObservacionDiferencia,
                Articulo = d.Articulo == null ? null : new
                {
                    d.Articulo.Id,
                    d.Articulo.CodigoBarras,
                    d.Articulo.Descripcion,
                    d.Articulo.StockActual
                }
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrdenCompra oc)
    {
        var nro = (await db.OrdenesCompra.MaxAsync(o => (int?)o.NroOrden) ?? 0) + 1;
        oc.NroOrden = nro;
        oc.Fecha = DateTime.UtcNow;
        if (oc.Estado != EstadoOrdenCompra.Borrador)
            oc.Estado = EstadoOrdenCompra.Pendiente;
        oc.TotalSinIva = oc.Detalles.Sum(d => d.Subtotal / (1 + d.AlicuotaIva / 100));
        oc.TotalIva = oc.Detalles.Sum(d => d.Subtotal - d.Subtotal / (1 + d.AlicuotaIva / 100));
        oc.Total = oc.Detalles.Sum(d => d.Subtotal);
        db.OrdenesCompra.Add(oc);
        await db.SaveChangesAsync();
        return Ok(oc);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrdenCompra ocActualizada)
    {
        var oc = await db.OrdenesCompra.Include(o => o.Detalles).FirstOrDefaultAsync(o => o.Id == id);
        if (oc is null) return NotFound();

        // Actualizar datos cabecera
        oc.FechaEntregaEsperada = ocActualizada.FechaEntregaEsperada;
        oc.TotalSinIva = ocActualizada.Detalles.Sum(d => d.Subtotal / (1 + d.AlicuotaIva / 100));
        oc.TotalIva = ocActualizada.Detalles.Sum(d => d.Subtotal - d.Subtotal / (1 + d.AlicuotaIva / 100));
        oc.Total = ocActualizada.Detalles.Sum(d => d.Subtotal);
        oc.Observaciones = ocActualizada.Observaciones;
        oc.MotivoDiferencia = ocActualizada.MotivoDiferencia;
        oc.IdOrdenCompraOriginal = ocActualizada.IdOrdenCompraOriginal;

        // Eliminar detalles que ya no están
        var idsNuevos = ocActualizada.Detalles.Select(d => d.IdArticulo).ToList();
        var aEliminar = oc.Detalles.Where(d => !idsNuevos.Contains(d.IdArticulo)).ToList();
        foreach (var d in aEliminar) oc.Detalles.Remove(d);

        // Actualizar existentes y agregar nuevos
        foreach (var detNuevo in ocActualizada.Detalles)
        {
            var detExistente = oc.Detalles.FirstOrDefault(d => d.IdArticulo == detNuevo.IdArticulo);
            if (detExistente != null)
            {
                detExistente.CantidadPedida = detNuevo.CantidadPedida;
                detExistente.PrecioCosto = detNuevo.PrecioCosto;
                detExistente.AlicuotaIva = detNuevo.AlicuotaIva;
                detExistente.Subtotal = detNuevo.Subtotal;
            }
            else
            {
                oc.Detalles.Add(new OrdenCompraDetalle
                {
                    IdArticulo = detNuevo.IdArticulo,
                    CantidadPedida = detNuevo.CantidadPedida,
                    PrecioCosto = detNuevo.PrecioCosto,
                    AlicuotaIva = detNuevo.AlicuotaIva,
                    Subtotal = detNuevo.Subtotal,
                    CantidadRecibida = 0
                });
            }
        }

        await db.SaveChangesAsync();
        return Ok(oc);
    }

    [HttpPut("{id}/enviar")]
    public async Task<IActionResult> Enviar(int id)
    {
        var oc = await db.OrdenesCompra.FindAsync(id);
        if (oc is null) return NotFound();
        oc.Estado = EstadoOrdenCompra.Enviada;
        await db.SaveChangesAsync();
        return Ok(oc);
    }

    [HttpPut("{id}/recibir")]
    public async Task<IActionResult> Recibir(int id, [FromBody] RecibirOCRequest req)
    {
        var oc = await db.OrdenesCompra.Include(o => o.Detalles).FirstOrDefaultAsync(o => o.Id == id);
        if (oc is null) return NotFound();

        foreach (var item in req.Items)
        {
            var det = oc.Detalles.FirstOrDefault(d => d.IdArticulo == item.IdArticulo);
            if (det != null)
            {
                det.CantidadRecibida = item.CantidadRecibida;
                det.ObservacionDiferencia = item.ObservacionDiferencia;
            }
            else
            {
                // Articulo excedente: agregarlo con cantidad pedida = 0
                var art = await db.Articulos.FindAsync(item.IdArticulo);
                if (art != null)
                {
                    oc.Detalles.Add(new OrdenCompraDetalle
                    {
                        IdArticulo = item.IdArticulo,
                        CantidadPedida = 0,
                        CantidadRecibida = item.CantidadRecibida,
                        PrecioCosto = item.PrecioCosto > 0 ? item.PrecioCosto : (decimal)art.PrecioCosto,
                        AlicuotaIva = (decimal)art.AlicuotaIva,
                        Subtotal = 0,
                        ObservacionDiferencia = item.ObservacionDiferencia
                    });
                }
            }
        }

        oc.Estado = EstadoOrdenCompra.RecepcionParcial;
        oc.FechaRecepcion = DateTime.UtcNow;
        oc.IdUsuarioRecepcion = req.IdUsuario;

        await db.SaveChangesAsync();
        return Ok(oc);
    }

    [HttpPut("{id}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        var oc = await db.OrdenesCompra.FindAsync(id);
        if (oc is null) return NotFound();
        if (oc.Estado == EstadoOrdenCompra.Recibida) return BadRequest("No se puede anular una OC ya recibida.");
        oc.Estado = EstadoOrdenCompra.Anulada;
        await db.SaveChangesAsync();
        return Ok(oc);
    }

    [HttpPut("{id}/devolver")]
    public async Task<IActionResult> Devolver(int id)
    {
        var oc = await db.OrdenesCompra.FindAsync(id);
        if (oc is null) return NotFound();
        if (oc.Estado == EstadoOrdenCompra.Recibida) return BadRequest("No se puede devolver una OC ya recibida.");
        oc.Estado = EstadoOrdenCompra.Devolvida;
        await db.SaveChangesAsync();
        return Ok(oc);
    }

    /// <summary>
    /// Genera una orden de compra sugerida por artículos bajo mínimo de stock
    /// </summary>
    [HttpGet("sugerida/{idProveedor}")]
    public async Task<IActionResult> GetSugerida(int idProveedor)
    {
        var arts = await db.Articulos
            .Where(a => a.IdProveedor == idProveedor && a.Activo && a.StockActual <= a.StockMinimo)
            .OrderBy(a => a.Descripcion)
            .ToListAsync();

        var items = arts.Select(a => new
        {
            a.Id,
            a.CodigoBarras,
            a.CodigoInterno,
            a.CodigoProveedor,
            a.Descripcion,
            a.StockActual,
            a.StockMinimo,
            a.StockMaximo,
            CantidadSugerida = Math.Max(a.StockMaximo - a.StockActual, a.UnidadesPorBulto),
            a.PrecioCosto,
            a.AlicuotaIva,
            SubtotalEstimado = Math.Max(a.StockMaximo - a.StockActual, a.UnidadesPorBulto) * a.PrecioCosto
        }).ToList();

        return Ok(new
        {
            IdProveedor = idProveedor,
            CantidadArticulos = items.Count,
            TotalEstimado = items.Sum(i => i.SubtotalEstimado),
            Items = items
        });
    }
}

public class OrdenCompraListItemDto
{
    public int Id { get; set; }
    public int NroOrden { get; set; }
    public DateTime Fecha { get; set; }
    public EstadoOrdenCompra Estado { get; set; }
    public decimal Total { get; set; }
    public decimal TotalSinIva { get; set; }
    public decimal TotalIva { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public string? ProveedorNombre { get; set; }
    public int CantItems { get; set; }
}

public class RecibirOCRequest
{
    public int IdUsuario { get; set; }
    /// <summary>Sucursal donde ingresa la mercadería. Por defecto: sucursal central (depósito).</summary>
    public int? IdSucursalDestino { get; set; }
    public List<ItemRecepcion> Items { get; set; } = [];
}

public class ItemRecepcion
{
    public int IdArticulo { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal PrecioCosto { get; set; }
    public string? ObservacionDiferencia { get; set; }
}
