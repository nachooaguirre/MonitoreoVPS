using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SuperPOS.Client.Views.Caja;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Ofertas;

public partial class NuevaOfertaWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Oferta? _original;
    private Articulo? _articuloSeleccionado;

    public NuevaOfertaWindow(Oferta? original)
    {
        InitializeComponent();
        _original = original;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_original is not null)
        {
            Title = "Editar Oferta Especial";
            
            // Cargar artículo
            try
            {
                if (_original.Articulo != null)
                {
                    _articuloSeleccionado = _original.Articulo;
                }
                else
                {
                    _articuloSeleccionado = await App.Api.GetArticulo(_original.IdArticulo);
                }
            }
            catch
            {
                MessageBox.Show("No se pudo cargar la información del artículo.", "Error");
            }

            if (_articuloSeleccionado != null)
            {
                TxtArticuloDesc.Text = _articuloSeleccionado.Descripcion;
                TxtPrecioOriginal.Text = $"Precio normal: {_articuloSeleccionado.PrecioVenta.ToString("$ #,##0.00")}";
                TxtPrecioOriginal.Visibility = Visibility.Visible;
            }

            TxtDetalle.Text = _original.Detalle;
            TxtPrecioOferta.Text = _original.PrecioOferta.ToString("N2");

            var localDesde = _original.FechaDesde.ToLocalTime();
            DpFechaDesde.SelectedDate = localDesde.Date;
            TxtHoraDesde.Text = localDesde.Hour.ToString("D2");
            TxtMinutoDesde.Text = localDesde.Minute.ToString("D2");

            var localHasta = _original.FechaHasta.ToLocalTime();
            DpFechaHasta.SelectedDate = localHasta.Date;
            TxtHoraHasta.Text = localHasta.Hour.ToString("D2");
            TxtMinutoHasta.Text = localHasta.Minute.ToString("D2");

            if (_original.LimiteStock.HasValue)
            {
                ChkConLimite.IsChecked = true;
                TxtLimiteStock.Text = _original.LimiteStock.Value.ToString("N1");
                PanelLimite.Visibility = Visibility.Visible;
            }

            ChkActiva.IsChecked = _original.Activa;
        }
        else
        {
            Title = "Nueva Oferta Especial";
            
            DpFechaDesde.SelectedDate = DateTime.Today;
            TxtHoraDesde.Text = "00";
            TxtMinutoDesde.Text = "00";

            DpFechaHasta.SelectedDate = DateTime.Today.AddDays(7);
            TxtHoraHasta.Text = "23";
            TxtMinutoHasta.Text = "59";
        }
    }

    private void BtnBuscarArticulo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new BuscadorArticulosWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.ArticuloSeleccionado is not null)
        {
            _articuloSeleccionado = dlg.ArticuloSeleccionado;
            TxtArticuloDesc.Text = _articuloSeleccionado.Descripcion;
            TxtPrecioOriginal.Text = $"Precio normal: {_articuloSeleccionado.PrecioVenta.ToString("$ #,##0.00")}";
            TxtPrecioOriginal.Visibility = Visibility.Visible;

            if (string.IsNullOrWhiteSpace(TxtDetalle.Text))
            {
                TxtDetalle.Text = $"Oferta {_articuloSeleccionado.Descripcion}";
            }
        }
    }

    private void ChkConLimite_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (PanelLimite != null)
        {
            PanelLimite.Visibility = ChkConLimite.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        var regex = new Regex("[^0-9,\\.]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (_articuloSeleccionado is null)
        {
            MessageBox.Show("Debe seleccionar un artículo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtDetalle.Text))
        {
            MessageBox.Show("Debe ingresar el detalle/nombre de la oferta.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPrecioOferta.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precio) || precio <= 0)
        {
            MessageBox.Show("Debe ingresar un precio de oferta válido y mayor a cero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DpFechaDesde.SelectedDate is null || DpFechaHasta.SelectedDate is null)
        {
            MessageBox.Show("Debe seleccionar fechas de inicio y fin válidas.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtHoraDesde.Text, out int hDesde) || hDesde < 0 || hDesde > 23 ||
            !int.TryParse(TxtMinutoDesde.Text, out int mDesde) || mDesde < 0 || mDesde > 59 ||
            !int.TryParse(TxtHoraHasta.Text, out int hHasta) || hHasta < 0 || hHasta > 23 ||
            !int.TryParse(TxtMinutoHasta.Text, out int mHasta) || mHasta < 0 || mHasta > 59)
        {
            MessageBox.Show("Horas o minutos de vigencia inválidos.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fechaDesde = DpFechaDesde.SelectedDate.Value.Date.AddHours(hDesde).AddMinutes(mDesde);
        var fechaHasta = DpFechaHasta.SelectedDate.Value.Date.AddHours(hHasta).AddMinutes(mHasta);

        if (fechaHasta <= fechaDesde)
        {
            MessageBox.Show("La fecha de fin debe ser posterior a la fecha de inicio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal? limite = null;
        if (ChkConLimite.IsChecked == true)
        {
            if (!decimal.TryParse(TxtLimiteStock.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lim) || lim <= 0)
            {
                MessageBox.Show("Debe ingresar un límite de stock válido y mayor a cero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            limite = lim;
        }

        var o = new Oferta
        {
            Id = _original?.Id ?? 0,
            IdArticulo = _articuloSeleccionado.Id,
            Detalle = TxtDetalle.Text.Trim(),
            FechaDesde = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Local).ToUniversalTime(),
            FechaHasta = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Local).ToUniversalTime(),
            PrecioOferta = precio,
            LimiteStock = limite,
            CantidadVendida = _original?.CantidadVendida ?? 0m,
            Activa = ChkActiva.IsChecked == true
        };

        try
        {
            if (_original is null)
            {
                await App.Api.CrearOferta(o);
            }
            else
            {
                await App.Api.ActualizarOferta(o);
            }
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar la oferta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
