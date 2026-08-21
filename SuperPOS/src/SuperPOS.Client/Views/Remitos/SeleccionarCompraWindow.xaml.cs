using System.Windows;
using SuperPOS.Shared.Entities.Ventas;
using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views.Remitos;

public partial class SeleccionarCompraWindow : FluentWindow
{
    public long? IdCompraSeleccionada { get; private set; }

    public SeleccionarCompraWindow(int idProveedor)
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarAsync(idProveedor);
    }

    private async Task CargarAsync(int idProveedor)
    {
        try
        {
            var compras = await App.Api.GetCompras(idProveedor);
            CboCompra.ItemsSource = compras.Select(c => new
            {
                c.Id,
                Display = $"{c.LetraFactura}{c.NumeroFactura} · {c.Fecha:dd/MM/yyyy} · {c.Total:C2}"
            }).ToList();
            if (CboCompra.Items.Count > 0) CboCompra.SelectedIndex = 0;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        dynamic? sel = CboCompra.SelectedItem;
        if (sel is null) { System.Windows.MessageBox.Show("Seleccioná una factura."); return; }
        IdCompraSeleccionada = (long)sel.Id;
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
