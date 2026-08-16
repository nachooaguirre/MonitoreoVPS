using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SuperPOS.Client.Views.Reportes;

public partial class ContabilidadPage : Page
{
    public class MesItem
    {
        public int Valor { get; set; }
        public string Nombre { get; set; } = "";
    }

    public ContabilidadPage()
    {
        InitializeComponent();
        TxtFechaHoy.Text = DateTime.Now.ToString("dddd dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-AR"));
        InicializarCombos();
    }

    private void InicializarCombos()
    {
        // Cargar Meses
        var meses = new List<MesItem>
        {
            new() { Valor = 1, Nombre = "Enero" },
            new() { Valor = 2, Nombre = "Febrero" },
            new() { Valor = 3, Nombre = "Marzo" },
            new() { Valor = 4, Nombre = "Abril" },
            new() { Valor = 5, Nombre = "Mayo" },
            new() { Valor = 6, Nombre = "Junio" },
            new() { Valor = 7, Nombre = "Julio" },
            new() { Valor = 8, Nombre = "Agosto" },
            new() { Valor = 9, Nombre = "Septiembre" },
            new() { Valor = 10, Nombre = "Octubre" },
            new() { Valor = 11, Nombre = "Noviembre" },
            new() { Valor = 12, Nombre = "Diciembre" }
        };
        CboMes.ItemsSource = meses;
        CboMes.SelectedValue = DateTime.Today.Month;

        // Cargar Años (Desde 2024 hasta año actual + 1)
        var anioActual = DateTime.Today.Year;
        var anios = new List<int>();
        for (int i = 2024; i <= anioActual + 1; i++)
        {
            anios.Add(i);
        }
        CboAnio.ItemsSource = anios;
        CboAnio.SelectedItem = anioActual;
    }

    private int ObtenerMes() => (int)(CboMes.SelectedValue ?? DateTime.Today.Month);
    private int ObtenerAnio() => (int)(CboAnio.SelectedItem ?? DateTime.Today.Year);

    private async Task DescargarArchivo(string defaultFileName, Func<int, int, Task<byte[]?>> apiCall)
    {
        int mes = ObtenerMes();
        int anio = ObtenerAnio();

        // Reemplazar placeholders en el nombre de archivo sugerido
        string fileName = defaultFileName
            .Replace("{mes}", mes.ToString("D2"))
            .Replace("{anio}", anio.ToString());

        var sfd = new SaveFileDialog
        {
            FileName = fileName,
            Filter = defaultFileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                ? "Archivos CSV (*.csv)|*.csv" 
                : "Archivos de texto (*.txt)|*.txt",
            Title = "Guardar Reporte Contable"
        };

        if (sfd.ShowDialog() != true) return;

        MostrarProgreso($"Descargando {fileName}...");

        try
        {
            var data = await apiCall(mes, anio);
            if (data == null || data.Length == 0)
            {
                throw new Exception("El servidor devolvió un archivo vacío o sin datos.");
            }

            await File.WriteAllBytesAsync(sfd.FileName, data);
            OcultarProgreso();
            MessageBox.Show("Archivo guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            OcultarProgreso();
            MessageBox.Show($"Error al descargar el reporte:\n{ex.Message}", "Error de Descarga", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MostrarProgreso(string mensaje)
    {
        TxtEstadoProgreso.Text = mensaje;
        BorderProgreso.Visibility = Visibility.Visible;
        IsEnabled = false;
    }

    private void OcultarProgreso()
    {
        BorderProgreso.Visibility = Visibility.Collapsed;
        IsEnabled = true;
    }

    // ─── Eventos de Botones Individuales ──────────────────────

    private async void BtnVentasCbte_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("LIBRO_IVA_DIGITAL_VENTAS_CBTE_{anio}{mes}.TXT", App.Api.DownloadLibroIvaVentasCbte);
    }

    private async void BtnVentasAlic_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("LIBRO_IVA_DIGITAL_VENTAS_ALICUOTAS_{anio}{mes}.TXT", App.Api.DownloadLibroIvaVentasAlic);
    }

    private async void BtnComprasCbte_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("COMPRAS_base_{anio}{mes}.txt", App.Api.DownloadLibroIvaComprasCbte);
    }

    private async void BtnComprasAlic_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("COMPRAS_base_alícuotas_{anio}{mes}.txt", App.Api.DownloadLibroIvaComprasAlic);
    }

    private async void BtnPercepcionesVentas_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("AR-30702841352-{anio}{mes}0-P7-LOTE2.txt", App.Api.DownloadPercepcionesIvaVentas);
    }

    private async void BtnPercepcionesCompras_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("AR-30702841352-{anio}{mes}2-6-LOTE1.txt", App.Api.DownloadPercepcionesIIBBCompras);
    }

    private async void BtnResumenVentas_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("resumen_ventas_{anio}{mes}.csv", App.Api.DownloadResumenVentasCsv);
    }

    private async void BtnResumenCompras_Click(object sender, RoutedEventArgs e)
    {
        await DescargarArchivo("resumen_compras_{anio}{mes}.csv", App.Api.DownloadResumenComprasCsv);
    }

    // ─── Descarga en Lote (Todos los Reportes) ────────────────

    private async void BtnDescargarTodo_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFolderDialog
        {
            Title = "Seleccionar Carpeta para Guardar Todos los Reportes"
        };

        if (ofd.ShowDialog() != true) return;

        string folderPath = ofd.FolderName;
        int mes = ObtenerMes();
        int anio = ObtenerAnio();
        string periodStr = $"{anio}{mes:D2}";

        var descargas = new List<(string NamePattern, Func<int, int, Task<byte[]?>> Call)>
        {
            ($"LIBRO_IVA_DIGITAL_VENTAS_CBTE_{periodStr}.TXT", App.Api.DownloadLibroIvaVentasCbte),
            ($"LIBRO_IVA_DIGITAL_VENTAS_ALICUOTAS_{periodStr}.TXT", App.Api.DownloadLibroIvaVentasAlic),
            ($"COMPRAS_base_{periodStr}.txt", App.Api.DownloadLibroIvaComprasCbte),
            ($"COMPRAS_base_alícuotas_{periodStr}.txt", App.Api.DownloadLibroIvaComprasAlic),
            ($"AR-30702841352-{periodStr}0-P7-LOTE2.txt", App.Api.DownloadPercepcionesIvaVentas),
            ($"AR-30702841352-{periodStr}2-6-LOTE1.txt", App.Api.DownloadPercepcionesIIBBCompras),
            ($"resumen_ventas_{periodStr}.csv", App.Api.DownloadResumenVentasCsv),
            ($"resumen_compras_{periodStr}.csv", App.Api.DownloadResumenComprasCsv)
        };

        int exitos = 0;
        int errores = 0;
        var detallesErrores = new List<string>();

        for (int i = 0; i < descargas.Count; i++)
        {
            var item = descargas[i];
            MostrarProgreso($"Descargando ({i + 1}/{descargas.Count}): {item.NamePattern}...");

            try
            {
                var data = await item.Call(mes, anio);
                if (data != null && data.Length > 0)
                {
                    string filePath = Path.Combine(folderPath, item.NamePattern);
                    await File.WriteAllBytesAsync(filePath, data);
                    exitos++;
                }
                else
                {
                    errores++;
                    detallesErrores.Add($"{item.NamePattern}: El servidor devolvió un archivo vacío.");
                }
            }
            catch (Exception ex)
            {
                errores++;
                detallesErrores.Add($"{item.NamePattern}: {ex.Message}");
            }
        }

        OcultarProgreso();

        if (errores == 0)
        {
            MessageBox.Show($"¡Descarga finalizada con éxito!\nSe descargaron los {exitos} reportes en:\n{folderPath}", 
                "Descarga en Lote Completa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            string msj = $"Descarga finalizada con advertencias.\n\nExitosos: {exitos}\nFallidos: {errores}\n\nDetalles de fallas:\n" + string.Join("\n", detallesErrores);
            MessageBox.Show(msj, "Resultados de Descarga en Lote", MessageBoxButton.OK, errores == descargas.Count ? MessageBoxImage.Error : MessageBoxImage.Warning);
        }
    }
}
