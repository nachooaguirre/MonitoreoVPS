using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Configuracion;

public partial class ConfiguracionPage : Page
{
    private ConfiguracionEmpresa? _cfg;

    public ConfiguracionPage()
    {
        InitializeComponent();
        CargarImpresoras();
        Loaded += async (_, _) => await CargarConfiguracion();
    }

    private void CargarImpresoras()
    {
        // Agrego impresoras instaladas en Windows via WMI
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Print\Printers");
            if (key is not null)
                foreach (var name in key.GetSubKeyNames())
                    CboImpresoraTicket.Items.Add(new ComboBoxItem { Content = name });
        }
        catch { }
        CboImpresoraTicket.Items.Insert(0, new ComboBoxItem { Content = "(Sin impresora de tickets)" });
        CboImpresoraTicket.SelectedIndex = 0;
    }

    private async Task CargarConfiguracion()
    {
        try
        {
            _cfg = await App.Api.GetConfiguracion();
            if (_cfg is null) { _cfg = new ConfiguracionEmpresa(); }

            TxtNombreEmpresa.Text  = _cfg.NombreEmpresa;
            TxtNombreFantasia.Text = _cfg.NombreFantasia ?? "";
            TxtCuit.Text           = _cfg.Cuit;
            TxtIIBB.Text           = _cfg.IngresosBrutos ?? "";
            TxtDireccion.Text      = _cfg.Direccion ?? "";
            TxtLocalidad.Text      = _cfg.Localidad ?? "";
            TxtProvincia.Text      = _cfg.Provincia ?? "";
            TxtTelefono.Text       = _cfg.Telefono ?? "";
            TxtEmail.Text          = _cfg.Email ?? "";
            TxtSitioWeb.Text       = _cfg.SitioWeb ?? "";
            TxtMensajePie.Text     = _cfg.MensajePiePagina ?? "";
            NumPuntoVenta.Value    = _cfg.PuntoVenta;
            CboAfipModo.SelectedIndex = _cfg.AfipHomologacion ? 0 : 1;
            TxtCertificado.Text    = _cfg.AfipCertificadoPath ?? "";
            ChkControlaStock.IsChecked = _cfg.ControlaStock;
            ChkPrecioConIva.IsChecked  = _cfg.PrecioConIva;
            TxtBackupRuta.Text     = _cfg.BackupRuta ?? "";

            // Cargar configuración de Pagos Integrados
            ChkPosnetHabilitado.IsChecked = _cfg.PosnetHabilitado;
            ChkMpqrHabilitado.IsChecked = _cfg.MpQrHabilitado;
            TxtMpAccessToken.Text = _cfg.MpAccessToken ?? "";
            TxtMpCollectorId.Text = _cfg.MpCollectorId ?? "";
            TxtMpStoreId.Text = _cfg.MpStoreId ?? "";
            TxtMpExternalPosId.Text = _cfg.MpExternalPosId ?? "";

            var pPort = _cfg.PostnetPuertoCom ?? "SIMULADOR";
            for (int i = 0; i < CboPosnetPuerto.Items.Count; i++)
                if (((ComboBoxItem)CboPosnetPuerto.Items[i]).Content?.ToString() == pPort)
                { CboPosnetPuerto.SelectedIndex = i; break; }

            // Impresora fiscal
            var modelo = _cfg.ImpresoraFiscalModelo ?? "";
            for (int i = 0; i < CboImpresoraFiscalModelo.Items.Count; i++)
                if (((ComboBoxItem)CboImpresoraFiscalModelo.Items[i]).Content?.ToString() == modelo)
                { CboImpresoraFiscalModelo.SelectedIndex = i; break; }

            var puerto = _cfg.ImpresoraFiscalPuerto ?? "";
            for (int i = 0; i < CboImpresoraFiscalPuerto.Items.Count; i++)
                if (((ComboBoxItem)CboImpresoraFiscalPuerto.Items[i]).Content?.ToString() == puerto)
                { CboImpresoraFiscalPuerto.SelectedIndex = i; break; }

            // Impresora ticket
            var ticket = _cfg.ImpresoraTicketNombre ?? "";
            for (int i = 0; i < CboImpresoraTicket.Items.Count; i++)
                if (((ComboBoxItem)CboImpresoraTicket.Items[i]).Content?.ToString() == ticket)
                { CboImpresoraTicket.SelectedIndex = i; break; }

            App.NombreEmpresa = _cfg.NombreEmpresa;
            TxtEstado.Text = "Configuración cargada";
        }
        catch (Exception ex) { TxtEstado.Text = $"Error: {ex.Message}"; }
    }

    private async void BtnProbarBalanza_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Balanza.Configurada)
        {
            TxtTestBalanza.Text = "No hay BalanzaIp configurada en appsettings.json de esta PC.";
            return;
        }

        TxtTestBalanza.Text = "Probando conexión...";
        var ok = await App.Balanza.TestConexionAsync();
        TxtTestBalanza.Text = ok
            ? "✅ Balanza respondió correctamente."
            : "❌ No se pudo conectar o la balanza no respondió (revisá IP/puerto y que esté encendida).";
    }

    private async void BtnLeerPlusBalanza_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Balanza.Configurada)
        {
            TxtTestBalanza.Text = "No hay BalanzaIp configurada en appsettings.json de esta PC.";
            return;
        }

        TxtTestBalanza.Text = "Leyendo cantidad de PLU...";
        var cantidad = await App.Balanza.LeerCantidadPlusAsync();
        TxtTestBalanza.Text = cantidad.HasValue
            ? $"✅ La balanza tiene {cantidad.Value} PLU cargados."
            : "❌ No se pudo leer (revisá IP/puerto y que esté encendida).";
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        _cfg ??= new ConfiguracionEmpresa();
        _cfg.NombreEmpresa    = TxtNombreEmpresa.Text.Trim();
        _cfg.NombreFantasia   = TxtNombreFantasia.Text.NullIfEmpty();
        _cfg.Cuit             = TxtCuit.Text.Trim();
        _cfg.IngresosBrutos   = TxtIIBB.Text.NullIfEmpty();
        _cfg.Direccion        = TxtDireccion.Text.NullIfEmpty();
        _cfg.Localidad        = TxtLocalidad.Text.NullIfEmpty();
        _cfg.Provincia        = TxtProvincia.Text.NullIfEmpty();
        _cfg.Telefono         = TxtTelefono.Text.NullIfEmpty();
        _cfg.Email            = TxtEmail.Text.NullIfEmpty();
        _cfg.SitioWeb         = TxtSitioWeb.Text.NullIfEmpty();
        _cfg.MensajePiePagina = TxtMensajePie.Text.NullIfEmpty();
        _cfg.PuntoVenta       = (int)(NumPuntoVenta.Value ?? 1);
        _cfg.AfipHomologacion = CboAfipModo.SelectedIndex == 0;
        _cfg.AfipCertificadoPath    = TxtCertificado.Text.NullIfEmpty();
        _cfg.AfipCertificadoPassword = TxtCertPassword.Password.NullIfEmpty();
        _cfg.ImpresoraFiscalModelo  = (CboImpresoraFiscalModelo.SelectedItem as ComboBoxItem)?.Content?.ToString().NullIfEmpty();
        _cfg.ImpresoraFiscalPuerto  = (CboImpresoraFiscalPuerto.SelectedItem as ComboBoxItem)?.Content?.ToString().NullIfEmpty();
        _cfg.ImpresoraTicketNombre  = (CboImpresoraTicket.SelectedItem as ComboBoxItem)?.Content?.ToString().NullIfEmpty();
        _cfg.ControlaStock  = ChkControlaStock.IsChecked == true;
        _cfg.PrecioConIva   = ChkPrecioConIva.IsChecked == true;
        _cfg.BackupRuta     = TxtBackupRuta.Text.NullIfEmpty();

        // Guardar configuración de Pagos Integrados
        _cfg.PosnetHabilitado = ChkPosnetHabilitado.IsChecked == true;
        _cfg.MpQrHabilitado = ChkMpqrHabilitado.IsChecked == true;
        _cfg.MpAccessToken = TxtMpAccessToken.Text.NullIfEmpty();
        _cfg.MpCollectorId = TxtMpCollectorId.Text.NullIfEmpty();
        _cfg.MpStoreId = TxtMpStoreId.Text.NullIfEmpty();
        _cfg.MpExternalPosId = TxtMpExternalPosId.Text.NullIfEmpty();
        _cfg.PostnetPuertoCom = (CboPosnetPuerto.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SIMULADOR";

        try
        {
            BtnGuardar.IsEnabled = false;
            await App.Api.GuardarConfiguracion(_cfg);
            App.NombreEmpresa = _cfg.NombreEmpresa;
            TxtEstado.Text = "✅ Guardado correctamente";
        }
        catch (Exception ex) { TxtEstado.Text = $"❌ Error: {ex.Message}"; }
        finally { BtnGuardar.IsEnabled = true; }
    }

    private void BtnSeleccionarCertificado_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Certificados (*.pfx;*.p12)|*.pfx;*.p12|Todos (*.*)|*.*" };
        if (dlg.ShowDialog() == true) TxtCertificado.Text = dlg.FileName;
    }

    private void BtnSeleccionarBackup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Seleccionar carpeta de backup" };
        if (dlg.ShowDialog() == true)
            TxtBackupRuta.Text = dlg.FolderName;
    }
}

file static class StringEx
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
