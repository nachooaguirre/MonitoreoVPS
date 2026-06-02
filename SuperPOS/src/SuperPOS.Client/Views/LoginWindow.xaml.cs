using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace SuperPOS.Client.Views;

public partial class LoginWindow : FluentWindow
{
    public LoginWindow()
    {
        InitializeComponent();
        CargarLogo();
        TxtUsuario.Focus();
    }

    private void CargarLogo()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // Buscar en Assets/ y Assets/Icons/ con múltiples nombres y extensiones
        var candidatos = new[]
        {
            Path.Combine(baseDir, "Assets", "Icons", "logo-07.jpeg"),
            Path.Combine(baseDir, "Assets", "Icons", "logo-07.jpg"),
            Path.Combine(baseDir, "Assets", "logo.png"),
            Path.Combine(baseDir, "Assets", "logo.jpg"),
            Path.Combine(baseDir, "Assets", "logo.jpeg"),
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
                ImgLogo.Source = bmp;
                TxtNombreEmpresa.Visibility = Visibility.Collapsed;
                return;
            }
            catch { }
        }
        // Sin logo encontrado: mostrar nombre de empresa
        TxtNombreEmpresa.Text = App.NombreEmpresa;
    }

    private void TxtUsuario_KeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) TxtPassword.Focus(); }

    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) _ = Ingresar(); }

    private void BtnIngresar_Click(object sender, RoutedEventArgs e) => _ = Ingresar();

    private async Task Ingresar()
    {
        var usuario = TxtUsuario.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
        { MostrarError("Ingrese usuario y contraseña"); return; }

        BtnIngresar.IsEnabled = false;
        TxtError.Visibility = Visibility.Collapsed;

        try
        {
            var user = await App.Api.Login(usuario, password);
            if (user is null || user.Perfil is null)
            { MostrarError("Usuario o contraseña incorrectos"); TxtPassword.Clear(); return; }

            App.UsuarioActual = user.NombreCompleto.Length > 0 ? user.NombreCompleto : user.NombreUsuario;
            App.IdUsuarioActual = user.Id;
            App.PerfilActual = user.Perfil;

            new MainWindow(App.UsuarioActual).Show();
            Close();
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo conectar al servidor.\nVerificá que la API esté corriendo.\n({ex.Message})");
        }
        finally { BtnIngresar.IsEnabled = true; }
    }

    private void MostrarError(string msg)
    {
        TxtError.Text = msg;
        TxtError.Visibility = Visibility.Visible;
    }
}
