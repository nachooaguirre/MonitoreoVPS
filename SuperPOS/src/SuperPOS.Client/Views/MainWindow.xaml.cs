using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using SuperPOS.Client.Models;
using SuperPOS.Client.Views.Caja;
using SuperPOS.Client.Views.Clientes;
using SuperPOS.Client.Views.Compras;
using SuperPOS.Client.Views.Configuracion;
using SuperPOS.Client.Views.CtaCte;
using SuperPOS.Client.Views.Inventario;
using SuperPOS.Client.Views.OrdenesCompra;
using SuperPOS.Client.Views.Proveedores;
using SuperPOS.Client.Views.Sucursales;
using SuperPOS.Client.Views.Remitos;
using SuperPOS.Client.Views.Tesoreria;
using SuperPOS.Client.Views.Reportes;
using SuperPOS.Client.Views.Stock;
using SuperPOS.Client.Views.Shared;
using SuperPOS.Client.Views.Usuarios;
using SuperPOS.Client.Views.Auditoria;
using SuperPOS.Client.Views.Ai;
using SuperPOS.Client.Views.Presupuestos;
using SuperPOS.Client.Views.Precios;
using SuperPOS.Client.Views.Ofertas;

namespace SuperPOS.Client.Views;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly DispatcherTimer _stockTimer;
    private string? _firmaStockBajo;
    private List<ArticuloStockDto> _alertas = [];
    private HubConnection? _posHub;

    private bool _esPantallaCompleta;
    private WindowStyle _savedWindowStyle;
    private WindowState _savedWindowState;
    private ResizeMode _savedResizeMode;

    public MainWindow(string usuario)
    {
        InitializeComponent();
        TxtUsuarioNav.Text = $"🚪  {usuario}";
        CargarLogoNav();
        // SignalR (VentaRealizada/StockBajo) ya empuja la actualización en tiempo real — este timer
        // es solo respaldo por si el hub se desconecta, así que no hace falta que sea frecuente.
        // Con muchas terminales abiertas a la vez, cada segundo de más acá es tráfico multiplicado.
        _stockTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _stockTimer.Tick += async (_, _) => await ConsultarAlertasStockAsync();
        Loaded += OnMainWindowLoaded;
        Closed += (_, _) => { _stockTimer.Stop(); _ = _posHub?.DisposeAsync(); };
        AplicarPermisos();
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        NavigarA("caja");
        if (PanelCampana.Visibility == Visibility.Visible)
        {
            _stockTimer.Start();
            _ = ConsultarAlertasStockAsync();
        }
        _ = ConectarPosHubAsync();
    }

    /// <summary>Conecta al hub SignalR de la API para recibir avisos en tiempo real (venta realizada, stock bajo).</summary>
    private async Task ConectarPosHubAsync()
    {
        try
        {
            _posHub = new HubConnectionBuilder()
                .WithUrl($"{App.ApiBaseUrl}/hubs/pos")
                .WithAutomaticReconnect()
                .Build();

            _posHub.On<int, decimal, System.DateTime>("VentaRealizada", (_, _, _) =>
                Dispatcher.InvokeAsync(RefrescarAlertasStock));
            _posHub.On<int, string, decimal>("StockBajo", (_, _, _) =>
                Dispatcher.InvokeAsync(RefrescarAlertasStock));
            _posHub.On<int>("ArticuloActualizado", async idArticulo => await SincronizarBalanzaAsync(idArticulo));

            await _posHub.StartAsync();
        }
        catch
        {
            /* API sin SignalR disponible; la campana sigue funcionando por polling */
        }
    }

    /// <summary>Llama desde otras ventanas (p. ej. al guardar artículo) para actualizar la campana.</summary>
    public void RefrescarAlertasStock() => _ = ConsultarAlertasStockAsync();

    /// <summary>Si esta PC tiene configurada una balanza Kretz en su red local, le transmite el precio actualizado.</summary>
    private static async Task SincronizarBalanzaAsync(int idArticulo)
    {
        if (!App.Balanza.Configurada) return;
        try
        {
            var art = await App.Api.GetArticulo(idArticulo);
            if (art != null) await App.Balanza.EnviarPrecioAsync(art);
        }
        catch
        {
            // ponytail: balanza apagada/desconectada, se reintenta con el próximo cambio de precio
        }
    }

    private async Task ConsultarAlertasStockAsync()
    {
        if (PanelCampana.Visibility != Visibility.Visible)
            return;
        try
        {
            var r = await App.Api.GetStockBajoMinimo();
            if (r?.Articulos is null)
                return;
            var firma = CalcularFirmaStock(r);
            var avisar = _firmaStockBajo is not null && firma != _firmaStockBajo && r.Total > 0;
            _firmaStockBajo = firma;
            _alertas = r.Articulos;
            await Dispatcher.InvokeAsync(() =>
            {
                if (avisar)
                    SystemSounds.Asterisk.Play();
                ActualizarUiAlertas(r.Total);
            });
        }
        catch
        {
            /* API no disponible; no crashear el monitor */
        }
    }

    private static string CalcularFirmaStock(StockBajoMinimoResult r)
    {
        if (r.Articulos is null || r.Articulos.Count == 0)
            return "";
        return string.Join("|", r.Articulos.OrderBy(x => x.Id)
            .Select(x => $"{x.Id}:{x.StockActual:0.#########}:{x.StockMinimo:0.#########}"));
    }

    private void ActualizarUiAlertas(int total)
    {
        TxtConteoAlertas.Text = total > 99 ? "99+" : total.ToString();
        BadgeAlertas.Visibility = total > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtTituloAlertas.Text = total == 0
            ? "Alertas de stock"
            : $"{total} artículo(s) al o bajo el mínimo";
        TxtVacioAlertas.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;
        ItemsAlertas.ItemsSource = null;
        ItemsAlertas.ItemsSource = _alertas;
    }

    private async void BtnAlertasStock_Click(object sender, RoutedEventArgs e)
    {
        await ConsultarAlertasStockAsync();
        PopupAlertas.IsOpen = true;
    }

    private void BtnIrAsistente_Click(object sender, RoutedEventArgs e)
    {
        PopupAlertas.IsOpen = false;
        NavigarA("ai");
    }

    private void BtnIrReportes_Click(object sender, RoutedEventArgs e)
    {
        PopupAlertas.IsOpen = false;
        NavigarA("reportes");
    }

    private void BtnIrStock_Click(object sender, RoutedEventArgs e)
    {
        PopupAlertas.IsOpen = false;
        NavigarA("stock");
    }

    private void CargarLogoNav()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidatos = new[]
        {
            Path.Combine(baseDir, "Assets", "Icons", "logo-07.jpeg"),
            Path.Combine(baseDir, "Assets", "Icons", "logo-07.jpg"),
            Path.Combine(baseDir, "Assets", "logo.png"),
            Path.Combine(baseDir, "Assets", "logo.jpg"),
        };
        foreach (var ruta in candidatos)
        {
            if (!File.Exists(ruta)) continue;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                ImgLogoNav.Source = bmp;
                TxtEmpresaNav.Visibility = Visibility.Collapsed;
                return;
            }
            catch { }
        }
    }

    private void AplicarPermisos()
    {
        var p = App.PerfilActual;
        BtnCaja.IsEnabled          = p.AccesoCaja || p.EsAdministrador;
        BtnArticulos.IsEnabled     = p.AccesoArticulos || p.EsAdministrador;
        BtnOfertas.IsEnabled       = p.AccesoArticulos || p.EsAdministrador;
        BtnClientes.IsEnabled      = p.AccesoClientes || p.EsAdministrador;
        BtnProveedores.IsEnabled   = p.AccesoProveedores || p.EsAdministrador;
        BtnPresupuestos.IsEnabled  = p.AccesoCaja || p.AccesoClientes || p.EsAdministrador;
        BtnOrdenesCompra.IsEnabled    = p.AccesoCompras || p.EsAdministrador;
        BtnTarifasProveedor.IsEnabled = p.AccesoCompras || p.EsAdministrador;
        BtnEtiquetas.IsEnabled        = p.AccesoStock || p.AccesoArticulos || p.EsAdministrador;
        BtnRemitos.IsEnabled          = p.AccesoCompras || p.EsAdministrador;
        BtnRemitosZebra.IsEnabled     = p.AccesoCompras || p.EsAdministrador;
        BtnTesoreria.IsEnabled     = p.EsAdministrador;
        BtnInventario.IsEnabled    = p.AccesoStock || p.EsAdministrador;
        BtnStock.IsEnabled         = p.AccesoStock || p.EsAdministrador;
        BtnTrazabilidad.IsEnabled  = p.AccesoStock || p.EsAdministrador;
        BtnCtaCte.IsEnabled        = p.AccesoCtaCte || p.EsAdministrador;
        BtnReportes.IsEnabled      = p.AccesoReportes || p.EsAdministrador;
        BtnContabilidad.IsEnabled  = p.AccesoReportes || p.EsAdministrador;
        BtnConfig.IsEnabled        = p.AccesoConfiguracion || p.EsAdministrador;
        BtnUsuarios.Visibility     = (p.AccesoUsuarios || p.EsAdministrador) ? Visibility.Visible : Visibility.Collapsed;
        BtnSucursales.Visibility   = p.EsAdministrador ? Visibility.Visible : Visibility.Collapsed;
        BtnTerminales.Visibility   = p.EsAdministrador ? Visibility.Visible : Visibility.Collapsed;
        BtnAuditoria.Visibility    = p.EsAdministrador ? Visibility.Visible : Visibility.Collapsed;

        var verCampana = p.EsAdministrador || p.AccesoStock || p.AccesoCompras || p.AccesoReportes;
        PanelCampana.Visibility = verCampana ? Visibility.Visible : Visibility.Collapsed;
        if (!verCampana)
        {
            PopupAlertas.IsOpen = false;
            _stockTimer.Stop();
        }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn) NavigarA(btn.Tag?.ToString() ?? "");
    }

    private void NavigarA(string destino)
    {
        Page? page = destino switch
        {
            "caja"          => new CajaPage(),
            "articulos"     => new ArticulosPage(),
            "ofertas"       => new OfertasPage(),
            "clientes"      => new ClientesPage(),
            "proveedores"   => new ProveedoresPage(),
            "presupuestos"  => new PresupuestosPage(),
            "ordenescompra"   => new OrdenesCompraPage(),
            "tarifasproveedor" => new ListasPrecioProveedorPage(),
            "etiquetas"       => new EtiquetasGondolaPage(),
            "remitos"         => new RemitosPage(),
            "remitoszebra"    => new RemitosZebraPage(),
            "tesoreria"     => new TesoreriaPage(),
            "inventario"    => new InventarioPage(),
            "stock"         => new StockPage(),
            "trazabilidad"  => new TrazabilidadPage(),
            "ctacte"        => new CtaCtePage(),
            "reportes"      => new ReportesPage(),
            "contabilidad"  => new ContabilidadPage(),
            "ai"            => new AiAsistentePage(),
            "configuracion" => new ConfiguracionPage(),
            "usuarios"      => new UsuariosPage(),
            "sucursales"    => new SucursalesPage(),
            "terminales"    => new TerminalesPage(),
            "auditoria"     => new AuditoriaPage(),
            "logout"        => null,
            _               => new ProximamentePage(destino)
        };

        if (destino == "logout")
        {
            if (MessageBox.Show("¿Cerrar sesión?", "SuperPOS", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                new LoginWindow().Show();
                Close();
            }
            return;
        }

        if (page is not null) ContentFrame.Navigate(page);
    }

    public void TogglePantallaCompleta(bool entrar)
    {
        if (entrar == _esPantallaCompleta) return;
        _esPantallaCompleta = entrar;

        if (entrar)
        {
            // Guardar estado original
            _savedWindowStyle = WindowStyle;
            _savedWindowState = WindowState;
            _savedResizeMode = ResizeMode;

            // Ocultar barra superior y lateral
            RowTitleBar.Height = new GridLength(0);
            GridTitleBar.Visibility = Visibility.Collapsed;
            GridTitleBar.MinHeight = 0;
            SidebarBorder.Visibility = Visibility.Collapsed;
            ColSidebar.Width = new GridLength(0);

            // Ajustar ventana a pantalla completa
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            // Restaurar estado original
            WindowStyle = _savedWindowStyle;
            ResizeMode = _savedResizeMode;
            WindowState = _savedWindowState;

            // Mostrar barra superior y lateral
            RowTitleBar.Height = GridLength.Auto;
            GridTitleBar.Visibility = Visibility.Visible;
            GridTitleBar.MinHeight = 32;
            SidebarBorder.Visibility = Visibility.Visible;
            ColSidebar.Width = new GridLength(200);
        }
    }
}
