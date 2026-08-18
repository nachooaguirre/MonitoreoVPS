using System.IO;
using System.Net.Http;
using System.Windows;

namespace SuperPOS.Client.Services;

/// <summary>
/// Compara la version local (archivo version.txt junto al ejecutable, generado en CI)
/// contra la version publicada en el servidor (wwwroot/downloads/client-version.txt)
/// y ofrece abrir la pagina de descargas si hay una diferencia.
/// </summary>
public static class UpdateChecker
{
    public static async Task CheckAsync(string apiBaseUrl)
    {
        try
        {
            var localVersionPath = Path.Combine(AppContext.BaseDirectory, "version.txt");
            if (!File.Exists(localVersionPath)) return; // build local/dev sin version embebida

            var localVersion = File.ReadAllText(localVersionPath).Trim();
            if (string.IsNullOrWhiteSpace(localVersion)) return;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var remoteVersion = (await http.GetStringAsync($"{apiBaseUrl}/downloads/client-version.txt")).Trim();

            if (string.IsNullOrWhiteSpace(remoteVersion) || remoteVersion == localVersion) return;

            var abrir = MessageBox.Show(
                "Hay una actualización disponible para SuperPOS.\n\n¿Querés abrir la página de descargas ahora?",
                "Actualización disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (abrir == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"{apiBaseUrl}/downloads/",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Sin conexion o servidor caido: no molestar al usuario en el login.
        }
    }
}
