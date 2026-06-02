using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.OrdenesCompra;

public partial class OrdenesCompraPage : Page
{
    public OrdenesCompraPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarOrdenes();
    }

    private async Task CargarOrdenes()
    {
        try
        {
            int? filtro = null;
            if (CboEstado.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out var n))
                filtro = n;
            var ordenes = await App.Api.GetOrdenesCompra(filtro);
            DgOrdenes.ItemsSource = ordenes;
            TxtTotalOC.Text = ordenes is null ? "" : $"Total: {ordenes.Count} órdenes";
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void CboEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => await CargarOrdenes();

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await CargarOrdenes();

    private async void BtnSugerida_Click(object sender, RoutedEventArgs e)
    {
        // Buscar proveedor con más artículos bajo mínimo
        var dlg = new SugerenciaOCWindow();
        dlg.ShowDialog();
        if (dlg.SeCreo) await CargarOrdenes();
    }

    private void BtnNuevaOC_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevaOCWindow();
        if (dlg.ShowDialog() == true) _ = CargarOrdenes();
    }

    private void DgOrdenes_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgOrdenes.SelectedItem is not OrdenCompraResumenDto oc) return;
        var dlg = new DetalleOCWindow(oc.Id);
        dlg.ShowDialog();
        _ = CargarOrdenes();
    }
}
