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
    public static string UsuarioNombre { get; set; } = "";
    // Retrocompatibilidad con código viejo que usa UsuarioActual como string
    public static string UsuarioActual { get => UsuarioNombre; set => UsuarioNombre = value; }
    public static UsuarioInfo? UsuarioSession { get; set; }
    public static int IdUsuarioActual { get; set; } = 1;
    public static int CajaId { get; set; } = 1;
    public static int SucursalId { get; set; } = 1;
    public static Perfil PerfilActual { get; set; } = new() { EsAdministrador = true, AccesoCaja = true };

    private static System.Threading.Timer? _syncTimer;

    public App()
    {
        CargarConfiguracionLocal();
        Api = new ApiService(ApiBaseUrl);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _ = IniciarCacheYSincronizacionAsync();
    }

    /// <summary>
    /// Al arrancar, y cada 3 minutos, refresca la caché local de precios/stock (para poder
    /// seguir vendiendo si se corta internet) y drena la cola de ventas offline pendientes.
    /// </summary>
    private static async Task IniciarCacheYSincronizacionAsync()
    {
        await Cache.InicializarAsync();
        _syncTimer = new System.Threading.Timer(async _ => await SincronizarAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
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
        }
        catch
        {
            /* ignorar config rota */
        }
    }
}
