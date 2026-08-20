using System.Windows;

namespace SuperPOS.Client.Views.Sucursales;

public partial class CajaEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly int _idSucursal;
    private readonly SuperPOS.Shared.Entities.Ventas.Caja? _existente;

    public CajaEditWindow(int idSucursal, SuperPOS.Shared.Entities.Ventas.Caja? caja)
    {
        InitializeComponent();
        _idSucursal = idSucursal;
        _existente = caja;

        if (caja is not null)
        {
            TxtNombre.Text = caja.Nombre;
            ChkActivo.IsChecked = caja.Activo;
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            MessageBox.Show("El nombre es obligatorio.", "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var c = new SuperPOS.Shared.Entities.Ventas.Caja
        {
            Id = _existente?.Id ?? 0,
            IdSucursal = _idSucursal,
            Nombre = TxtNombre.Text.Trim(),
            Activo = ChkActivo.IsChecked == true
        };

        try
        {
            if (_existente is null) await App.Api.CrearCaja(c);
            else await App.Api.ActualizarCaja(c);
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
