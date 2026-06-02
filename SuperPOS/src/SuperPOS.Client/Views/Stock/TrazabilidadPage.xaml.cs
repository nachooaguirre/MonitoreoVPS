using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock
{
    public partial class TrazabilidadPage : Page
    {
        public TrazabilidadPage()
        {
            InitializeComponent();
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await BuscarArticuloAsync();
        }

        private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await BuscarArticuloAsync();
            }
        }

        private async Task BuscarArticuloAsync()
        {
            var texto = TxtBuscar.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("Ingresá un término de búsqueda (EAN o descripción).", "Búsqueda", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Buscar artículos (permite buscar por EAN o texto)
                var (total, items) = await App.Api.GetArticulos(buscar: texto, pageSize: 50);

                if (total == 0)
                {
                    MessageBox.Show("No se encontró ningún artículo que coincida con la búsqueda.", "Búsqueda", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (total == 1)
                {
                    // Un solo artículo encontrado: cargar directamente
                    var art = items.First();
                    await CargarArticuloSeleccionadoAsync(art);
                }
                else
                {
                    // Múltiples artículos: mostrar lista de selección
                    PanelDetalleArticulo.Visibility = Visibility.Collapsed;
                    PanelMensajeInicio.Visibility = Visibility.Collapsed;
                    ScrollTimeline.Visibility = Visibility.Collapsed;
                    TxtNoEventos.Visibility = Visibility.Visible;

                    PanelArticulos.Visibility = Visibility.Visible;
                    LstArticulos.ItemsSource = items;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LstArticulos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstArticulos.SelectedItem is Articulo art)
            {
                await CargarArticuloSeleccionadoAsync(art);
            }
        }

        private async Task CargarArticuloSeleccionadoAsync(Articulo art)
        {
            PanelArticulos.Visibility = Visibility.Collapsed;
            PanelMensajeInicio.Visibility = Visibility.Collapsed;

            // Cargar datos en la tarjeta lateral
            TxtArtDescripcion.Text = art.Descripcion;
            TxtArtCodigo.Text = $"EAN: {art.CodigoBarras}  ·  Int: {art.CodigoInterno}";
            TxtStockActual.Text = art.StockActual.ToString("N2");
            TxtStockDeposito.Text = art.StockDeposito.ToString("N2");
            TxtPrecioVenta.Text = art.PrecioVenta.ToString("C2");
            TxtPrecioCosto.Text = art.PrecioCosto.ToString("C2");
            TxtStockMinimo.Text = art.StockMinimo.ToString("N2");

            PanelDetalleArticulo.Visibility = Visibility.Visible;

            // Consultar eventos de trazabilidad
            try
            {
                var eventosJson = await App.Api.GetTrazabilidadPorArticulo(art.Id, take: 100);
                if (eventosJson == null || eventosJson.Count == 0)
                {
                    ScrollTimeline.Visibility = Visibility.Collapsed;
                    TxtNoEventos.Visibility = Visibility.Visible;
                    return;
                }

                var timelineItems = eventosJson.Select(el => {
                    var cantidad = el.TryGetProperty("cantidad", out var c) ? c.GetDecimal() : 0m;
                    var tipo = el.TryGetProperty("tipo", out var t) ? t.GetInt32() : 0;
                    var fecha = el.TryGetProperty("fecha", out var f) ? f.GetDateTime() : DateTime.UtcNow;
                    var ubicacion = el.TryGetProperty("ubicacion", out var u) ? u.GetString() : "Casa Central";
                    var idUsuario = el.TryGetProperty("idUsuario", out var usr) && usr.ValueKind == JsonValueKind.Number ? usr.GetInt32() : (int?)null;
                    var observaciones = el.TryGetProperty("observaciones", out var obs) ? obs.GetString() : null;

                    return new TimelineItemViewModel
                    {
                        Tipo = tipo,
                        TipoTexto = GetTipoTexto(tipo),
                        FechaFormatted = fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                        Cantidad = Math.Abs(cantidad),
                        EsPositivo = cantidad >= 0,
                        UbicacionVal = string.IsNullOrWhiteSpace(ubicacion) ? "Casa Central" : ubicacion,
                        OperadorVal = idUsuario.HasValue ? $"Usuario ID {idUsuario.Value}" : "Sistema",
                        Observaciones = observaciones,
                        ObservacionesVis = string.IsNullOrWhiteSpace(observaciones) ? Visibility.Collapsed : Visibility.Visible
                    };
                }).ToList();

                ItemsTimeline.ItemsSource = timelineItems;
                TxtNoEventos.Visibility = Visibility.Collapsed;
                ScrollTimeline.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la trazabilidad: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetTipoTexto(int tipo)
        {
            return tipo switch
            {
                0 => "📥 Recepción en Depósito",
                1 => "📤 Reposición a Sala",
                2 => "⚠️ Ajuste de Stock",
                3 => "🛒 Venta en Caja",
                4 => "🔄 Anulación de Venta",
                5 => "⚠️ Merma de Mercadería",
                _ => "📝 Movimiento de Stock"
            };
        }
    }

    public class TimelineItemViewModel
    {
        public int Tipo { get; set; }
        public string TipoTexto { get; set; } = string.Empty;
        public string FechaFormatted { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public bool EsPositivo { get; set; }
        public string UbicacionVal { get; set; } = "Casa Central";
        public string OperadorVal { get; set; } = "Sistema";
        public string? Observaciones { get; set; }
        public Visibility ObservacionesVis { get; set; } = Visibility.Collapsed;
    }
}
