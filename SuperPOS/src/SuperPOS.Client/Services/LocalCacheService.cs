using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Services;

/// <summary>
/// Caché local (SQLite, un archivo) de precios/stock y cola de ventas offline.
/// Permite seguir vendiendo en efectivo si se corta la conexión con el servidor:
/// la venta se guarda acá y se sincroniza sola cuando vuelve internet.
/// </summary>
public class LocalCacheService
{
    private readonly string _connString;

    public LocalCacheService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuperPOS");
        Directory.CreateDirectory(dir);
        _connString = $"Data Source={Path.Combine(dir, "cache.db")}";
    }

    public async Task InicializarAsync()
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ArticulosCache (
                Id INTEGER PRIMARY KEY,
                CodigoBarras TEXT,
                Descripcion TEXT,
                PrecioVenta REAL,
                StockActual REAL,
                AlicuotaIva REAL
            );
            CREATE INDEX IF NOT EXISTS IX_ArticulosCache_CodigoBarras ON ArticulosCache(CodigoBarras);
            CREATE TABLE IF NOT EXISTS VentasPendientes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Json TEXT NOT NULL,
                Fecha TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Descarga el catálogo completo (precio + stock) y reemplaza la caché local. Llamar con conexión activa.</summary>
    public async Task RefrescarAsync(ApiService api)
    {
        var (_, items) = await api.GetArticulos(page: 1, pageSize: 20000, incluirInactivos: false);

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        var del = conn.CreateCommand();
        del.Transaction = tx;
        del.CommandText = "DELETE FROM ArticulosCache";
        await del.ExecuteNonQueryAsync();

        var ins = conn.CreateCommand();
        ins.Transaction = tx;
        ins.CommandText = "INSERT INTO ArticulosCache (Id, CodigoBarras, Descripcion, PrecioVenta, StockActual, AlicuotaIva) VALUES ($id, $cb, $desc, $precio, $stock, $iva)";
        var pId = ins.Parameters.Add("$id", SqliteType.Integer);
        var pCb = ins.Parameters.Add("$cb", SqliteType.Text);
        var pDesc = ins.Parameters.Add("$desc", SqliteType.Text);
        var pPrecio = ins.Parameters.Add("$precio", SqliteType.Real);
        var pStock = ins.Parameters.Add("$stock", SqliteType.Real);
        var pIva = ins.Parameters.Add("$iva", SqliteType.Real);

        foreach (var art in items)
        {
            pId.Value = art.Id;
            pCb.Value = art.CodigoBarras;
            pDesc.Value = art.Descripcion;
            pPrecio.Value = (double)art.PrecioVenta;
            pStock.Value = (double)art.StockActual;
            pIva.Value = (double)art.AlicuotaIva;
            await ins.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task<Articulo?> BuscarPorCodigoAsync(string codigo)
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CodigoBarras, Descripcion, PrecioVenta, StockActual, AlicuotaIva FROM ArticulosCache WHERE CodigoBarras = $cb LIMIT 1";
        cmd.Parameters.AddWithValue("$cb", codigo);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new Articulo
        {
            Id = reader.GetInt32(0),
            CodigoBarras = reader.GetString(1),
            Descripcion = reader.GetString(2),
            PrecioVenta = (decimal)reader.GetDouble(3),
            StockActual = (decimal)reader.GetDouble(4),
            AlicuotaIva = (decimal)reader.GetDouble(5)
        };
    }

    /// <summary>Descuenta stock de la caché tras una venta offline, para que el próximo escaneo lo refleje.</summary>
    public async Task AjustarStockAsync(int idArticulo, decimal delta)
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE ArticulosCache SET StockActual = StockActual + $delta WHERE Id = $id";
        cmd.Parameters.AddWithValue("$delta", (double)delta);
        cmd.Parameters.AddWithValue("$id", idArticulo);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EncolarVentaAsync(Comprobante cbte)
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO VentasPendientes (Json, Fecha) VALUES ($json, $fecha)";
        cmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(cbte));
        cmd.Parameters.AddWithValue("$fecha", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<(long Id, Comprobante Cbte)>> ObtenerPendientesAsync()
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Json FROM VentasPendientes ORDER BY Id";
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = new List<(long, Comprobante)>();
        while (await reader.ReadAsync())
        {
            var cbte = JsonSerializer.Deserialize<Comprobante>(reader.GetString(1));
            if (cbte != null) result.Add((reader.GetInt64(0), cbte));
        }
        return result;
    }

    public async Task EliminarPendienteAsync(long id)
    {
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM VentasPendientes WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
