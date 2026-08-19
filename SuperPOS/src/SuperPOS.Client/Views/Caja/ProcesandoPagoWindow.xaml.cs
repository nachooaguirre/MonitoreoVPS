using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Caja;

public partial class ProcesandoPagoWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly decimal _monto;
    private readonly bool _esCredito;
    private readonly CancellationTokenSource _cts = new();
    private TarjetaInfoDto? _tarjetaElegida;

    public bool TransaccionAprobada { get; private set; }
    public string TarjetaMarca { get; private set; } = "";
    public string TarjetaUltimosDigitos { get; private set; } = "";
    public string CodigoAutorizacion { get; private set; } = "";
    public string NumeroCupon { get; private set; } = "";
    public string MensajeError { get; private set; } = "";
    /// <summary>Recargo/descuento aplicado según la marca elegida (puede ser negativo).</summary>
    public decimal Recargo { get; private set; }
    /// <summary>Monto real cobrado en el terminal (monto original + recargo).</summary>
    public decimal MontoConRecargo { get; private set; }

    public ProcesandoPagoWindow(decimal monto, bool esCredito)
    {
        InitializeComponent();
        _monto = monto;
        _esCredito = esCredito;

        TxtMontoSeleccion.Text = $"Monto: $ {monto:N2}";
        TxtMonto.Text = $"Monto: $ {monto:N2}";

        Loaded += async (_, _) => await CargarTarjetasAsync();
    }

    private async Task CargarTarjetasAsync()
    {
        try
        {
            var tarjetas = await App.Api.GetTarjetasSoportadas();
            IcTarjetas.ItemsSource = tarjetas.Where(t => t.EsCredito == _esCredito).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo obtener la lista de tarjetas:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnTarjeta_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not TarjetaInfoDto tarjeta) return;

        _tarjetaElegida = tarjeta;
        Recargo = Math.Round(_monto * tarjeta.PorcentajeRecargo / 100, 2);
        MontoConRecargo = _monto + Recargo;

        TxtTitulo.Text = $"💳 {tarjeta.Nombre}";
        PanelSeleccion.Visibility = Visibility.Collapsed;
        PanelProcesando.Visibility = Visibility.Visible;
        TxtMonto.Text = Recargo == 0
            ? $"Monto: $ {_monto:N2}"
            : $"Monto: $ {MontoConRecargo:N2}  ({(Recargo > 0 ? "+" : "")}{tarjeta.PorcentajeRecargo:N2}% = $ {Recargo:N2})";
        TxtEstado.Text = $"Esperando tarjeta en el terminal ({tarjeta.Nombre})...";

        await IniciarCobroAsync();
    }

    private async Task IniciarCobroAsync()
    {
        try
        {
            await Task.Delay(500); // Pequeño delay de cortesía visual
            var client = new HttpClient { BaseAddress = new Uri(App.ApiBaseUrl) };

            var requestData = new
            {
                Monto = MontoConRecargo,
                EsCredito = _esCredito,
                TarjetaCodigo = _tarjetaElegida?.Codigo,
                TarjetaNombre = _tarjetaElegida?.Nombre
            };

            var response = await client.PostAsJsonAsync("api/pagos-integrados/posnet/iniciar", requestData, _cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"El servidor de pagos respondió con error: {errorText}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            bool exito = false;
            if (result.TryGetProperty("exito", out var ex))
            {
                exito = ex.GetBoolean();
            }

            if (exito)
            {
                TransaccionAprobada = true;
                TarjetaMarca = result.TryGetProperty("tarjetaMarca", out var tm) ? tm.GetString() ?? _tarjetaElegida?.Nombre ?? "VISA" : _tarjetaElegida?.Nombre ?? "VISA";
                TarjetaUltimosDigitos = result.TryGetProperty("tarjetaUltimosDigitos", out var ud) ? ud.GetString() ?? "0000" : "0000";
                CodigoAutorizacion = result.TryGetProperty("codigoAutorizacion", out var ca) ? ca.GetString() ?? "000000" : "000000";
                NumeroCupon = result.TryGetProperty("numeroCupon", out var nc) ? nc.GetString() ?? "0000" : "0000";

                TxtEstado.Text = "¡PAGO APROBADO!";
                Spinner.IsIndeterminate = false;

                await Task.Delay(800); // Dar feedback visual de éxito
                DialogResult = true;
                Close();
            }
            else
            {
                var msg = result.TryGetProperty("mensaje", out var m) ? m.GetString() ?? "Denegada" : "Denegada";
                throw new Exception(msg);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelado
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
            MessageBox.Show($"Transacción Rechazada:\n{ex.Message}", "Postnet denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
            DialogResult = false;
            Close();
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        DialogResult = false;
        Close();
    }
}
