using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Views.Clientes;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Usuarios;

public partial class UsuarioEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Usuario? _original;

    public UsuarioEditWindow(Usuario? usuario)
    {
        InitializeComponent();
        _original = usuario;
        TitleBarCtrl.Title = usuario is null ? "Nuevo Usuario" : $"Editar: {usuario.NombreUsuario}";
        if (usuario is not null)
        {
            TxtNombreUsuario.Text = usuario.NombreUsuario;
            TxtNombreUsuario.IsEnabled = false; // No se puede cambiar el username
            TxtNombreCompleto.Text = usuario.NombreCompleto;
            TxtEmail.Text = usuario.Email;
            TxtTelefono.Text = usuario.Telefono;
            ChkActivo.IsChecked = usuario.Activo;
            ChkAccesoZebra.IsChecked = usuario.AccesoZebra;
            TxtLabelPass.Text = "Nueva contraseña (dejar vacío para no cambiar)";
        }
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var perfiles = await App.Api.GetPerfiles();
        CmbPerfil.ItemsSource = perfiles;
        if (_original is not null)
            CmbPerfil.SelectedValue = _original.IdPerfil;
        else
            CmbPerfil.SelectedIndex = 0;
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombreCompleto.Text))
        { MessageBox.Show("El nombre completo es obligatorio."); return; }
        if (CmbPerfil.SelectedValue is not int idPerfil)
        { MessageBox.Show("Seleccione un perfil."); return; }

        var pass = TxtPassword.Password;
        var confirm = TxtPasswordConfirm.Password;

        if (_original is null && string.IsNullOrWhiteSpace(pass))
        { MessageBox.Show("La contraseña es obligatoria para nuevos usuarios."); return; }
        if (!string.IsNullOrWhiteSpace(pass) && pass.Length < 4)
        { MessageBox.Show("La contraseña debe tener al menos 4 caracteres."); return; }
        if (!string.IsNullOrWhiteSpace(pass) && pass != confirm)
        { MessageBox.Show("Las contraseñas no coinciden."); return; }

        try
        {
            if (_original is null)
            {
                if (string.IsNullOrWhiteSpace(TxtNombreUsuario.Text))
                { MessageBox.Show("El nombre de usuario es obligatorio."); return; }

                await App.Api.CrearUsuario(
                    TxtNombreUsuario.Text.Trim(),
                    TxtNombreCompleto.Text.Trim(),
                    pass, idPerfil,
                    TxtEmail.Text.Trim().NullIfEmpty(),
                    TxtTelefono.Text.Trim().NullIfEmpty(),
                    ChkAccesoZebra.IsChecked == true);
            }
            else
            {
                await App.Api.ActualizarUsuario(
                    _original.Id,
                    TxtNombreCompleto.Text.Trim(),
                    idPerfil,
                    ChkActivo.IsChecked == true,
                    string.IsNullOrWhiteSpace(pass) ? null : pass,
                    TxtEmail.Text.Trim().NullIfEmpty(),
                    TxtTelefono.Text.Trim().NullIfEmpty(),
                    ChkAccesoZebra.IsChecked == true);
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            var msg = ex.Message.Contains("ya existe") ? "El nombre de usuario ya está en uso." : $"Error: {ex.Message}";
            MessageBox.Show(msg);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
