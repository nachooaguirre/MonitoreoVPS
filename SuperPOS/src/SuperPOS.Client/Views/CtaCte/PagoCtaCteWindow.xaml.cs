using System.Windows;
using SuperPOS.Client.Models;
using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views.CtaCte;

public partial class PagoCtaCteWindow : FluentWindow
{
    private readonly ClienteCtaCteDto _cliente;

    public PagoCtaCteWindow(ClienteCtaCteDto cliente)
    {
        InitializeComponent();
        _cliente = cliente;
        TxtCliente.Text = cliente.RazonSocial;
        TxtSaldo.Text   = cliente.SaldoCtaCte.ToString("C2");
        NumMonto.Value  = (double)cliente.SaldoCtaCte;
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (NumMonto.Value is null or <= 0) { System.Windows.MessageBox.Show("Ingresá un monto válido"); return; }
        try
        {
            await App.Api.RegistrarPagoCtaCte(_cliente.Id, (decimal)NumMonto.Value,
                TxtConcepto.Text.Trim(), App.IdUsuarioActual);
            DialogResult = true;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
