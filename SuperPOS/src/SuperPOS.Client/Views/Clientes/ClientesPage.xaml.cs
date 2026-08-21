using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Clientes;

public partial class ClientesPage : Page
{
    private int _page = 1;
    private const int PageSize = 100;
    private int _total;

    public ClientesPage() { InitializeComponent(); Loaded += OnLoaded; }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await CargarDatos();

    private async Task CargarDatos()
    {
        DgClientes.ItemsSource = null;
        var (total, items) = await App.Api.GetClientes(
            string.IsNullOrWhiteSpace(TxtBuscar.Text) ? null : TxtBuscar.Text.Trim(), _page, PageSize);
        _total = total;
        DgClientes.ItemsSource = items;
        TxtTotal.Text = $"{total} cliente(s)";
        TxtPagina.Text = $"Página {_page} de {Math.Max(1, (int)Math.Ceiling((double)total / PageSize))}";
        BtnPrev.IsEnabled = _page > 1;
        BtnNext.IsEnabled = _page * PageSize < total;
    }

    private async void Filtro_Changed(object sender, RoutedEventArgs e) { _page = 1; await CargarDatos(); }
    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { _page = 1; await CargarDatos(); } }
    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) { _page = 1; await CargarDatos(); }

    private void BtnNuevo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ClienteEditWindow(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) _ = CargarDatos();
    }

    private void DgClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    { if (DgClientes.SelectedItem is Cliente c) AbrirEdicion(c); }

    private void BtnEditar_Click(object sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Cliente c) AbrirEdicion(c); }

    private void AbrirEdicion(Cliente c)
    {
        var dlg = new ClienteEditWindow(c) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) _ = CargarDatos();
    }

    private void BtnNotaCliente_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not Cliente c) return;
        var dlg = new NotaClienteWindow(c) { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not Cliente c) return;
        if (c.Id == 1) { MessageBox.Show("No se puede eliminar el Consumidor Final.", "Error"); return; }
        if (MessageBox.Show($"¿Eliminar \"{c.RazonSocial}\"?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await App.Api.EliminarCliente(c.Id); await CargarDatos(); }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e) { _page--; await CargarDatos(); }
    private async void BtnNext_Click(object sender, RoutedEventArgs e) { _page++; await CargarDatos(); }
}
