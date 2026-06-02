using System.Windows;
using System.Windows.Media;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Caja;

public partial class CierreCajaWindow : Window
{
    private ArqueoDto? _arqueo;
    private bool _cierreRealizado;

    public bool CierreRealizado => _cierreRealizado;

    public CierreCajaWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarArqueo();
    }

    private async Task CargarArqueo()
    {
        try
        {
            _arqueo = await App.Api.GetArqueoCaja(App.CajaId);
            if (_arqueo is null) return;

            TxtNroZeta.Text = _arqueo.NroZetaSiguiente.ToString("D4");
            TxtPeriodo.Text = $"Desde {_arqueo.FechaDesde:dd/MM/yyyy HH:mm} hasta ahora";
            TxtTotalVentas.Text = _arqueo.TotalVentas.ToString("$ #,##0.00");
            TxtCantVentas.Text = $"{_arqueo.CantidadVentas} ventas emitidas";
            TxtDescuentos.Text = _arqueo.TotalDescuentos.ToString("$ #,##0.00");
            TxtIva21.Text = _arqueo.TotalIva21.ToString("$ #,##0.00");
            TxtIvaTotal.Text = (_arqueo.TotalIva21 + _arqueo.TotalIva105).ToString("$ #,##0.00");
            TxtIva105.Text = _arqueo.TotalIva105.ToString("$ #,##0.00");

            // Efectivo del sistema
            var efectivo = _arqueo.DetallesMedios.FirstOrDefault(d => d.Id == 1);
            TxtEfectivoSistema.Text = (efectivo?.Total ?? 0).ToString("$ #,##0.00");

            IcMedios.ItemsSource = _arqueo.DetallesMedios;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando arqueo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtEfectivoDeclarado_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_arqueo is null) return;
        if (!decimal.TryParse(TxtEfectivoDeclarado.Text.Replace("$", "").Trim(), out var declarado)) return;

        var sistema = _arqueo.DetallesMedios.FirstOrDefault(d => d.Id == 1)?.Total ?? 0;
        var dif = declarado - sistema;

        TxtDiferencia.Text = dif >= 0 ? $"+{dif:N2}" : dif.ToString("N2");
        TxtDiferencia.Foreground = dif >= 0
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0xD0, 0x80))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x50, 0x50));
    }

    private async void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        if (_arqueo is null) return;

        decimal declarado = 0;
        decimal.TryParse(TxtEfectivoDeclarado.Text.Replace("$", "").Trim(), out declarado);

        var confirm = MessageBox.Show(
            $"¿Confirmar CIERRE de caja?\n\n" +
            $"Zeta N°: {_arqueo.NroZetaSiguiente:D4}\n" +
            $"Total ventas: {_arqueo.TotalVentas:$ #,##0.00}\n" +
            $"Efectivo declarado: {declarado:$ #,##0.00}\n\n" +
            "Esta acción no se puede deshacer.",
            "Confirmar Cierre Z",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        BtnCerrar.IsEnabled = false;
        try
        {
            var resultado = await App.Api.CerrarCaja(new
            {
                IdCaja = App.CajaId,
                IdSucursal = App.SucursalId,
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                EfectivoDeclarado = declarado,
                Observaciones = (string?)null
            });

            _cierreRealizado = true;
            MessageBox.Show($"✅ Cierre realizado exitosamente.\nZeta N°: {_arqueo.NroZetaSiguiente:D4}", "Cierre Completado", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cerrar caja: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnCerrar.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
}
