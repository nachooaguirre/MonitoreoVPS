using System.Windows;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Caja;

public partial class BuscadorClientesWindow : Wpf.Ui.Controls.FluentWindow
{
    public Cliente? ClienteSeleccionado { get; private set; }

    public BuscadorClientesWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => { TxtBuscar.Focus(); await Buscar(); };
    }

    private async Task Buscar()
    {
        var (_, items) = await App.Api.GetClientes(TxtBuscar.Text.Trim(), pageSize: 50);
        DgClientes.ItemsSource = items;
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) await Buscar(); }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) => await Buscar();

    private void DgClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgClientes.SelectedItem is Cliente c) { ClienteSeleccionado = c; DialogResult = true; }
    }

    private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
    {
        if (DgClientes.SelectedItem is Cliente c) { ClienteSeleccionado = c; DialogResult = true; }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
}
