using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SuperPOS.Client.Views.Caja;

public partial class ProcesandoPagoWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly decimal _monto;
    private readonly bool _esCredito;
    private readonly CancellationTokenSource _cts = new();

    public bool TransaccionAprobada { get; private set; }
    public string TarjetaMarca { get; private set; } = "";
    public string TarjetaUltimosDigitos { get; private set; } = "";
    public string CodigoAutorizacion { get; private set; } = "";
    public string NumeroCupon { get; private set; } = "";
    public string MensajeError { get; private set; } = "";

    public ProcesandoPagoWindow(decimal monto, bool esCredito)
    {
        InitializeComponent();
        _monto = monto;
        _esCredito = esCredito;

        TxtMonto.Text = $"Monto: $ {monto:N2}";
        TxtEstado.Text = $"Esperando tarjeta de {(esCredito ? "CRÉDITO" : "DÉBITO")} en Ingenico Lane 3000...";
        
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Delay(500); // Pequeño delay de cortesía visual
            var client = new HttpClient { BaseAddress = new Uri(App.ApiBaseUrl) };

            var requestData = new
            {
                Monto = _monto,
                EsCredito = _esCredito
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
                TarjetaMarca = result.TryGetProperty("tarjetaMarca", out var tm) ? tm.GetString() ?? "VISA" : "VISA";
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
