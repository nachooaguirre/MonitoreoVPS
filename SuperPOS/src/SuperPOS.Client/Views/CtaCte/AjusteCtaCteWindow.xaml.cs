using System.Windows;
using SuperPOS.Client.Models;
using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views.CtaCte;

public partial class AjusteCtaCteWindow : FluentWindow
{
    private readonly ClienteCtaCteDto _cliente;

    public AjusteCtaCteWindow(ClienteCtaCteDto cliente)
    {
        InitializeComponent();
        _cliente = cliente;
        TxtCliente.Text = cliente.RazonSocial;
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (NumMonto.Value is null or <= 0) { System.Windows.MessageBox.Show("Ingresá un monto válido"); return; }
        if (string.IsNullOrWhiteSpace(TxtConcepto.Text)) { System.Windows.MessageBox.Show("Ingresá el concepto"); return; }
        try
        {
            var esDebito = CboTipo.SelectedIndex == 0;
            await App.Api.AjusteManualCtaCte(_cliente.Id, (decimal)NumMonto.Value, esDebito,
                TxtConcepto.Text.Trim(), App.IdUsuarioActual);
            DialogResult = true;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
