using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Sucursales;

public partial class SucursalesPage : Page
{
    private SucursalAdminDto? _sucursalSeleccionada;

    public SucursalesPage() { InitializeComponent(); Loaded += OnLoaded; }
    private async void OnLoaded(object sender, RoutedEventArgs e) => await CargarSucursales();

    private async Task CargarSucursales()
    {
        try
        {
            DgSucursales.ItemsSource = await App.Api.GetSucursalesAdmin();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar las sucursales:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DgSucursales_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _sucursalSeleccionada = DgSucursales.SelectedItem as SucursalAdminDto;
        BtnNuevaCaja.IsEnabled = _sucursalSeleccionada is not null;
        TxtCajasTitulo.Text = _sucursalSeleccionada is null
            ? "Puntos de venta"
            : $"Puntos de venta · {_sucursalSeleccionada.Nombre}";

        if (_sucursalSeleccionada is null) { DgCajas.ItemsSource = null; return; }

        try { DgCajas.ItemsSource = await App.Api.GetCajas(_sucursalSeleccionada.Id); }
        catch (Exception ex) { MessageBox.Show($"No se pudieron cargar los puntos de venta:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void BtnNuevaSucursal_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SucursalEditWindow(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await CargarSucursales();
    }

    private async void BtnEditarSucursal_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not SucursalAdminDto s) return;
        var dlg = new SucursalEditWindow(s) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await CargarSucursales();
    }

    private async void BtnEliminarSucursal_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not SucursalAdminDto s) return;
        if (MessageBox.Show($"¿Eliminar la sucursal \"{s.Nombre}\"?\n\nSi tiene puntos de venta o stock cargado no se puede borrar del todo y va a quedar desactivada en su lugar.",
            "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            await App.Api.EliminarSucursal(s.Id);
            await CargarSucursales();
        }
        catch (Exception ex) { MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void BtnEliminarCaja_Click(object sender, RoutedEventArgs e)
    {
        if (_sucursalSeleccionada is null) return;
        if ((sender as Button)?.Tag is not SuperPOS.Shared.Entities.Ventas.Caja c) return;
        if (MessageBox.Show($"¿Eliminar el punto de venta \"{c.Nombre}\"?\n\nSi ya tiene ventas registradas no se puede borrar del todo y va a quedar desactivado en su lugar.",
            "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            await App.Api.EliminarCaja(c.Id);
            await CargarSucursales();
            DgCajas.ItemsSource = await App.Api.GetCajas(_sucursalSeleccionada.Id);
        }
        catch (Exception ex) { MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void BtnNuevaCaja_Click(object sender, RoutedEventArgs e)
    {
        if (_sucursalSeleccionada is null) return;
        var dlg = new CajaEditWindow(_sucursalSeleccionada.Id, null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await CargarSucursales();
            DgCajas.ItemsSource = await App.Api.GetCajas(_sucursalSeleccionada.Id);
        }
    }

    private async void BtnEditarCaja_Click(object sender, RoutedEventArgs e)
    {
        if (_sucursalSeleccionada is null) return;
        if ((sender as Button)?.Tag is not SuperPOS.Shared.Entities.Ventas.Caja c) return;
        var dlg = new CajaEditWindow(_sucursalSeleccionada.Id, c) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await CargarSucursales();
            DgCajas.ItemsSource = await App.Api.GetCajas(_sucursalSeleccionada.Id);
        }
    }
}
