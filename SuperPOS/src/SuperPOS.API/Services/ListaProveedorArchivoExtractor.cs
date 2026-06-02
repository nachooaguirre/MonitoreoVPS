using System.Linq;
using System.Text;
using ClosedXML.Excel;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SuperPOS.API.Services;

public static class ListaProveedorArchivoExtractor
{
    public const int MaxTextLength = 120_000;

    /// <summary>
    /// Si el archivo es una imagen (extensión o cabecera), se envía a la IA por visión.
    /// Rechaza PDF (texto) y ZIP (xlsx) aunque tengan extensión rara, por firma.
    /// </summary>
    public static bool PuedeEnviarAVision(string extension, ReadOnlySpan<byte> datos, out string? mime)
    {
        mime = null;
        if (datos.Length < 4) return false;
        if (datos[0] == 0x25 && datos[1] == 0x50 && datos[2] == 0x44 && datos[3] == 0x46) // %PDF
            return false;
        if (datos[0] == 0x50 && datos[1] == 0x4B) // PK (zip: xlsx, etc.)
            return false;

        var ext = (extension ?? "").ToLowerInvariant();
        if (ext is
                ".png" or ".apng" or
                ".jpg" or ".jpeg" or ".jpe" or ".jif" or ".jfif" or
                ".webp" or ".gif" or ".bmp" or
                ".tif" or ".tiff" or
                ".heic" or ".heif" or
                ".ico" or ".avif")
        {
            mime = ext switch
            {
                ".png" or ".apng" => "image/png",
                ".jpg" or ".jpeg" or ".jpe" or ".jif" or ".jfif" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tif" or ".tiff" => "image/tiff",
                ".heic" or ".heif" => "image/heic",
                ".avif" => "image/avif",
                _ => "image/jpeg"
            };
            return true;
        }

        if (ext is ".pdf" or ".xlsx" or ".xls" or ".csv" or ".txt" or ".doc" or ".docx" or ".xlsm")
            return false;

        if (datos.Length >= 3 && datos[0] == 0xFF && datos[1] == 0xD8 && datos[2] == 0xFF) { mime = "image/jpeg"; return true; }
        if (datos.Length >= 4 && datos[0] == 0x89 && datos[1] == 0x50 && datos[2] == 0x4E && datos[3] == 0x47) { mime = "image/png"; return true; }
        if (datos.Length >= 4 && datos[0] == 0x47 && datos[1] == 0x49 && datos[2] == 0x46 && datos[3] == 0x38) { mime = "image/gif"; return true; }
        if (datos.Length >= 12 && datos[0] == 0x52 && datos[1] == 0x49 && datos[2] == 0x46 && datos[3] == 0x46) { mime = "image/webp"; return true; }
        if (datos.Length >= 2 && datos[0] == 0x42 && datos[1] == 0x4D) { mime = "image/bmp"; return true; }
        if (datos.Length >= 4 && ((datos[0] == 0x49 && datos[1] == 0x49 && datos[2] == 0x2A && datos[3] == 0x00) || (datos[0] == 0x4D && datos[1] == 0x4D && datos[2] == 0x00 && datos[3] == 0x2A)))
        {
            mime = "image/tiff";
            return true;
        }
        if (datos.Length >= 12)
        {
            var ascii = Encoding.ASCII.GetString(datos[..Math.Min(32, datos.Length)]);
            if (ascii.Contains("ftypheic", StringComparison.Ordinal) || ascii.Contains("ftypmif1", StringComparison.Ordinal)
                || ascii.Contains("ftypheix", StringComparison.Ordinal))
            {
                mime = "image/heic";
                return true;
            }
        }
        return false;
    }

    /// <summary>Extrae texto tabular/legible para la IA. Devuelve (texto, aviso si hubo corte).</summary>
    public static (string Texto, string? Aviso) Extraer(string extension, Stream stream, string? nombreHoja = null)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "xlsx" or "xlsm" => ExtraerExcel(stream, nombreHoja),
            "xls" => ("", "xls-legacy: El archivo Excel antiguo (.xls) no se puede leer acá. Abrilo en Excel y guardalo como .xlsx, o exportá a CSV, y subí de nuevo."),
            "csv" or "txt" => ExtraerTextoPlano(stream),
            "pdf" => ExtraerPdf(stream),
            _ => ("", $"Extensión no soportada para extracción de texto: {ext}. Imágenes (jpg, png, webp, etc.) se leen con la IA por visión sin convertir a texto previo.")
        };
    }

    private static (string, string?) ExtraerTextoPlano(Stream stream)
    {
        using var r = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var t = r.ReadToEnd();
        if (t.Length > MaxTextLength) return (t[..MaxTextLength], $"Texto truncado a {MaxTextLength} caracteres.");
        return (t, null);
    }

    private static (string, string?) ExtraerExcel(Stream stream, string? nombreHoja)
    {
        using var wb = new XLWorkbook(stream);
        IXLWorksheet ws = wb.Worksheets.First();
        if (!string.IsNullOrWhiteSpace(nombreHoja))
        {
            var f = wb.Worksheets.FirstOrDefault(x => string.Equals(x.Name, nombreHoja, StringComparison.OrdinalIgnoreCase));
            if (f != null) ws = f;
        }

        var sb = new StringBuilder();
        var used = ws.RangeUsed();
        if (used is null) return ("(hoja vacía)", "La primera hoja no tiene celdas usadas.");
        int rowMax = used.LastRow().RowNumber();
        int colMax = used.LastColumn().ColumnNumber();
        for (int r = 1; r <= rowMax; r++)
        {
            for (int c = 1; c <= colMax; c++)
            {
                if (c > 1) sb.Append('\t');
                sb.Append(ws.Cell(r, c).GetString());
            }
            sb.AppendLine();
        }
        var t = sb.ToString();
        if (t.Length > MaxTextLength) return (t[..MaxTextLength], $"Excel: texto truncado a {MaxTextLength} caracteres.");
        return (t, null);
    }

    private static (string, string?) ExtraerPdf(Stream stream)
    {
        try
        {
            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(stream);
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            var t = sb.ToString();
            if (string.IsNullOrWhiteSpace(t)) return ("", "No se extrajo texto del PDF. Si es escaneado, exportá a imagen o usá un PDF con texto seleccionable. También probá con fotos o Excel.");
            if (t.Length > MaxTextLength) return (t[..MaxTextLength], "PDF: texto truncado para la IA.");
            return (t, null);
        }
        catch (Exception ex)
        {
            var m = ex.Message;
            if (m.Length > 200) m = m[..197] + "…";
            return ("", "PDF: no se pudo abrir. " + m + " Tip: actualizá la API, reinstalá el paquete NuGet PdfPig, o subí el archivo como imagen/Excel.");
        }
    }
}
