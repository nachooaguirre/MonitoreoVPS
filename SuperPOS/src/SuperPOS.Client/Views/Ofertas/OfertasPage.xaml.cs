using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Ofertas;

public partial class OfertasPage : Page
{
    private List<Oferta> _ofertas = [];

    public OfertasPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarOfertas();
    }

    private async Task CargarOfertas()
    {
        try
        {
            var list = await App.Api.GetOfertas();
            _ofertas = list ?? [];
            DgOfertas.ItemsSource = _ofertas;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar ofertas: {ex.Message}", "Ofertas", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DgOfertas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgOfertas.SelectedItem is Oferta o)
        {
            TxtInfoVacia.Visibility = Visibility.Collapsed;
            GridDetallesRendimiento.Visibility = Visibility.Visible;

            TxtResumenVendida.Text = $"{o.CantidadVendida:N1} u.";
            TxtResumenLimite.Text = o.LimiteStock.HasValue ? $"{o.LimiteStock.Value:N1} u." : "Sin límite";
            TxtResumenPrecioReg.Text = o.Articulo != null ? o.Articulo.PrecioVenta.ToString("$ #,##0.00") : "$ 0.00";
            TxtResumenPrecioPromo.Text = o.PrecioOferta.ToString("$ #,##0.00");

            if (o.LimiteStock.HasValue && o.LimiteStock.Value > 0)
            {
                PbProgresoStock.Visibility = Visibility.Visible;
                var pct = (double)(o.CantidadVendida / o.LimiteStock.Value * 100);
                PbProgresoStock.Value = Math.Min(100, Math.Max(0, pct));
                TxtProgresoPorcentaje.Text = $"{PbProgresoStock.Value:F1}% agotado";
                if (pct >= 100)
                {
                    TxtProgresoPorcentaje.Text += " (Agotada)";
                    PbProgresoStock.Foreground = System.Windows.Media.Brushes.Crimson;
                }
                else
                {
                    PbProgresoStock.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            else
            {
                PbProgresoStock.Visibility = Visibility.Collapsed;
                TxtProgresoPorcentaje.Text = "Sin límite de stock (vigente por rango de fechas)";
            }

            _ = CargarGrafica(o.Id);
        }
        else
        {
            TxtInfoVacia.Visibility = Visibility.Visible;
            GridDetallesRendimiento.Visibility = Visibility.Collapsed;
            ChartVentas.Series = [];
            TxtChartSinDatos.Visibility = Visibility.Collapsed;
        }
    }

    private async Task CargarGrafica(int idOferta)
    {
        try
        {
            var puntos = await App.Api.GetGraficaVentasOferta(idOferta);
            if (puntos != null && puntos.Count > 0)
            {
                TxtChartSinDatos.Visibility = Visibility.Collapsed;

                ChartVentas.Series = [
                    new ColumnSeries<double>
                    {
                        Name = "Cantidad Vendida",
                        Values = puntos.Select(p => (double)p.Cantidad).ToArray(),
                        Fill = new LinearGradientPaint(
                            new[] { SKColor.Parse("#FF8000"), SKColor.Parse("#FFA834") }, // Warm flames gradient
                            new SKPoint(0, 1), new SKPoint(0, 0)),
                        Stroke = null,
                        MaxBarWidth = 24,
                        Rx = 4, Ry = 4,
                    }
                ];

                ChartVentas.XAxes = [
                    new Axis
                    {
                        Labels = puntos.Select(p => p.FechaLabel).ToArray(),
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#8090A0")),
                        SeparatorsPaint = null,
                        TicksPaint = null,
                    }
                ];

                ChartVentas.YAxes = [
                    new Axis
                    {
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#8090A0")),
                        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#252525")) { StrokeThickness = 1 },
                        MinLimit = 0
                    }
                ];
            }
            else
            {
                ChartVentas.Series = [];
                TxtChartSinDatos.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            ChartVentas.Series = [];
            TxtChartSinDatos.Visibility = Visibility.Visible;
        }
    }

    private async void BtnNuevaOferta_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevaOfertaWindow(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await CargarOfertas();
        }
    }

    private async void BtnEditar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Oferta o)
        {
            var dlg = new NuevaOfertaWindow(o) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                await CargarOfertas();
            }
        }
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Oferta o)
        {
            if (MessageBox.Show($"¿Eliminar la oferta \"{o.Detalle}\"?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await App.Api.EliminarOferta(o.Id);
                    await CargarOfertas();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar la oferta: {ex.Message}", "Error");
                }
            }
        }
    }
}
