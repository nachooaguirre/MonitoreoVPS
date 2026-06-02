using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API;
using SuperPOS.API.Data;
using SuperPOS.API.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/listas-precio-proveedor")]
public class ListasPrecioProveedorController(
    SuperPOSDbContext db,
    IAiService ai,
    IWebHostEnvironment env,
    ILogger<ListasPrecioProveedorController> log) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idProveedor)
    {
        IQueryable<ListaPrecioProveedor> q = db.ListasPrecioProveedor.AsNoTracking().Where(x => x.Activo);
        if (idProveedor.HasValue) q = q.Where(x => x.IdProveedor == idProveedor.Value);
        var listas = await q
            .OrderByDescending(x => x.FechaCargaUtc)
            .Select(x => new
            {
                x.Id,
                x.Nombre,
                x.IdProveedor,
                proveedor = x.Proveedor == null ? null : x.Proveedor.RazonSocial,
                x.FechaCargaUtc,
                x.ArchivoOrigenNombre,
                lineasCount = x.Lineas.Count
            })
            .ToListAsync();
        return Ok(listas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var x = await db.ListasPrecioProveedor
            .AsNoTracking()
            .Include(l => l.Proveedor)
            .Include(l => l.Lineas)
            .ThenInclude(ln => ln.Articulo)
            .FirstOrDefaultAsync(l => l.Id == id);
        return x is null ? NotFound() : Ok(x);
    }

    /// <summary>Importa tarifa del proveedor: genera <see cref="ListaPrecioProveedorLinea"/>. No crea artículos ni stock; el inventario se mueve solo por compras, remitos o transferencias; usá "matchear" para asociar a <see cref="Articulo"/> existentes.</summary>
    [HttpPost("importar")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Importar([FromForm] int idProveedor, [FromForm] string? nombre, IFormFile? file, [FromForm] string? textoPegado, CancellationToken ct)
    {
        var prov = await db.Proveedores.FindAsync(idProveedor, ct);
        if (prov is null) return BadRequest(new { error = "Proveedor inválido." });

        var pegadoLimpio = (textoPegado ?? "").Trim();
        var tieneFile = file is { Length: > 0 };
        if (!tieneFile && string.IsNullOrEmpty(pegadoLimpio))
            return BadRequest(new { error = "Enviá un archivo o pegá el texto de la lista (por ejemplo de WhatsApp)." });

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = tieneFile && file is not null
                ? Path.GetFileNameWithoutExtension(file.FileName)
                : "Pegado manual";

        string? archNombre;
        string ext;
        byte[]? bytesCuerpo;
        if (tieneFile && file is not null)
        {
            archNombre = file.FileName;
            ext = Path.GetExtension(archNombre).ToLowerInvariant();
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            bytesCuerpo = ms.ToArray();
        }
        else
        {
            archNombre = "texto-pegado.txt";
            ext = ".txt";
            bytesCuerpo = Encoding.UTF8.GetBytes(pegadoLimpio);
        }

        string? texto;
        string? b64;
        string? mime;
        string? aviso;
        if (tieneFile && bytesCuerpo is not null && ListaProveedorArchivoExtractor.PuedeEnviarAVision(ext, bytesCuerpo, out var mimeVision))
        {
            texto = null;
            b64 = Convert.ToBase64String(bytesCuerpo);
            mime = mimeVision;
            aviso = "IMAGEN";
        }
        else if (!tieneFile)
        {
            b64 = null;
            mime = null;
            texto = pegadoLimpio;
            aviso = texto.Length > ListaProveedorArchivoExtractor.MaxTextLength
                ? $"Texto recortado a {ListaProveedorArchivoExtractor.MaxTextLength} caracteres."
                : null;
            if (texto.Length > ListaProveedorArchivoExtractor.MaxTextLength)
                texto = texto[..ListaProveedorArchivoExtractor.MaxTextLength];
        }
        else
        {
            b64 = null;
            mime = null;
            await using var s2 = new MemoryStream(bytesCuerpo!);
            (texto, aviso) = ListaProveedorArchivoExtractor.Extraer(ext, s2);
            if (aviso?.StartsWith("xls-legacy", StringComparison.OrdinalIgnoreCase) == true)
                return BadRequest(new { error = aviso });
            if (string.IsNullOrWhiteSpace(texto) && string.IsNullOrEmpty(b64))
                return BadRequest(new { error = "No se pudo extraer texto. Guardá el Excel como .xlsx, o probá con CSV, PDF o imagen nítida." });
        }

        var parse = await ai.EstructurarListaProveedorAsync(texto, b64, mime, prov.RazonSocial, ct);
        if (!parse.Exito || parse.Lineas.Count == 0)
            return UnprocessableEntity(new
            {
                error = parse.Error ?? "No se generaron líneas",
                detalle = parse.AvisoOrigen,
                avisoOrigen = aviso
            });

        var dir = Path.Combine(env.ContentRootPath, "Data", "uploads", "listas-proveedor");
        Directory.CreateDirectory(dir);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var rel = Path.Combine("Data", "uploads", "listas-proveedor", safeName);
        var full = Path.Combine(env.ContentRootPath, rel);
        await System.IO.File.WriteAllBytesAsync(full, bytesCuerpo!, ct);

        var notas = string.IsNullOrEmpty(parse.AvisoOrigen) ? aviso : $"{aviso}; {parse.AvisoOrigen}".Trim(';', ' ');
        var lista = new ListaPrecioProveedor
        {
            IdProveedor = idProveedor,
            Nombre = nombre.Trim(),
            Notas = string.IsNullOrWhiteSpace(notas) ? null : notas,
            FechaCargaUtc = DateTime.UtcNow,
            ArchivoOrigenNombre = archNombre,
            ArchivoOrigenRutaRelativa = rel.Replace("\\", "/"),
            Activo = true
        };
        db.ListasPrecioProveedor.Add(lista);
        await db.SaveChangesAsync(ct);

        foreach (var l in parse.Lineas)
        {
            var bjson = l.Bonificaciones.Count == 0
                ? "[]"
                : JsonSerializer.Serialize(l.Bonificaciones);
            db.ListasPrecioProveedorLineas.Add(new ListaPrecioProveedorLinea
            {
                IdLista = lista.Id,
                CodigoProveedor = l.CodigoProveedor,
                Descripcion = l.Descripcion,
                PrecioUnitario = l.PrecioUnitario,
                IvaPorcentaje = l.IvaPorcentaje,
                BonificacionesJson = bjson
            });
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Lista proveedor creada id={Id} proveedor={Prov} líneas={N}", lista.Id, idProveedor, parse.Lineas.Count);
        return Ok(new { id = lista.Id, lineas = parse.Lineas.Count, aviso = aviso is not null and not "IMAGEN" ? aviso : null });
    }

    /// <summary>
    /// Vincula cada línea a un <see cref="Articulo"/> preexistente. Prioriza el mismo <see cref="Proveedor"/>
    /// que la lista. Si en depósito los artículos no tienen ese IdProveedor (o faltan códigos en el OCR),
    /// repite el intento con EAN/cód. proveedor o interno <b>únicos en todo el catálogo</b> (p. ej. EAN
    /// suele ser el mismo producto aunque el proveedor en ficha no coincida aún).
    /// </summary>
    [HttpPost("{id:int}/matchear-articulos")]
    public async Task<IActionResult> Matchear(int id, CancellationToken ct)
    {
        var lista = await db.ListasPrecioProveedor.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lista is null) return NotFound();
        var lineas = await db.ListasPrecioProveedorLineas.Where(x => x.IdLista == id).ToListAsync(ct);
        var idProv = lista.IdProveedor;

        var artRows = await db.Articulos
            .AsNoTracking()
            .Where(a => a.Activo)
            .Select(a => new
            {
                a.Id,
                a.IdProveedor,
                a.CodigoBarras,
                a.CodigoProveedor,
                a.CodigoInterno,
                a.Descripcion,
                a.DescripcionCorta
            })
            .ToListAsync(ct);

        var eanAlt = await db.ArticulosCodigoBarras
            .AsNoTracking()
            .Select(c => new { c.IdArticulo, c.CodigoBarras })
            .ToListAsync(ct);

        var provArts = artRows.Where(a => a.IdProveedor == idProv).ToList();
        var provIds = new HashSet<int>(provArts.Select(x => x.Id));
        var byCpP = provArts.Where(a => !string.IsNullOrEmpty(a.CodigoProveedor))
            .ToLookup(a => a.CodigoProveedor.Trim(), a => a.Id, StringComparer.OrdinalIgnoreCase);
        var byCiP = provArts.Where(a => !string.IsNullOrEmpty(a.CodigoInterno))
            .ToLookup(a => a.CodigoInterno.Trim(), a => a.Id, StringComparer.OrdinalIgnoreCase);
        var byEanP = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byEanDigP = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var a in provArts) AgregarEanEnDiccionarios(a.Id, a.CodigoBarras, byEanP, byEanDigP);
        foreach (var e in eanAlt)
        {
            if (provIds.Contains(e.IdArticulo)) AgregarEanEnDiccionarios(e.IdArticulo, e.CodigoBarras, byEanP, byEanDigP);
        }
        var byDescP = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in provArts) AgregarDescripcionesClave(a.Id, a.Descripcion, a.DescripcionCorta, byDescP);

        var byEanG = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byEanDigG = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var a in artRows) AgregarEanEnDiccionarios(a.Id, a.CodigoBarras, byEanG, byEanDigG);
        foreach (var e in eanAlt) AgregarEanEnDiccionarios(e.IdArticulo, e.CodigoBarras, byEanG, byEanDigG);

        var cpUnico = artRows
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigoProveedor))
            .GroupBy(a => a.CodigoProveedor!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var ciUnico = artRows
            .Where(a => !string.IsNullOrWhiteSpace(a.CodigoInterno))
            .GroupBy(a => a.CodigoInterno!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var n = 0;
        foreach (var line in lineas)
        {
            int? idArt;
            if (ResolucionMatcheoProveedor(line, byCpP, byCiP, byEanP, byEanDigP, byDescP) is { } a1)
                idArt = a1;
            else if (ResolucionMatcheoGlobal(line, byEanG, byEanDigG, cpUnico, ciUnico) is { } a2)
                idArt = a2;
            else
                idArt = null;
            if (idArt is { } ok && ok > 0) { line.IdArticulo = ok; n++; }
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { vinculados = n, total = lineas.Count });
    }

    private static void AgregarDescripcionesClave(
        int idArt, string? descripcion, string? descripcionCorta, IDictionary<string, int> d)
    {
        void Uno(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            var k = NormDesc(s);
            if (k.Length == 0) return;
            d.TryAdd(k, idArt);
        }
        Uno(descripcion);
        Uno(descripcionCorta);
    }

    private static void AgregarEanEnDiccionarios(
        int idArticulo, string? codigo, Dictionary<string, int> ean, Dictionary<string, int> eanSoloD)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return;
        var t = codigo.Trim();
        if (!ean.ContainsKey(t)) ean[t] = idArticulo;
        var d = SoloDigitos(t);
        if (d.Length is < 8 or > 14) return;
        if (!eanSoloD.ContainsKey(d)) eanSoloD[d] = idArticulo;
    }

    private static int? ResolucionMatcheoProveedor(
        ListaPrecioProveedorLinea line,
        ILookup<string, int> byCp,
        ILookup<string, int> byCi,
        Dictionary<string, int> byEan,
        Dictionary<string, int> byEanDig,
        IReadOnlyDictionary<string, int> byDesc)
    {
        foreach (var cand in CandidatosParaMatcheo(line).Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(cand)) continue;
            if (byCp.Contains(cand) && byCp[cand].FirstOrDefault() is int z and > 0) return z;
            if (byCi.Contains(cand) && byCi[cand].FirstOrDefault() is int y and > 0) return y;
            if (byEan.TryGetValue(cand, out var e1) && e1 > 0) return e1;
            var dig = SoloDigitos(cand);
            if (dig.Length is >= 8 and <= 14 && byEanDig.TryGetValue(dig, out var e2) && e2 > 0) return e2;
        }
        if (!string.IsNullOrWhiteSpace(line.Descripcion) &&
            byDesc.TryGetValue(NormDesc(line.Descripcion), out var d0) && d0 > 0)
            return d0;
        return null;
    }

    private static int? ResolucionMatcheoGlobal(
        ListaPrecioProveedorLinea line,
        Dictionary<string, int> byEanG,
        Dictionary<string, int> byEanDigG,
        IReadOnlyDictionary<string, int> cpUnico,
        IReadOnlyDictionary<string, int> ciUnico)
    {
        foreach (var cand in CandidatosParaMatcheo(line).Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(cand)) continue;
            if (byEanG.TryGetValue(cand, out var e1) && e1 > 0) return e1;
            var dig = SoloDigitos(cand);
            if (dig.Length is >= 8 and <= 14 && byEanDigG.TryGetValue(dig, out var e2) && e2 > 0) return e2;
            if (cpUnico.TryGetValue(cand, out var u3) && u3 > 0) return u3;
            if (ciUnico.TryGetValue(cand, out var u4) && u4 > 0) return u4;
        }
        if (!string.IsNullOrWhiteSpace(line.CodigoProveedor))
        {
            var t = line.CodigoProveedor.Trim();
            if (cpUnico.TryGetValue(t, out var u1) && u1 > 0) return u1;
            var sinEsp = t.Replace(" ", "", StringComparison.Ordinal);
            if (sinEsp != t && cpUnico.TryGetValue(sinEsp, out var u2) && u2 > 0) return u2;
        }
        return null;
    }

    private static string NormDesc(string? s) =>
        string.IsNullOrWhiteSpace(s) ? "" : Regex.Replace(s.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);

    private static string SoloDigitos(string s) => new(s.Where(char.IsDigit).ToArray());

    private static IEnumerable<string> CandidatosParaMatcheo(ListaPrecioProveedorLinea line)
    {
        if (!string.IsNullOrWhiteSpace(line.CodigoProveedor))
        {
            var t = line.CodigoProveedor.Trim();
            yield return t;
            var sinEsp = t.Replace(" ", "", StringComparison.Ordinal);
            if (sinEsp != t) yield return sinEsp;
            var solo = SoloDigitos(t);
            if (solo.Length is >= 8 and <= 14) yield return solo;
        }
        var desc = line.Descripcion ?? "";
        foreach (Match m in Regex.Matches(desc, @"\b\d{8,14}\b", RegexOptions.CultureInvariant))
            yield return m.Value;
        foreach (Match m in Regex.Matches(desc, @"\d{8,14}", RegexOptions.CultureInvariant))
        {
            if (m.Value.Length is >= 8 and <= 14) yield return m.Value;
        }
    }

    [HttpPut("linea/{idLinea:int}")]
    public async Task<IActionResult> UpdateLinea(int idLinea, [FromBody] ListaLineaUpdateDto? dto, CancellationToken ct)
    {
        if (dto is null) return BadRequest();
        var line = await db.ListasPrecioProveedorLineas.FirstOrDefaultAsync(x => x.Id == idLinea, ct);
        if (line is null) return NotFound();
        if (dto.CodigoProveedor != null) line.CodigoProveedor = dto.CodigoProveedor;
        if (dto.Descripcion != null) line.Descripcion = dto.Descripcion;
        if (dto.PrecioUnitario.HasValue) line.PrecioUnitario = dto.PrecioUnitario.Value;
        if (dto.IvaPorcentaje.HasValue) line.IvaPorcentaje = dto.IvaPorcentaje;
        if (dto.BonificacionesJson != null) line.BonificacionesJson = dto.BonificacionesJson;
        if (dto.IdArticulo.HasValue) line.IdArticulo = dto.IdArticulo;
        await db.SaveChangesAsync(ct);
        return Ok(line);
    }

    [HttpPost("{id:int}/linea")]
    public async Task<IActionResult> AddLinea(int id, [FromBody] ListaLineaUpdateDto dto, CancellationToken ct)
    {
        var lista = await db.ListasPrecioProveedor.FindAsync(new object?[] { id }, ct);
        if (lista is null) return NotFound();
        var line = new ListaPrecioProveedorLinea
        {
            IdLista = id,
            CodigoProveedor = dto.CodigoProveedor ?? "",
            Descripcion = dto.Descripcion ?? "",
            PrecioUnitario = dto.PrecioUnitario ?? 0,
            IvaPorcentaje = dto.IvaPorcentaje,
            BonificacionesJson = string.IsNullOrEmpty(dto.BonificacionesJson) ? "[]" : dto.BonificacionesJson,
            IdArticulo = dto.IdArticulo
        };
        db.ListasPrecioProveedorLineas.Add(line);
        await db.SaveChangesAsync(ct);
        return Ok(line);
    }

    [HttpDelete("linea/{idLinea:int}")]
    public async Task<IActionResult> DeleteLinea(int idLinea, CancellationToken ct)
    {
        var line = await db.ListasPrecioProveedorLineas.FindAsync(new object?[] { idLinea }, ct);
        if (line is null) return NotFound();
        db.ListasPrecioProveedorLineas.Remove(line);
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLista(int id, CancellationToken ct)
    {
        var lista = await db.ListasPrecioProveedor.Include(x => x.Lineas).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (lista is null) return NotFound();
        lista.Activo = false;
        await db.SaveChangesAsync(ct);
        return Ok();
    }
}
