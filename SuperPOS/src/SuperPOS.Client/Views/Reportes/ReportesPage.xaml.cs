using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using SuperPOS.Client.Models;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Reportes;

public partial class ReportesPage : Page
{
    public ReportesPage()
    {
        InitializeComponent();
        TxtFechaHoy.Text = DateTime.Now.ToString("dddd dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-AR"));
        DpFechaDia.SelectedDate      = DateTime.Today;
        DpFechaHistorial.SelectedDate = DateTime.Today;
        DpDesde.SelectedDate         = DateTime.Today.AddMonths(-1);
        DpHasta.SelectedDate         = DateTime.Today;
        DpRankingDesde.SelectedDate  = DateTime.Today.AddMonths(-1);
        DpRankingHasta.SelectedDate  = DateTime.Today;
        DpRentabilidadDesde.SelectedDate = DateTime.Today.AddMonths(-1);
        DpRentabilidadHasta.SelectedDate = DateTime.Today;

        Loaded += async (_, _) =>
        {
            await CargarSucursalesFiltro();
            await ConsultarVentasDia();
            await ConsultarHistorial();
            await ConsultarStockBajo();
            await CargarProveedoresCalendario();
            await ConsultarCalendarioPagos();
            await ConsultarRentabilidadProveedores();
        };
    }

    // ─── Filtro de sucursal (global para las pestañas de ventas) ──
    /// <summary>Sucursal elegida en el filtro, o null = todas las que el usuario puede ver.</summary>
    private int? IdSucursalFiltro => CboSucursalFiltro.SelectedValue is int id && id > 0 ? id : null;

    private async Task CargarSucursalesFiltro()
    {
        try
        {
            var sucursales = await App.Api.GetMisSucursales();
            var conTodas = new List<SucursalSimpleDto> { new() { Id = 0, Nombre = "(Todas)" } };
            conTodas.AddRange(sucursales);
            CboSucursalFiltro.ItemsSource = conTodas;
            CboSucursalFiltro.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show($"Error al cargar sucursales: {ex.Message}"); }
    }

    private async void CboSucursalFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await ConsultarVentasDia();
        await ConsultarHistorial();
        await ConsultarPeriodo();
        await ConsultarRanking();
    }

    // ─── Eventos ──────────────────────────────────────────────
    private async void BtnVentasDia_Click(object s, RoutedEventArgs e) => await ConsultarVentasDia();
    private async void BtnConsultarHistorial_Click(object s, RoutedEventArgs e) => await ConsultarHistorial();
    private async void BtnPeriodo_Click(object s, RoutedEventArgs e)   => await ConsultarPeriodo();
    private async void BtnRanking_Click(object s, RoutedEventArgs e)   => await ConsultarRanking();
    private async void BtnStockBajo_Click(object s, RoutedEventArgs e) => await ConsultarStockBajo();
    private async void BtnCalendarioPagos_Click(object s, RoutedEventArgs e) => await ConsultarCalendarioPagos();
    private async void BtnRentabilidadProveedores_Click(object s, RoutedEventArgs e) => await ConsultarRentabilidadProveedores();

    // ─── Paleta de colores para gráficos ──────────────────────
    private static readonly SKColor[] Paleta = [
        SKColor.Parse("#60B4FF"), SKColor.Parse("#40D080"), SKColor.Parse("#FFB040"),
        SKColor.Parse("#FF6070"), SKColor.Parse("#C080FF"), SKColor.Parse("#40D0D0"),
        SKColor.Parse("#FF80C0"), SKColor.Parse("#A0D860"), SKColor.Parse("#FFA060"),
        SKColor.Parse("#60D0C0"),
    ];

    // ─── TAB 1: VENTAS DEL DÍA ────────────────────────────────
    private async Task ConsultarVentasDia()
    {
        try
        {
            var fecha = DpFechaDia.SelectedDate ?? DateTime.Today;
            var r = await App.Api.GetVentasDia(fecha, IdSucursalFiltro);
            if (r is null) return;

            TxtTotalDia.Text       = r.Total.ToString("$ #,##0.00");
            TxtCantVentasDia.Text  = r.CantVentas.ToString();
            TxtTicketPromedio.Text = r.TicketPromedio.ToString("$ #,##0.00");
            TxtIvaDia.Text         = r.Iva.ToString("$ #,##0.00");
            TxtTendenciaDia.Text   = $"Al {fecha:dd/MM/yyyy}";

            // Pie chart medios de pago
            if (r.PagosPorMedio?.Count > 0)
            {
                var series = r.PagosPorMedio
                    .Select((p, i) => new PieSeries<double>
                    {
                        Name    = p.MedioPago,
                        Values  = [(double)p.Total],
                        Fill    = new SolidColorPaint(Paleta[i % Paleta.Length]),
                        Stroke  = null,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsFormatter = pt => $"{pt.Coordinate.PrimaryValue:N0}",
                        DataLabelsSize = 13,
                    })
                    .ToArray<ISeries>();

                ChartMediosPago.Series = series;
                ChartMediosPago.LegendTextPaint = new SolidColorPaint(SKColors.LightGray);
            }

            // Lista detalle medios de pago
            IcMediosPago.ItemsSource = r.PagosPorMedio;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    // ─── TAB 2: POR PERÍODO ───────────────────────────────────
    private async Task ConsultarPeriodo()
    {
        try
        {
            var desde   = DpDesde.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var hasta   = DpHasta.SelectedDate ?? DateTime.Today;
            var agrupar = CboAgrupar.SelectedIndex == 1 ? "mes" : "dia";
            var r       = await App.Api.GetVentasPeriodo(desde, hasta, agrupar, IdSucursalFiltro);
            if (r is null) return;

            TxtTotalPeriodo.Text = r.TotalPeriodo.ToString("$ #,##0.00");
            TxtCantPeriodo.Text  = r.CantTotal.ToString("N0") + " ventas";
            DgPeriodo.ItemsSource = r.Detalle;

            if (r.Detalle?.Count > 0)
            {
                var labels  = r.Detalle.Select(d => d.Periodo).ToArray();
                var valores = r.Detalle.Select(d => (double)d.Total).ToArray();
                var maxVal  = valores.Max();

                ChartPeriodo.Series = [
                    new ColumnSeries<double>
                    {
                        Name   = "Total ventas",
                        Values = valores,
                        Fill   = new LinearGradientPaint(
                            new[] { SKColor.Parse("#1040A0"), SKColor.Parse("#60B4FF") },
                            new SKPoint(0, 1), new SKPoint(0, 0)),
                        Stroke = null,
                        MaxBarWidth = 28,
                        Rx = 4, Ry = 4,
                    }
                ];

                ChartPeriodo.XAxes = [
                    new Axis
                    {
                        Labels = labels,
                        LabelsRotation = agrupar == "dia" ? -45 : 0,
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#7090B0")),
                        SeparatorsPaint = null,
                        TicksPaint = null,
                    }
                ];

                ChartPeriodo.YAxes = [
                    new Axis
                    {
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#7090B0")),
                        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#252545")) { StrokeThickness = 1 },
                        Labeler = v => $"$ {v / 1000:N0}k",
                    }
                ];
            }
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    // ─── TAB 3: RANKING ───────────────────────────────────────
    private async Task ConsultarRanking()
    {
        try
        {
            var desde = DpRankingDesde.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var hasta = DpRankingHasta.SelectedDate ?? DateTime.Today;
            var top   = (int)(NumTop.Value ?? 10);
            var items = await App.Api.GetRankingProductos(desde, hasta, top, IdSucursalFiltro);
            if (items is null || items.Count == 0) return;

            // Horizontal bar chart (invertir: XAxes=valores, YAxes=labels)
            var labels   = items.Select(x => ShortLabel(x.Descripcion)).ToArray();
            var valores  = items.Select(x => (double)x.TotalVendido).ToArray();

            ChartRanking.Series = [
                new RowSeries<double>
                {
                    Name   = "Total vendido",
                    Values = valores,
                    Fill   = new LinearGradientPaint(
                        new[] { SKColor.Parse("#206040"), SKColor.Parse("#40D080") },
                        new SKPoint(0, 0), new SKPoint(1, 0)),
                    Stroke = null,
                    MaxBarWidth = 22,
                    Rx = 4, Ry = 4,
                }
            ];

            ChartRanking.YAxes = [
                new Axis
                {
                    Labels   = labels,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#C0D0FF")),
                    SeparatorsPaint = null,
                    TicksPaint = null,
                }
            ];

            ChartRanking.XAxes = [
                new Axis
                {
                    TextSize = 10,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#7090B0")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#252545")) { StrokeThickness = 1 },
                    Labeler = v => $"$ {v / 1000:N0}k",
                }
            ];

            // Lista lateral con posición
            IcRanking.ItemsSource = items
                .Select((x, i) => new RankingItemVm
                {
                    Posicion     = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"#{i + 1}",
                    Descripcion  = x.Descripcion,
                    CantVendida  = x.CantVendida,
                    TotalVendido = x.TotalVendido,
                })
                .ToList();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    // ─── TAB 4: STOCK BAJO MÍNIMO ─────────────────────────────
    private async Task ConsultarStockBajo()
    {
        try
        {
            var r = await App.Api.GetStockBajoMinimo();
            if (r is null) return;

            TxtTotalCriticos.Text = r.Total.ToString();
            DgStockBajo.ItemsSource = r.Articulos;

            var costoTotal = r.Articulos.Sum(a => a.PrecioCosto * a.UnidadesAReponer);
            TxtCostoReposicion.Text = costoTotal.ToString("$ #,##0");

            // Obtener total de artículos activos (estimado)
            TxtStockNormal.Text = "OK";

            // Gráfico barras apiladas: stock actual vs mínimo (primeros 12)
            var muestra = r.Articulos.Take(12).ToList();
            if (muestra.Count > 0)
            {
                var labels   = muestra.Select(a => ShortLabel(a.Descripcion)).ToArray();
                var actuales = muestra.Select(a => (double)a.StockActual).ToArray();
                var minimos  = muestra.Select(a => (double)a.StockMinimo).ToArray();

                ChartStock.Series = [
                    new ColumnSeries<double>
                    {
                        Name   = "Stock Actual",
                        Values = actuales,
                        Fill   = new SolidColorPaint(SKColor.Parse("#FF5050")),
                        Stroke = null,
                        MaxBarWidth = 20,
                        Rx = 3, Ry = 3,
                    },
                    new ColumnSeries<double>
                    {
                        Name   = "Stock Mínimo",
                        Values = minimos,
                        Fill   = new SolidColorPaint(SKColor.Parse("#FFB04060")),
                        Stroke = new SolidColorPaint(SKColor.Parse("#FFB040")) { StrokeThickness = 1 },
                        MaxBarWidth = 20,
                        Rx = 3, Ry = 3,
                    },
                ];

                ChartStock.XAxes = [
                    new Axis
                    {
                        Labels = labels,
                        LabelsRotation = -40,
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#9090B0")),
                        SeparatorsPaint = null,
                        TicksPaint = null,
                    }
                ];

                ChartStock.YAxes = [
                    new Axis
                    {
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#7090B0")),
                        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#252545")) { StrokeThickness = 1 },
                    }
                ];

                ChartStock.LegendPosition = LiveChartsCore.Measure.LegendPosition.Top;
                ChartStock.LegendTextPaint = new SolidColorPaint(SKColor.Parse("#C0D0FF"));
            }
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    // ─── TAB HISTORIAL DE VENTAS ──────────────────────────────
    private async Task ConsultarHistorial()
    {
        try
        {
            var fecha = DpFechaHistorial.SelectedDate ?? DateTime.Today;
            var res = await App.Api.GetVentas(desde: fecha, hasta: fecha, page: 1, pageSize: 200, idSucursal: IdSucursalFiltro);
            DgHistorialVentas.ItemsSource = res.items;
            LimpiarDetalle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar el historial: {ex.Message}");
        }
    }

    private async void DgHistorialVentas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgHistorialVentas.SelectedItem is not Comprobante c)
        {
            LimpiarDetalle();
            return;
        }

        try
        {
            var cbte = await App.Api.GetVentaById(c.Id);
            if (cbte is null) return;

            TxtNoSeleccionado.Visibility = Visibility.Collapsed;
            PanelDetalleContenido.Visibility = Visibility.Visible;
            DgDetallesVenta.Visibility = Visibility.Visible;
            PanelDetalleTotales.Visibility = Visibility.Visible;

            TxtDetalleTitulo.Text = $"Detalle de Venta {cbte.TipoComprobante?.Abreviatura} {cbte.Letra} {cbte.PuntoVenta:D3}-{cbte.Numero:D8}";
            TxtDetalleUsuario.Text = $"Cajero ID: {cbte.IdUsuario}";
            TxtDetalleFecha.Text = $"Fecha: {cbte.Fecha.ToLocalTime():dd/MM/yyyy HH:mm}";

            TxtDetalleEstado.Text = cbte.Estado.ToString();
            if (cbte.Estado == EstadoComprobante.Anulado)
            {
                BorderDetalleEstado.Background = System.Windows.Media.Brushes.DarkRed;
                TxtDetalleEstado.Foreground = System.Windows.Media.Brushes.White;
                PanelDetalleAcciones.Visibility = Visibility.Collapsed;
            }
            else
            {
                BorderDetalleEstado.Background = System.Windows.Media.Brushes.DarkGreen;
                TxtDetalleEstado.Foreground = System.Windows.Media.Brushes.White;
                PanelDetalleAcciones.Visibility = (App.PerfilActual.PuedeAnularVentas || App.PerfilActual.EsAdministrador) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            DgDetallesVenta.ItemsSource = cbte.Detalles;

            TxtDetalleSubtotal.Text = cbte.SubTotal.ToString("$ #,##0.00");
            TxtDetalleDescuento.Text = cbte.TotalDescuento.ToString("$ #,##0.00");
            TxtDetalleTotal.Text = cbte.Total.ToString("$ #,##0.00");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar el detalle de venta: {ex.Message}");
        }
    }

    private void LimpiarDetalle()
    {
        TxtNoSeleccionado.Visibility = Visibility.Visible;
        PanelDetalleContenido.Visibility = Visibility.Collapsed;
        DgDetallesVenta.Visibility = Visibility.Collapsed;
        PanelDetalleTotales.Visibility = Visibility.Collapsed;
        PanelDetalleAcciones.Visibility = Visibility.Collapsed;
        DgDetallesVenta.ItemsSource = null;
    }

    private async void BtnAnularVenta_Click(object sender, RoutedEventArgs e)
    {
        if (DgHistorialVentas.SelectedItem is not Comprobante c) return;

        if (MessageBox.Show($"¿Está seguro de que desea ANULAR la venta seleccionada?\nEsta acción devolverá la mercadería al stock.", 
            "Confirmar Anulación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await App.Api.AnularVenta(c.Id, App.IdUsuarioActual);
            MessageBox.Show("Venta anulada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            await ConsultarHistorial();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al anular la venta: {ex.Message}");
        }
    }

    // ─── TAB CALENDARIO DE PAGOS ──────────────────────────────
    private async Task CargarProveedoresCalendario()
    {
        try
        {
            var proveedores = await App.Api.GetProveedoresLista() ?? [];
            var conTodos = new List<ProveedorSimple> { new() { Id = 0, RazonSocial = "(Todos los proveedores)" } };
            conTodos.AddRange(proveedores);
            CboProveedorCalendario.ItemsSource = conTodos;
            CboProveedorCalendario.SelectedIndex = 0;
        }
        catch (Exception ex) { MessageBox.Show($"Error al cargar proveedores: {ex.Message}"); }
    }

    private async Task ConsultarCalendarioPagos()
    {
        try
        {
            var idProveedor = CboProveedorCalendario.SelectedValue is int id && id > 0 ? id : (int?)null;
            var items = await App.Api.GetCalendarioPagos(idProveedor) ?? [];
            DgCalendarioPagos.ItemsSource = items;

            TxtTotalPendientePago.Text = items.Sum(i => i.Total).ToString("$ #,##0.00");
            TxtCantPendientes.Text = items.Count.ToString();
            TxtCantVencidas.Text = items.Count(i => i.Vencida).ToString();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void BtnMarcarPagada_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not CalendarioPagoDto c) return;

        if (MessageBox.Show($"¿Confirmás el pago de {c.Total:$ #,##0.00} a {c.Proveedor}?",
            "Marcar como pagada", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            await App.Api.RegistrarPagoCompra(c.IdProveedor, c.Total,
                $"Pago factura {c.LetraFactura}{c.NumeroFactura}", App.IdUsuarioActual, c.Id);
            await ConsultarCalendarioPagos();
        }
        catch (Exception ex) { MessageBox.Show($"Error al registrar el pago: {ex.Message}"); }
    }

    // ─── TAB RENTABILIDAD POR PROVEEDOR ───────────────────────
    private async Task ConsultarRentabilidadProveedores()
    {
        try
        {
            var desde = DpRentabilidadDesde.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var hasta = DpRentabilidadHasta.SelectedDate ?? DateTime.Today;
            var r = await App.Api.GetRentabilidadProveedores(desde, hasta);
            DgRentabilidadProveedores.ItemsSource = r?.Proveedores ?? [];
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    // ─── Helpers ──────────────────────────────────────────────
    private static string ShortLabel(string s) =>
        s.Length > 20 ? s[..20] + "…" : s;
}

// VM para ranking lateral
public class RankingItemVm
{
    public string Posicion     { get; set; } = "";
    public string Descripcion  { get; set; } = "";
    public decimal CantVendida  { get; set; }
    public decimal TotalVendido { get; set; }
}
