using System.Windows;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Caja;

public partial class BuscadorArticulosWindow : Wpf.Ui.Controls.FluentWindow
{
    public Articulo? ArticuloSeleccionado { get; private set; }

    public BuscadorArticulosWindow(string buscarInicial = "")
    {
        InitializeComponent();
        TxtBuscar.Text = buscarInicial;
        Loaded += async (_, _) => { TxtBuscar.Focus(); await Buscar(); };
    }

    private async Task Buscar()
    {
        var (_, items) = await App.Api.GetArticulos(TxtBuscar.Text.Trim(), pageSize: 50);
        DgResultados.ItemsSource = items;
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) await Buscar(); }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) => await Buscar();

    private void DgResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgResultados.SelectedItem is Articulo a) { ArticuloSeleccionado = a; DialogResult = true; }
    }

    private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
    {
        if (DgResultados.SelectedItem is Articulo a) { ArticuloSeleccionado = a; DialogResult = true; }
        else MessageBox.Show("Seleccione un artículo de la lista.");
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
}
