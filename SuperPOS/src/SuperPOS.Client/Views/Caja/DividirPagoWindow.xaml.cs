using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Caja;

public partial class DividirPagoWindow : Window
{
    private readonly decimal _totalVenta;
    private readonly ObservableCollection<PagoItem> _pagosAgregados = new();
    public List<ComprobantePago> Pagos { get; } = new();
    /// <summary>Suma de recargos/descuentos por tarjeta aplicados en este cobro (para sumar al total del comprobante).</summary>
    public decimal TotalRecargo { get; private set; }

    public DividirPagoWindow(decimal totalVenta, List<MedioPago> mediosPago)
    {
        InitializeComponent();
        _totalVenta = totalVenta;

        TxtTotalVenta.Text = $"$ {_totalVenta:N2}";
        CmbMedio.ItemsSource = mediosPago;
        CmbMedio.SelectedIndex = 0;

        DgPagos.ItemsSource = _pagosAgregados;

        RecalcularCobro();
        TxtMonto.Focus();
    }

    private void RecalcularCobro()
    {
        decimal acumulado = 0;
        foreach (var pago in _pagosAgregados)
        {
            decimal restante = _totalVenta - acumulado;
            if (restante <= 0)
            {
                pago.Importe = 0;
                pago.Vuelto = pago.MontoIngresado;
            }
            else if (pago.Medio.Tipo == TipoMedioPago.Efectivo)
            {
                if (pago.MontoIngresado > restante)
                {
                    pago.Importe = restante;
                    pago.Vuelto = pago.MontoIngresado - restante;
                }
                else
                {
                    pago.Importe = pago.MontoIngresado;
                    pago.Vuelto = 0;
                }
            }
            else
            {
                pago.Importe = Math.Min(pago.MontoIngresado, restante);
                pago.Vuelto = 0;
            }
            acumulado += pago.Importe;
        }

        decimal totalRestante = Math.Max(0, _totalVenta - acumulado);
        decimal totalVuelto = _pagosAgregados.Sum(p => p.Vuelto);

        TxtTotalCubierto.Text = $"$ {acumulado:N2}";

        if (totalRestante > 0)
        {
            LblRestanteVuelto.Text = "SALDO RESTANTE";
            TxtRestanteVuelto.Text = $"$ {totalRestante:N2}";
            TxtRestanteVuelto.Foreground = new SolidColorBrush(Color.FromRgb(255, 112, 112)); // #FF7070
            BrdRestanteVuelto.Background = new SolidColorBrush(Color.FromRgb(42, 26, 26)); // #2A1A1A
            BrdRestanteVuelto.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 42, 42)); // #5A2A2A
            BtnConfirmar.IsEnabled = false;
        }
        else
        {
            LblRestanteVuelto.Text = "VUELTO";
            TxtRestanteVuelto.Text = $"$ {totalVuelto:N2}";
            TxtRestanteVuelto.Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 128)); // #64C880
            BrdRestanteVuelto.Background = new SolidColorBrush(Color.FromRgb(26, 58, 42)); // #1A3A2A
            BrdRestanteVuelto.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 90, 58)); // #2A5A3A
            BtnConfirmar.IsEnabled = true;
        }

        DgPagos.Items.Refresh();

        // Actualizar monto sugerido en el input
        TxtMonto.Text = $"{totalRestante:N2}".Replace(',', '.');
        TxtMonto.SelectAll();
    }

    private void CmbMedio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbMedio.SelectedItem is MedioPago medio)
        {
            bool esIntegrado = (medio.Tipo == TipoMedioPago.TarjetaDebito || medio.Tipo == TipoMedioPago.TarjetaCredito || medio.Tipo == TipoMedioPago.MercadoPago);
            TxtReferencia.IsEnabled = !esIntegrado;
            TxtReferencia.Text = esIntegrado ? "[Integración de hardware activa]" : "";
        }
    }

    private async void BtnAgregarPago_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMedio.SelectedItem is not MedioPago medio) return;

        string montoStr = TxtMonto.Text.Trim().Replace(',', '.');
        if (!decimal.TryParse(montoStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
        {
            MessageBox.Show("Ingrese un monto válido mayor a cero.", "Monto inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal totalCubierto = _pagosAgregados.Sum(p => p.Importe);
        decimal restante = _totalVenta - totalCubierto;

        if (restante <= 0)
        {
            MessageBox.Show("El total de la venta ya ha sido completamente cubierto.", "Total cubierto", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (medio.Tipo != TipoMedioPago.Efectivo && monto > restante)
        {
            MessageBox.Show($"Para medios de pago que no sean Efectivo, el monto no puede superar el saldo restante ($ {restante:N2}).", "Monto excedido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string referencia = TxtReferencia.Text.Trim();
        if (referencia == "[Integración de hardware activa]")
            referencia = "";

        // Verificar integraciones activas
        if (medio.Tipo == TipoMedioPago.TarjetaDebito || medio.Tipo == TipoMedioPago.TarjetaCredito)
        {
            try
            {
                var globalCfg = await App.Api.GetConfiguracion();
                if (globalCfg?.PosnetHabilitado == true)
                {
                    var dlg = new ProcesandoPagoWindow(monto, esCredito: medio.Tipo == TipoMedioPago.TarjetaCredito) { Owner = this };
                    if (dlg.ShowDialog() != true)
                    {
                        return;
                    }
                    referencia = $"{dlg.TarjetaMarca} (*{dlg.TarjetaUltimosDigitos}) Aut:{dlg.CodigoAutorizacion} Cup:{dlg.NumeroCupon}";
                    if (dlg.Recargo != 0)
                    {
                        TotalRecargo += dlg.Recargo;
                        referencia += $" | Recargo: $ {dlg.Recargo:N2}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con el postnet:\n{ex.Message}", "Error de Conexión");
                return;
            }
        }
        else if (medio.Tipo == TipoMedioPago.MercadoPago)
        {
            try
            {
                var globalCfg = await App.Api.GetConfiguracion();
                if (globalCfg?.MpQrHabilitado == true)
                {
                    var dlg = new MercadoPagoQrWindow(monto) { Owner = this };
                    if (dlg.ShowDialog() != true)
                    {
                        return;
                    }
                    referencia = dlg.ReferenciaPago;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con Mercado Pago:\n{ex.Message}", "Error de Conexión");
                return;
            }
        }

        var item = new PagoItem
        {
            Medio = medio,
            MontoIngresado = monto,
            Referencia = string.IsNullOrEmpty(referencia) ? "" : referencia
        };

        _pagosAgregados.Add(item);
        RecalcularCobro();

        TxtReferencia.Text = "";
        TxtMonto.Focus();
    }

    private void BtnQuitarPago_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PagoItem item)
        {
            _pagosAgregados.Remove(item);
            RecalcularCobro();
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        decimal totalCubierto = _pagosAgregados.Sum(p => p.Importe);
        if (totalCubierto < _totalVenta)
        {
            MessageBox.Show("El total ingresado no cubre el total de la venta.", "Cobro incompleto", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Pagos.Clear();
        foreach (var p in _pagosAgregados)
        {
            Pagos.Add(new ComprobantePago
            {
                IdMedioPago = p.Medio.Id,
                Importe = p.Importe,
                Vuelto = p.Vuelto,
                Referencia = p.Referencia
            });
        }

        DialogResult = true;
        Close();
    }
}

public class PagoItem
{
    public MedioPago Medio { get; set; } = null!;
    public decimal MontoIngresado { get; set; }
    public decimal Importe { get; set; }
    public decimal Vuelto { get; set; }
    public string Referencia { get; set; } = "";
}
