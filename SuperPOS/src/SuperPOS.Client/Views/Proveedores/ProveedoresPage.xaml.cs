using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Proveedores;

public partial class ProveedoresPage : Page
{
    private int _page = 1;
    private const int PageSize = 100;

    public ProveedoresPage() { InitializeComponent(); Loaded += OnLoaded; }
    private async void OnLoaded(object sender, RoutedEventArgs e) => await CargarDatos();

    private async Task CargarDatos()
    {
        DgProveedores.ItemsSource = null;
        var (total, items) = await App.Api.GetProveedores(
            string.IsNullOrWhiteSpace(TxtBuscar.Text) ? null : TxtBuscar.Text.Trim(), _page, PageSize);
        DgProveedores.ItemsSource = items;
        TxtTotal.Text = $"{total} proveedor(es)";
        TxtPagina.Text = $"Página {_page} de {Math.Max(1, (int)Math.Ceiling((double)total / PageSize))}";
        BtnPrev.IsEnabled = _page > 1;
        BtnNext.IsEnabled = _page * PageSize < total;
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { _page = 1; await CargarDatos(); } }
    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) { _page = 1; await CargarDatos(); }

    private void BtnNuevo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ProveedorEditWindow(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) _ = CargarDatos();
    }

    private void DgProveedores_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    { if (DgProveedores.SelectedItem is Proveedor p) AbrirEdicion(p); }

    private void BtnEditar_Click(object sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Proveedor p) AbrirEdicion(p); }

    private void AbrirEdicion(Proveedor p)
    {
        var dlg = new ProveedorEditWindow(p) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) _ = CargarDatos();
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e) { _page--; await CargarDatos(); }
    private async void BtnNext_Click(object sender, RoutedEventArgs e) { _page++; await CargarDatos(); }
}
