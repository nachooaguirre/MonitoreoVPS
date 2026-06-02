using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Usuarios;

public partial class UsuariosPage : Page
{
    private Perfil? _perfilEditando;

    public UsuariosPage() { InitializeComponent(); Loaded += OnLoaded; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await CargarUsuarios();
        await CargarPerfiles();
    }

    private async Task CargarUsuarios()
    {
        DgUsuarios.ItemsSource = null;
        DgUsuarios.ItemsSource = await App.Api.GetUsuarios();
    }

    private async Task CargarPerfiles()
    {
        var perfiles = await App.Api.GetPerfiles();
        LstPerfiles.ItemsSource = perfiles;
    }

    private void LstPerfiles_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (LstPerfiles.SelectedItem is Perfil p) CargarEditorPerfil(p);
    }

    private void CargarEditorPerfil(Perfil p)
    {
        _perfilEditando = p;
        TxtNombrePerfil.Text = p.Nombre;
        ChkAccesoCaja.IsChecked        = p.AccesoCaja;
        ChkAccesoArticulos.IsChecked   = p.AccesoArticulos;
        ChkAccesoClientes.IsChecked    = p.AccesoClientes;
        ChkAccesoProveedores.IsChecked = p.AccesoProveedores;
        ChkAccesoCompras.IsChecked     = p.AccesoCompras;
        ChkAccesoStock.IsChecked       = p.AccesoStock;
        ChkAccesoCtaCte.IsChecked      = p.AccesoCtaCte;
        ChkAccesoReportes.IsChecked    = p.AccesoReportes;
        ChkAccesoConfig.IsChecked      = p.AccesoConfiguracion;
        ChkAccesoUsuarios.IsChecked    = p.AccesoUsuarios;
        ChkPuedeVender.IsChecked         = p.PuedeVender;
        ChkPuedeAnular.IsChecked         = p.PuedeAnularVentas;
        ChkPuedeDescuentos.IsChecked     = p.PuedeHacerDescuentos;
        ChkPuedeCambiarPrecios.IsChecked = p.PuedeCambiarPrecios;
        ChkPuedeVerCostos.IsChecked      = p.PuedeVerCostos;
        ChkPuedeModifStock.IsChecked     = p.PuedeModificarStock;
        ChkPuedeAbrirCaja.IsChecked      = p.PuedeAbrirCaja;
        ChkPuedeCerrarCaja.IsChecked     = p.PuedeCerrarCaja;
        ChkEsAdmin.IsChecked             = p.EsAdministrador;
        TxtMaxDescuento.Text             = p.MaximoDescuento.ToString("N0");
    }

    private async void BtnNuevoPerfil_Click(object sender, RoutedEventArgs e)
    {
        _perfilEditando = null;
        LstPerfiles.SelectedItem = null;
        TxtNombrePerfil.Text = "";
        foreach (CheckBox cb in new[] { ChkAccesoCaja, ChkAccesoArticulos, ChkAccesoClientes, ChkAccesoProveedores,
            ChkAccesoCompras, ChkAccesoStock, ChkAccesoCtaCte, ChkAccesoReportes, ChkAccesoConfig, ChkAccesoUsuarios,
            ChkPuedeVender, ChkPuedeAnular, ChkPuedeDescuentos, ChkPuedeCambiarPrecios, ChkPuedeVerCostos,
            ChkPuedeModifStock, ChkPuedeAbrirCaja, ChkPuedeCerrarCaja, ChkEsAdmin })
            cb.IsChecked = false;
        ChkPuedeVender.IsChecked = true;
        ChkAccesoCaja.IsChecked = true;
        ChkPuedeAbrirCaja.IsChecked = true;
        TxtMaxDescuento.Text = "0";
        TxtNombrePerfil.Focus();
    }

    private async void BtnGuardarPerfil_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombrePerfil.Text))
        { MessageBox.Show("El nombre del perfil es obligatorio."); return; }

        decimal.TryParse(TxtMaxDescuento.Text.Replace(',', '.'),
            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var maxDto);

        var p = _perfilEditando ?? new Perfil();
        p.Nombre                = TxtNombrePerfil.Text.Trim();
        p.AccesoCaja            = ChkAccesoCaja.IsChecked == true;
        p.AccesoArticulos       = ChkAccesoArticulos.IsChecked == true;
        p.AccesoClientes        = ChkAccesoClientes.IsChecked == true;
        p.AccesoProveedores     = ChkAccesoProveedores.IsChecked == true;
        p.AccesoCompras         = ChkAccesoCompras.IsChecked == true;
        p.AccesoStock           = ChkAccesoStock.IsChecked == true;
        p.AccesoCtaCte          = ChkAccesoCtaCte.IsChecked == true;
        p.AccesoReportes        = ChkAccesoReportes.IsChecked == true;
        p.AccesoConfiguracion   = ChkAccesoConfig.IsChecked == true;
        p.AccesoUsuarios        = ChkAccesoUsuarios.IsChecked == true;
        p.PuedeVender           = ChkPuedeVender.IsChecked == true;
        p.PuedeAnularVentas     = ChkPuedeAnular.IsChecked == true;
        p.PuedeHacerDescuentos  = ChkPuedeDescuentos.IsChecked == true;
        p.PuedeCambiarPrecios   = ChkPuedeCambiarPrecios.IsChecked == true;
        p.PuedeVerCostos        = ChkPuedeVerCostos.IsChecked == true;
        p.PuedeModificarStock   = ChkPuedeModifStock.IsChecked == true;
        p.PuedeAbrirCaja        = ChkPuedeAbrirCaja.IsChecked == true;
        p.PuedeCerrarCaja       = ChkPuedeCerrarCaja.IsChecked == true;
        p.EsAdministrador       = ChkEsAdmin.IsChecked == true;
        p.MaximoDescuento       = maxDto;

        try
        {
            if (_perfilEditando is null) await App.Api.CrearPerfil(p);
            else await App.Api.ActualizarPerfil(p);
            MessageBox.Show("Perfil guardado correctamente.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            await CargarPerfiles();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnNuevoUsuario_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UsuarioEditWindow(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) _ = CargarUsuarios();
    }

    private void DgUsuarios_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    { if (DgUsuarios.SelectedItem is Usuario u) AbrirEdicionUsuario(u); }

    private void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
    { if ((sender as Button)?.Tag is Usuario u) AbrirEdicionUsuario(u); }

    private void AbrirEdicionUsuario(Usuario u)
    {
        var dlg = new UsuarioEditWindow(u) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) _ = CargarUsuarios();
    }

    private async void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not Usuario u) return;
        if (u.Id == 1) { MessageBox.Show("No se puede eliminar el admin principal."); return; }
        if (MessageBox.Show($"¿Desactivar usuario \"{u.NombreUsuario}\"?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await App.Api.EliminarUsuario(u.Id); await CargarUsuarios(); }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }
}
