using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.CtaCte;

public partial class CtaCtePage : Page
{
    private ClienteCtaCteDto? _clienteSeleccionado;
    private List<ClienteCtaCteDto>? _todosClientes;

    public CtaCtePage()
    {
        InitializeComponent();
        DpMovDesde.SelectedDate = DateTime.Today.AddMonths(-1);
        DpMovHasta.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CargarClientes();
    }

    private async Task CargarClientes()
    {
        try
        {
            var soloDeudores = ChkSoloDeudores.IsChecked == true;
            _todosClientes = await App.Api.GetClientesCtaCte(soloDeudores) ?? [];
            FiltrarClientes();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void FiltrarClientes()
    {
        if (_todosClientes is null) return;
        var buscar = TxtBuscarCliente.Text.Trim().ToLower();
        var filtrados = string.IsNullOrEmpty(buscar)
            ? _todosClientes
            : _todosClientes.Where(c => c.RazonSocial.ToLower().Contains(buscar)).ToList();
        LstClientes.ItemsSource = filtrados;
    }

    private void TxtBuscarCliente_TextChanged(object sender, TextChangedEventArgs e) => FiltrarClientes();
    private async void ChkSoloDeudores_Changed(object sender, RoutedEventArgs e) => await CargarClientes();

    private async void LstClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _clienteSeleccionado = LstClientes.SelectedItem as ClienteCtaCteDto;
        if (_clienteSeleccionado is null) return;

        TxtClienteNombre.Text = _clienteSeleccionado.RazonSocial;
        TxtClienteInfo.Text   = $"CUIT: {_clienteSeleccionado.Cuit}  |  Límite: {_clienteSeleccionado.LimiteCredito:C2}";
        TxtSaldoActual.Text   = _clienteSeleccionado.SaldoCtaCte.ToString("C2");

        BtnRegistrarPago.IsEnabled = true;
        BtnAjuste.IsEnabled = true;

        await CargarMovimientos();
    }

    private async void BtnFiltrarMov_Click(object sender, RoutedEventArgs e) => await CargarMovimientos();

    private async Task CargarMovimientos()
    {
        if (_clienteSeleccionado is null) return;
        try
        {
            var desde = DpMovDesde.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var hasta = DpMovHasta.SelectedDate ?? DateTime.Today;
            var r = await App.Api.GetMovimientosCtaCte(_clienteSeleccionado.Id, desde, hasta);
            DgMovimientos.ItemsSource = r?.Items;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void BtnRegistrarPago_Click(object sender, RoutedEventArgs e)
    {
        if (_clienteSeleccionado is null) return;
        var dlg = new PagoCtaCteWindow(_clienteSeleccionado) { Owner = System.Windows.Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await CargarClientes();
            await CargarMovimientos();
        }
    }

    private async void BtnAjuste_Click(object sender, RoutedEventArgs e)
    {
        if (_clienteSeleccionado is null) return;
        var dlg = new AjusteCtaCteWindow(_clienteSeleccionado) { Owner = System.Windows.Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            await CargarClientes();
            await CargarMovimientos();
        }
    }
}
