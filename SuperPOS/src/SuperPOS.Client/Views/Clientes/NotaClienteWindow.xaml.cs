using System.Windows;
using System.Windows.Controls;
using SuperPOS.Shared.Entities.Ventas;
using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views.Clientes;

public partial class NotaClienteWindow : FluentWindow
{
    private readonly Cliente _cliente;

    public NotaClienteWindow(Cliente cliente)
    {
        InitializeComponent();
        _cliente = cliente;
        TxtCliente.Text = cliente.RazonSocial;
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (NumMonto.Value is null or <= 0) { System.Windows.MessageBox.Show("Ingresá un monto válido"); return; }
        if (string.IsNullOrWhiteSpace(TxtConcepto.Text)) { System.Windows.MessageBox.Show("Ingresá el motivo"); return; }
        if (CboTipo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag) return;

        var partes = tag.Split(',');
        var idTipoComprobante = int.Parse(partes[0]);
        var letra = partes[1][0];
        var total = (decimal)NumMonto.Value;

        // ponytail: asume siempre IVA 21% para el desglose fiscal; ajustar si se factura a alícuota distinta.
        var subTotal = Math.Round(total / 1.21m, 2);
        var iva21 = total - subTotal;

        var cbte = new Comprobante
        {
            IdTipoComprobante = idTipoComprobante,
            Letra = letra,
            PuntoVenta = 1,
            IdCliente = _cliente.Id,
            IdCaja = App.CajaId,
            IdSucursal = App.SucursalId,
            IdUsuario = App.IdUsuarioActual,
            SubTotal = subTotal,
            TotalIva21 = iva21,
            Total = total,
            Observaciones = TxtConcepto.Text.Trim()
        };

        try
        {
            var resultado = await App.Api.RegistrarNotaCliente(cbte);
            System.Windows.MessageBox.Show(resultado?.CAE is > 0
                ? $"Nota emitida. CAE: {resultado.CAE}"
                : "Nota registrada (sin CAE — revisar log AFIP si correspondía).");
            DialogResult = true;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
