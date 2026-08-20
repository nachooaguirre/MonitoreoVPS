using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using SuperPOS.Client.Models;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client;

public partial class App : Application
{
    public static string ApiBaseUrl { get; set; } = "http://localhost:5075";
    public static string NombreEmpresa { get; set; } = "Los Angeles Supermercados";
    public static ApiService Api { get; private set; } = null!;
    public static LocalCacheService Cache { get; } = new();
    /// <summary>Solo tiene efecto si esta PC configuró una balanza Kretz en su red local (appsettings.json).</summary>
    public static KretzBalanzaService Balanza { get; private set; } = new(null);
    public static string UsuarioNombre { get; set; } = "";
    // Retrocompatibilidad con código viejo que usa UsuarioActual como string
    public static string UsuarioActual { get => UsuarioNombre; set => UsuarioNombre = value; }
    public static UsuarioInfo? UsuarioSession { get; set; }
    public static int IdUsuarioActual { get; set; } = 1;
    public static int CajaId { get; set; } = 1;
    public static int SucursalId { get; set; } = 1;
    public static Perfil PerfilActual { get; set; } = new() { EsAdministrador = true, AccesoCaja = true };

    private static System.Threading.Timer? _syncTimer;
    private static int _syncTicks;

    public App()
    {
        CargarConfiguracionLocal();
        Api = new ApiService(ApiBaseUrl);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _ = Cache.InicializarAsync(); // solo crea el archivo/tablas locales, no toca la red
    }

    /// <summary>
    /// Arranca la sincronización offline recién después de loguearse (no en el arranque de la
    /// app, para no pegarle al servidor desde la pantalla de login) y solo si este perfil puede
    /// vender — un puesto de back-office que nunca abre Caja no necesita el catálogo cacheado.
    /// Con 30+ terminales en simultáneo, un jitter inicial evita que todas golpeen el server
    /// al mismo segundo; el drenado de ventas pendientes es liviano y corre cada 3 min, pero el
    /// refresco completo del catálogo (pesado, hasta 20k artículos) es cada 6 ticks (~18 min).
    /// </summary>
    public static void IniciarSincronizacionSiCorresponde()
    {
        if (_syncTimer != null) return; // ya arrancada (ej. re-login en la misma sesión del proceso)
        if (!PerfilActual.AccesoCaja && !PerfilActual.EsAdministrador) return;

        var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 60));
        _syncTimer = new System.Threading.Timer(async _ => await SincronizarAsync(), null, jitter, TimeSpan.FromMinutes(3));
    }

    private static async Task SincronizarAsync()
    {
        try
        {
            foreach (var (id, cbte) in await Cache.ObtenerPendientesAsync())
            {
                await Api.RegistrarVenta(cbte);
                await Cache.EliminarPendienteAsync(id);
            }

            _syncTicks++;
            if (_syncTicks % 6 == 1) // primer tick incluido, después cada ~18 min
                await Cache.RefrescarAsync(Api);
        }
        catch
        {
            // ponytail: sin conexión o servidor caído, se reintenta en el próximo tick
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Error inesperado:\n\n{e.Exception.GetType().Name}\n{e.Exception.Message}\n\n{e.Exception.InnerException?.Message}",
            "SuperPOS - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            MessageBox.Show($"Error fatal:\n{ex.Message}", "SuperPOS", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>Lee appsettings.json junto al ejecutable (ApiBaseUrl, etc.).</summary>
    private static void CargarConfiguracionLocal()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("ApiBaseUrl", out var u) && u.ValueKind == JsonValueKind.String)
            {
                var url = u.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                    ApiBaseUrl = url.TrimEnd('/');
            }
            if (root.TryGetProperty("NombreEmpresa", out var n) && n.ValueKind == JsonValueKind.String)
            {
                var ne = n.GetString();
                if (!string.IsNullOrWhiteSpace(ne))
                    NombreEmpresa = ne;
            }
            if (root.TryGetProperty("BalanzaIp", out var bip) && bip.ValueKind == JsonValueKind.String)
            {
                var puerto = root.TryGetProperty("BalanzaPuerto", out var bp) && bp.TryGetInt32(out var p) ? p : 1001;
                Balanza = new KretzBalanzaService(bip.GetString(), puerto);
            }
        }
        catch
        {
            /* ignorar config rota */
        }
    }
}
