using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SuperPOS.Client.Models;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Inventario;

public partial class InventarioDetalleViewWindow : Window
{
    public InventarioDetalleViewWindow(int idInventario)
    {
        InitializeComponent();
        Loaded += async (_, _) => await Cargar(idInventario);
    }

    private async Task Cargar(int idInventario)
    {
        try
        {
            var inv = await App.Api.GetInventarioById(idInventario);
            if (inv is null) { MessageBox.Show("No se encontró el inventario.", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information); Close(); return; }

            TxtTitulo.Text = inv.Descripcion;
            TxtSub.Text = $"Inicio: {inv.FechaInicio.ToLocalTime():dd/MM/yyyy HH:mm}" +
                (inv.FechaCierre.HasValue ? $"  ·  Cierre: {inv.FechaCierre.Value.ToLocalTime():dd/MM/yyyy HH:mm}" : "");
            TxtSucursal.Text = inv.Sucursal?.Nombre ?? $"#{inv.IdSucursal}";

            var (label, bg, fg) = inv.Estado switch
            {
                EstadoInventario.EnProceso => ("En proceso", "#1A2530", "#FFB040"),
                EstadoInventario.Cerrado => ("Cerrado", "#101828", "#60B4FF"),
                _ => ("Aplicado al stock", "#0A1A0C", "#40D080")
            };
            TxtEstado.Text = label;
            BrdEstado.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);
            TxtEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)!);

            var contadosFue = inv.Detalles?.Count(d => d.FueConteado) ?? 0;
            TxtContados.Text = $"{contadosFue} / {inv.TotalArticulos}";

            var difVal = inv.Detalles?.Where(d => d.FueConteado).Sum(d => (d.StockContado - d.StockSistema) * d.PrecioCosto) ?? 0;
            TxtDifVal.Text = difVal.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"));

            var filas = (inv.Detalles ?? [])
                .OrderBy(d => d.Articulo?.Descripcion)
                .Select(d => new InventarioDetalleDto
                {
                    Id = d.Id,
                    IdInventario = d.IdInventario,
                    IdArticulo = d.IdArticulo,
                    CodigoBarras = d.Articulo?.CodigoBarras,
                    Descripcion = d.Articulo?.Descripcion ?? $"Art. #{d.IdArticulo}",
                    StockSistema = d.StockSistema,
                    StockContado = d.StockContado,
                    FueConteado = d.FueConteado,
                    PrecioCosto = d.PrecioCosto
                })
                .ToList();
            Dg.ItemsSource = filas;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); Close(); }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();
}
