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
        DgSucursales.ItemsSource = await App.Api.GetSucursales();
    }

    private async void DgSucursales_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _sucursalSeleccionada = DgSucursales.SelectedItem as SucursalAdminDto;
        BtnNuevaCaja.IsEnabled = _sucursalSeleccionada is not null;
        TxtCajasTitulo.Text = _sucursalSeleccionada is null
            ? "Puntos de venta"
            : $"Puntos de venta · {_sucursalSeleccionada.Nombre}";

        DgCajas.ItemsSource = _sucursalSeleccionada is null
            ? null
            : await App.Api.GetCajas(_sucursalSeleccionada.Id);
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
