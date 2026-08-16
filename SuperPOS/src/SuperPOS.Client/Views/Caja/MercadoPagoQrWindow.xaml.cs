using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SuperPOS.Client.Views.Caja;

public partial class MercadoPagoQrWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly decimal _monto;
    private string _referencia = "";
    private readonly DispatcherTimer _timer;
    private readonly HttpClient _client;

    public bool TransaccionAprobada { get; private set; }
    public string ReferenciaPago { get; private set; } = "";

    public MercadoPagoQrWindow(decimal monto)
    {
        InitializeComponent();
        _monto = monto;

        TxtMonto.Text = $"Monto: $ {monto:N2}";
        _client = new HttpClient { BaseAddress = new Uri(App.ApiBaseUrl) };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += Timer_Tick;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            TxtEstado.Text = "Generando código QR dinámico de Mercado Pago...";
            
            var response = await _client.PostAsJsonAsync("api/pagos-integrados/mercadopago/qr/crear", new { Monto = _monto });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al crear orden en MP: {err}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            _referencia = result.GetProperty("referencia").GetString() ?? "";
            string qrData = result.GetProperty("qrData").GetString() ?? "";

            // Generar imagen de QR usando servicio público rápido y limpio
            var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=250x250&data={Uri.EscapeDataString(qrData)}";
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(qrUrl);
            bitmap.EndInit();
            ImgQr.Source = bitmap;

            TxtEstado.Text = "QR generado. Esperando escaneo y pago del cliente...";
            
            // Iniciar consulta de estado automática
            _timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al iniciar Mercado Pago QR:\n{ex.Message}", "Error de Pago", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
            Close();
        }
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_referencia)) return;

        try
        {
            var response = await _client.GetFromJsonAsync<JsonElement>($"api/pagos-integrados/mercadopago/estado/{_referencia}");
            bool pagado = response.GetProperty("pagado").GetBoolean();

            if (pagado)
            {
                _timer.Stop();
                TransaccionAprobada = true;
                ReferenciaPago = $"Mercado Pago: {_referencia}";
                TxtEstado.Text = "¡PAGO RECIBIDO CON ÉXITO!";
                TxtEstado.Foreground = System.Windows.Media.Brushes.GreenYellow;

                await Task.Delay(1000); // Dar feedback visual del pago
                DialogResult = true;
                Close();
            }
        }
        catch
        {
            // Omitir fallos temporales de red durante el polling
        }
    }

    private async void BtnSimularPago_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_referencia)) return;

        try
        {
            BtnSimularPago.IsEnabled = false;
            var response = await _client.PostAsync($"api/pagos-integrados/mercadopago/simular-pago/{_referencia}", null);
            if (response.IsSuccessStatusCode)
            {
                // El timer detectará el cambio de estado en el siguiente tick, o forzamos aprobación local
                TxtEstado.Text = "Simulando aprobación de pago...";
            }
            else
            {
                MessageBox.Show("No se pudo simular el pago en el servidor.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al simular: {ex.Message}");
        }
        finally
        {
            BtnSimularPago.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
    }
}
