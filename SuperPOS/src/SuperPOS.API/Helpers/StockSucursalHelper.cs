using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Helpers;

public static class StockSucursalHelper
{
    public static async Task<int?> ObtenerIdSucursalCentralAsync(SuperPOSDbContext db, CancellationToken ct = default)
        => await db.Sucursales.AsNoTracking()
            .Where(s => s.Activo && s.EsCentral)
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

    public static async Task<decimal> ObtenerCantidadAsync(SuperPOSDbContext db, int idArticulo, int idSucursal, CancellationToken ct = default)
    {
        return await db.ArticulosStockPorSucursal.AsNoTracking()
            .Where(x => x.IdArticulo == idArticulo && x.IdSucursal == idSucursal)
            .Select(x => (decimal?)x.Cantidad)
            .FirstOrDefaultAsync(ct) ?? 0m;
    }

    /// <summary>Fija el stock en una sucursal (inventario aplicado).</summary>
    public static async Task FijarCantidadAsync(SuperPOSDbContext db, int idArticulo, int idSucursal, decimal cantidad, CancellationToken ct = default)
    {
        if (cantidad < 0) throw new InvalidOperationException("La cantidad de stock no puede ser negativa.");
        var row = await db.ArticulosStockPorSucursal.FirstOrDefaultAsync(x => x.IdArticulo == idArticulo && x.IdSucursal == idSucursal, ct);
        if (row is null)
        {
            row = new ArticuloStockSucursal { IdArticulo = idArticulo, IdSucursal = idSucursal, Cantidad = cantidad };
            db.ArticulosStockPorSucursal.Add(row);
        }
        else
            row.Cantidad = cantidad;

        await SincronizarCamposArticuloAsync(db, idArticulo, ct);
    }

    /// <summary>Incrementa o decrementa stock en sucursal (ventas, remitos, transferencias, compras).</summary>
    public static async Task AplicarMovimientoAsync(SuperPOSDbContext db, int idArticulo, int idSucursal, decimal delta, bool permitirNegativo = false, CancellationToken ct = default)
    {
        if (delta == 0) return;

        var controlaStock = permitirNegativo ? false : await db.ConfiguracionEmpresa.Select(x => x.ControlaStock).FirstOrDefaultAsync(ct);

        var row = await db.ArticulosStockPorSucursal.FirstOrDefaultAsync(x => x.IdArticulo == idArticulo && x.IdSucursal == idSucursal, ct);
        if (row is null)
        {
            if (delta < 0 && controlaStock)
                throw new InvalidOperationException($"Sin stock en sucursal {idSucursal} para el artículo {idArticulo}.");
            row = new ArticuloStockSucursal { IdArticulo = idArticulo, IdSucursal = idSucursal, Cantidad = 0 };
            db.ArticulosStockPorSucursal.Add(row);
        }

        var nueva = row.Cantidad + delta;
        if (nueva < 0 && controlaStock)
            throw new InvalidOperationException(
                $"Stock insuficiente en sucursal {idSucursal} para el artículo {idArticulo} (disponible {row.Cantidad}, requerido {-delta}).");

        row.Cantidad = nueva;
        await SincronizarCamposArticuloAsync(db, idArticulo, ct);
    }

    /// <summary>
    /// Mantiene <see cref="Articulo.StockActual"/> como total en todas las sucursales y
    /// <see cref="Articulo.StockDeposito"/> como cantidad en la sucursal marcada EsCentral.
    /// </summary>
    public static async Task SincronizarCamposArticuloAsync(SuperPOSDbContext db, int idArticulo, CancellationToken ct = default)
    {
        var art = await db.Articulos.FirstOrDefaultAsync(a => a.Id == idArticulo, ct);
        if (art is null) return;

        // 1. Obtener todos los registros de stock de la base de datos (con fusión automática de modificados por EF)
        var dbRows = await db.ArticulosStockPorSucursal
            .Where(x => x.IdArticulo == idArticulo)
            .ToListAsync(ct);

        // 2. Incorporar los registros agregados ("Added") en memoria que no se han guardado aún
        var localAdded = db.ChangeTracker.Entries<ArticuloStockSucursal>()
            .Where(e => e.State == EntityState.Added && e.Entity.IdArticulo == idArticulo)
            .Select(e => e.Entity);

        var allRows = dbRows.Concat(localAdded)
            .GroupBy(x => x.IdSucursal)
            .Select(g => g.First())
            .ToList();

        var total = allRows.Sum(x => x.Cantidad);
        var centralId = await ObtenerIdSucursalCentralAsync(db, ct);
        var enCentral = centralId.HasValue
            ? allRows.Where(x => x.IdSucursal == centralId.Value).Select(x => x.Cantidad).FirstOrDefault()
            : 0m;

        art.StockActual = total;
        art.StockDeposito = enCentral;
        art.FechaModificacion = DateTime.UtcNow;
    }
}
