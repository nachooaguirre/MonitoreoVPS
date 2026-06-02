using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SuperPOS.Client.Views.Remitos;

public partial class RemitosPage : Page
{
    public RemitosPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarRemitos();
    }

    private async Task CargarRemitos()
    {
        try
        {
            var remitos = await App.Api.GetRemitos();
            DgRemitos.ItemsSource = remitos;
            TxtTotal.Text = $"{remitos?.Count ?? 0} remitos";
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void Filtrar_Changed(object sender, SelectionChangedEventArgs e)
        => await CargarRemitos();

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await CargarRemitos();

    private void BtnRecibirOC_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RecibirPedidoWindow();
        dlg.ShowDialog();
        _ = CargarRemitos();
    }

    private void BtnNuevoRemito_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevoRemitoWindow();
        if (dlg.ShowDialog() == true) _ = CargarRemitos();
    }

    private void DgRemitos_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgRemitos.SelectedItem is not System.Text.Json.JsonElement r) return;
        int id = Convert.ToInt32(r.GetProperty("id").GetInt32());
        var dlg = new DetalleRemitoWindow(id);
        dlg.ShowDialog();
        _ = CargarRemitos();
    }
}
