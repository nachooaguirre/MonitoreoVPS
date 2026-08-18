using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using SuperPOS.Client.Views;

namespace SuperPOS.Client.Services;

/// <summary>
/// Compara la version local (archivo version.txt junto al ejecutable, generado en CI)
/// contra la version publicada en el servidor (wwwroot/downloads/client-version.txt).
/// Si hay una nueva, descarga el zip dentro de la app y usa un script de PowerShell
/// auxiliar para reemplazar los archivos (la app no puede sobreescribirse mientras
/// esta corriendo) y reabrirse sola.
/// </summary>
public static class UpdateChecker
{
    private const string UpdaterScript = """
        param(
            [string]$ZipPath,
            [string]$InstallDir,
            [string]$ExeName,
            [int]$ProcessId
        )
        try { Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue } catch {}
        Start-Sleep -Seconds 1
        Expand-Archive -Path $ZipPath -DestinationPath $InstallDir -Force
        Remove-Item -Path $ZipPath -Force -ErrorAction SilentlyContinue
        Start-Process -FilePath (Join-Path $InstallDir $ExeName)
        """;

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

            var descargar = MessageBox.Show(
                "Hay una actualización disponible para SuperPOS.\n\n¿Descargarla e instalarla ahora? La aplicación se va a cerrar y reabrir sola.",
                "Actualización disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (descargar != MessageBoxResult.Yes) return;

            await DownloadAndInstallAsync(apiBaseUrl);
        }
        catch
        {
            // Sin conexion o servidor caido: no molestar al usuario en el login.
        }
    }

    private static async Task DownloadAndInstallAsync(string apiBaseUrl)
    {
        var progressWindow = new UpdateProgressWindow();
        progressWindow.Show();

        try
        {
            var updateDir = Path.Combine(Path.GetTempPath(), "SuperPOS-Update");
            Directory.CreateDirectory(updateDir);
            var zipPath = Path.Combine(updateDir, "SuperPOS-Client-win-x64.zip");

            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            using (var response = await http.GetAsync($"{apiBaseUrl}/downloads/SuperPOS-Client-win-x64.zip", HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using var httpStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(zipPath);

                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;
                while ((bytesRead = await httpStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                    if (totalBytes > 0)
                    {
                        var pct = (int)(totalRead * 100 / totalBytes);
                        progressWindow.SetProgreso(pct);
                    }
                }
            }

            progressWindow.SetEstado("Instalando actualización...");
            progressWindow.SetProgreso(100);

            var scriptPath = Path.Combine(updateDir, "updater.ps1");
            await File.WriteAllTextAsync(scriptPath, UpdaterScript);

            var installDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
            var exeName = Path.GetFileName(Environment.ProcessPath) ?? "SuperPOS.Client.exe";

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\" " +
                            $"-ZipPath \"{zipPath}\" -InstallDir \"{installDir}\" -ExeName \"{exeName}\" -ProcessId {Environment.ProcessId}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            progressWindow.Close();
            MessageBox.Show($"No se pudo descargar la actualización:\n{ex.Message}", "SuperPOS",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
