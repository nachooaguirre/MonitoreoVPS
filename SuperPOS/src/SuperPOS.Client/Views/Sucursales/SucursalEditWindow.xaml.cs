using System.Windows;
using SuperPOS.Client.Models;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Sucursales;

public partial class SucursalEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Sucursal? _existente;

    public SucursalEditWindow(SucursalAdminDto? sucursal)
    {
        InitializeComponent();

        if (sucursal is not null)
        {
            _existente = new Sucursal { Id = sucursal.Id, Nombre = sucursal.Nombre, Direccion = sucursal.Direccion, EsCentral = sucursal.EsCentral, Activo = sucursal.Activo };
            TxtNombre.Text = sucursal.Nombre;
            TxtDireccion.Text = sucursal.Direccion ?? "";
            ChkEsCentral.IsChecked = sucursal.EsCentral;
            ChkActivo.IsChecked = sucursal.Activo;
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            MessageBox.Show("El nombre es obligatorio.", "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var s = new Sucursal
        {
            Id = _existente?.Id ?? 0,
            Nombre = TxtNombre.Text.Trim(),
            Direccion = string.IsNullOrWhiteSpace(TxtDireccion.Text) ? null : TxtDireccion.Text.Trim(),
            EsCentral = ChkEsCentral.IsChecked == true,
            Activo = ChkActivo.IsChecked == true
        };

        try
        {
            if (_existente is null) await App.Api.CrearSucursal(s);
            else await App.Api.ActualizarSucursal(s);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
