using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Remitos;

public partial class RemitosZebraPage : Page
{
    public RemitosZebraPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarRemitos();
    }

    private async Task CargarRemitos()
    {
        try
        {
            // Estado 0 = Pendiente (ver EstadoRemito): solo lo que todavía no se confirmó en caja.
            var remitos = await App.Api.GetRemitos(estado: 0, soloZebra: true);
            DgRemitos.ItemsSource = remitos;
            TxtTotal.Text = $"{remitos.Count} pendiente(s)";
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await CargarRemitos();

    private void DgRemitos_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgRemitos.SelectedItem is not RemitoListItemDto r) return;
        var dlg = new DetalleRemitoWindow(r.Id);
        dlg.ShowDialog();
        _ = CargarRemitos();
    }
}
