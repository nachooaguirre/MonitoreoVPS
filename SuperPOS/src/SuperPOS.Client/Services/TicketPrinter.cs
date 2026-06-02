using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using SuperPOS.Client.Views.Caja;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Services;

public static class TicketPrinter
{
    public static async Task ImprimirVenta(Comprobante cbte, List<ItemVenta> items, Cliente cliente, string? nombreImpresora, string? mensajePie = null)
    {
        // 1. Descarga del QR en segundo plano si está disponible
        byte[]? qrImageBytes = null;
        if (!string.IsNullOrEmpty(cbte.QrAfip))
        {
            try
            {
                using var http = new HttpClient();
                qrImageBytes = await http.GetByteArrayAsync($"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={Uri.EscapeDataString(cbte.QrAfip)}");
            }
            catch { /* fallback silencioso */ }
        }

        // 2. Generar el texto formateado del ticket (límite estricto de 40 caracteres)
        var lineas = new List<string>();
        lineas.Add("========================================");
        lineas.Add(CentrarText("LOS ANGELES SUPERMERCADOS", 40));
        lineas.Add(CentrarText("CUIT: 30-00000000-0", 40));
        lineas.Add(CentrarText("Casa Central - Bs.As.", 40));
        lineas.Add("========================================");

        string tipoComprobante = cbte.IdTipoComprobante switch
        {
            1 => "FACTURA A",
            2 => "FACTURA B",
            3 => "FACTURA C",
            _ => "TICKET DE VENTA"
        };

        lineas.Add(CentrarText($"{tipoComprobante}", 40));
        lineas.Add(CentrarText($"Nro: {cbte.PuntoVenta:0000}-{cbte.Numero:00000000}", 40));
        lineas.Add($"Fecha: {cbte.Fecha.ToLocalTime():dd/MM/yyyy HH:mm:ss}");
        lineas.Add($"Cliente: {cliente.RazonSocial}");
        if (!string.IsNullOrEmpty(cliente.Cuit) && cliente.Cuit != "00000000000")
            lineas.Add($"CUIT/DNI: {cliente.Cuit}");
        lineas.Add("----------------------------------------");
        lineas.Add("DESCRIPCION          CANTxP.UNIT   TOTAL");
        lineas.Add("----------------------------------------");

        foreach (var item in items)
        {
            var itemLines = FormatearItem(item.Descripcion, item.Cantidad, item.PrecioUnitario, item.SubTotal);
            lineas.AddRange(itemLines);
        }

        lineas.Add("----------------------------------------");
        lineas.Add($"SUBTOTAL:                      ${cbte.SubTotal:N2}".PadLeft(40));
        if (cbte.TotalIva21 > 0)
            lineas.Add($"IVA 21%:                       ${cbte.TotalIva21:N2}".PadLeft(40));
        if (cbte.TotalIva105 > 0)
            lineas.Add($"IVA 10.5%:                     ${cbte.TotalIva105:N2}".PadLeft(40));
        lineas.Add($"TOTAL:                         ${cbte.Total:N2}".PadLeft(40));
        lineas.Add("========================================");

        if (cbte.CAE.HasValue)
        {
            lineas.Add($"CAE: {cbte.CAE}");
            lineas.Add($"Vto CAE: {cbte.CAEVencimiento:dd/MM/yyyy}");
        }

        lineas.Add("");
        if (!string.IsNullOrWhiteSpace(mensajePie))
        {
            lineas.Add(CentrarText(mensajePie, 40));
        }
        else
        {
            lineas.Add(CentrarText("¡Gracias por su compra!", 40));
        }
        lineas.Add("");

        // 3. Ejecutar la impresión física
        try
        {
            using var pd = new PrintDocument();
            
            // Configurar impresora si es válida
            if (!string.IsNullOrWhiteSpace(nombreImpresora) && nombreImpresora != "(Sin impresora de tickets)")
            {
                pd.PrinterSettings.PrinterName = nombreImpresora;
            }

            pd.PrintPage += (sender, e) =>
            {
                float y = 10;
                using var font = new Font("Courier New", 8, System.Drawing.FontStyle.Regular);
                using var boldFont = new Font("Courier New", 9, System.Drawing.FontStyle.Bold);
                float lineSp = font.GetHeight(e.Graphics);

                foreach (var line in lineas)
                {
                    var activeFont = line.Contains("LOS ANGELES SUPERMERCADOS") || line.Contains("TOTAL:") ? boldFont : font;
                    e.Graphics.DrawString(line, activeFont, Brushes.Black, 10, y);
                    y += lineSp;
                }

                // Dibujar el QR si se descargó exitosamente
                if (qrImageBytes != null)
                {
                    try
                    {
                        y += 10;
                        using var ms = new MemoryStream(qrImageBytes);
                        using var img = Image.FromStream(ms);
                        // Centrar aproximadamente en papel de 80mm (ancho de página común es 300px a 96dpi)
                        e.Graphics.DrawImage(img, 70, y, 110, 110);
                        y += 120;
                    }
                    catch { /* Fallback */ }
                }

                e.HasMorePages = false;
            };

            pd.Print();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error físico al imprimir ticket:\n{ex.Message}", "Impresión fallida", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string CentrarText(string text, int width)
    {
        if (text.Length >= width) return text.Substring(0, width);
        int padding = (width - text.Length) / 2;
        return new string(' ', padding) + text;
    }

    private static List<string> FormatearItem(string descripcion, decimal cantidad, decimal precioUnitario, decimal subtotal)
    {
        var lineas = new List<string>();
        var desc = descripcion.Trim();
        var descLines = new List<string>();

        while (desc.Length > 20)
        {
            descLines.Add(desc.Substring(0, 20));
            desc = desc.Substring(20);
        }
        descLines.Add(desc);

        var cantPrecioStr = $"{cantidad:N2}x{precioUnitario:N2}";
        var subtotalStr = $"${subtotal:N2}";

        // Primera línea
        var col1 = descLines[0].PadRight(20).Substring(0, 20);
        var col2 = cantPrecioStr.PadLeft(10).Substring(0, 10);
        var col3 = subtotalStr.PadLeft(10).Substring(0, 10);
        lineas.Add(col1 + col2 + col3);

        // Líneas adicionales para descripción larga
        for (int i = 1; i < descLines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(descLines[i]))
            {
                lineas.Add(descLines[i].Trim().PadRight(40));
            }
        }

        return lineas;
    }
}
