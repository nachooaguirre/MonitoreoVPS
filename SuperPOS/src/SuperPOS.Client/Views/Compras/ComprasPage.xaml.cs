using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Compras;

public partial class ComprasPage : Page
{
    public ComprasPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarCompras();
    }

    private async Task CargarCompras()
    {
        try
        {
            int? estado = CboEstado.SelectedIndex switch
            {
                1 => (int)EstadoCompra.Pendiente,
                2 => (int)EstadoCompra.Recibida,
                3 => (int)EstadoCompra.Anulada,
                _ => null
            };
            var compras = await App.Api.GetCompras(estado: estado);
            DgCompras.ItemsSource = compras;
            TxtTotal.Text = $"{compras.Count} factura(s)";
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void Filtrar_Changed(object sender, SelectionChangedEventArgs e)
        => await CargarCompras();

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await CargarCompras();

    private async void BtnNuevaFactura_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevaCompraWindow();
        if (dlg.ShowDialog() == true) await CargarCompras();
    }

    private void DgCompras_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgCompras.SelectedItem is not Compra c) return;
        MessageBox.Show(
            $"Factura {c.LetraFactura}-{c.NumeroFactura}\nProveedor: {c.Proveedor?.RazonSocial}\nTotal: {c.Total:C2}\nArtículos: {c.Detalles.Count}",
            "Detalle de factura");
    }

    private async void VerArchivo_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Compra c) return;
        if (string.IsNullOrEmpty(c.ArchivoFacturaNombre)) return;
        try
        {
            var descarga = await App.Api.DescargarFacturaArchivo(c.Id);
            if (descarga is null) { MessageBox.Show("No se pudo descargar el archivo."); return; }

            var tmp = Path.Combine(Path.GetTempPath(), "SuperPOS_Facturas");
            Directory.CreateDirectory(tmp);
            var destino = Path.Combine(tmp, $"{c.Id}_{descarga.Value.FileName}");
            await File.WriteAllBytesAsync(destino, descarga.Value.Bytes);
            Process.Start(new ProcessStartInfo(destino) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show($"No se pudo abrir el archivo: {ex.Message}"); }
    }
}
