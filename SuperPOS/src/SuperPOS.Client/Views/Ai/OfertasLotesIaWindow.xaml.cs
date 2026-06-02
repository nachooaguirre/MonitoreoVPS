using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Ai;

public partial class OfertasLotesIaWindow : Window
{
    private readonly ObservableCollection<OfertaLoteFila> _rows = new();

    public OfertasLotesIaWindow(IEnumerable<AiAlertaVencimientoDto> alertas)
    {
        InitializeComponent();
        Dg.ItemsSource = _rows;
        Loaded += async (_, _) => await CargarAsync(alertas);
    }

    private async Task CargarAsync(IEnumerable<AiAlertaVencimientoDto> alertas)
    {
        foreach (var a in alertas)
        {
            try
            {
                var art = await App.Api.GetArticulo(a.IdArticulo);
                if (art is null) continue;
                var pv = art.PrecioVenta;
                var suger = Math.Round(pv * 0.9m, 2, MidpointRounding.AwayFromZero);
                _rows.Add(new OfertaLoteFila
                {
                    IdArticulo = a.IdArticulo,
                    Descripcion = a.Descripcion,
                    LoteNro = a.LoteNro,
                    FechaVenceStr = a.FechaVencimiento.ToString("dd/MM/yyyy"),
                    PrecioVenta = pv,
                    NuevoPrecioOferta = art.PrecioOferta > 0 && art.PrecioOferta < pv ? art.PrecioOferta : suger
                });
            }
            catch
            {
                // ítem con error, omitir
            }
        }
        if (_rows.Count == 0)
            MessageBox.Show("No se pudo cargar artículos para ofertar.", "Ofertas", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnSugerir10_Click(object sender, RoutedEventArgs e)
    {
        foreach (var r in _rows)
            r.NuevoPrecioOferta = Math.Round(r.PrecioVenta * 0.9m, 2, MidpointRounding.AwayFromZero);
        Dg.Items.Refresh();
    }

    private async void BtnAplicar_Click(object sender, RoutedEventArgs e)
    {
        var ok = 0;
        var err = 0;
        foreach (var r in _rows)
        {
            try
            {
                var art = await App.Api.GetArticulo(r.IdArticulo);
                if (art is null) { err++; continue; }
                if (r.NuevoPrecioOferta < 0) { err++; continue; }
                art.PrecioOferta = r.NuevoPrecioOferta;
                art.FechaModificacion = DateTime.UtcNow;
                await App.Api.ActualizarArticulo(art);
                ok++;
            }
            catch
            {
                err++;
            }
        }
        MessageBox.Show(
            ok > 0
                ? $"Actualizados {ok} artículos (precio de oferta).{(err > 0 ? $" {err} con error." : "")}"
                : "No se pudo actualizar ningún artículo.",
            "Ofertas",
            MessageBoxButton.OK,
            err > 0 && ok == 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        if (ok > 0) DialogResult = true;
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public class OfertaLoteFila
    {
        public int IdArticulo { get; set; }
        public string Descripcion { get; set; } = "";
        public string? LoteNro { get; set; }
        public string FechaVenceStr { get; set; } = "";
        public decimal PrecioVenta { get; set; }
        public decimal NuevoPrecioOferta { get; set; }
    }
}
