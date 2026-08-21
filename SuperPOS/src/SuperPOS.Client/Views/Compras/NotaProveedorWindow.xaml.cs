using System.Windows;
using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views.Compras;

public partial class NotaProveedorWindow : FluentWindow
{
    private readonly int _idProveedor;
    private readonly long? _idCompra;

    public NotaProveedorWindow(int idProveedor, string nombreProveedor, long? idCompra = null, decimal? montoSugerido = null, bool esDebitoSugerido = false)
    {
        InitializeComponent();
        _idProveedor = idProveedor;
        _idCompra = idCompra;
        TxtProveedor.Text = nombreProveedor;
        if (montoSugerido is > 0) NumMonto.Value = (double)montoSugerido.Value;
        CboTipo.SelectedIndex = esDebitoSugerido ? 1 : 0;
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (NumMonto.Value is null or <= 0) { System.Windows.MessageBox.Show("Ingresá un monto válido"); return; }
        if (string.IsNullOrWhiteSpace(TxtConcepto.Text)) { System.Windows.MessageBox.Show("Ingresá el concepto"); return; }
        try
        {
            var esDebito = CboTipo.SelectedIndex == 1;
            await App.Api.AjusteManualCtaCteProveedor(_idProveedor, (decimal)NumMonto.Value, esDebito,
                TxtConcepto.Text.Trim(), App.IdUsuarioActual, _idCompra);
            DialogResult = true;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
