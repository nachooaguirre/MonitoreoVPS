using System.Windows;
using SuperPOS.Client.Views.Clientes;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Proveedores;

public partial class ProveedorEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Proveedor? _original;

    public ProveedorEditWindow(Proveedor? proveedor)
    {
        InitializeComponent();
        _original = proveedor;
        TitleBarCtrl.Title = proveedor is null ? "Nuevo Proveedor" : $"Editar: {proveedor.RazonSocial}";
        if (proveedor is not null) CargarDatos(proveedor);
    }

    private void CargarDatos(Proveedor p)
    {
        TxtRazonSocial.Text = p.RazonSocial;
        TxtCuit.Text = p.Cuit;
        TxtCodigo.Text = p.CodigoProveedor;
        TxtTelefono.Text = p.Telefono;
        TxtCelular.Text = p.Celular;
        TxtEmail.Text = p.Email;
        TxtDireccion.Text = p.Direccion;
        TxtLocalidad.Text = p.Localidad;
        TxtProvincia.Text = p.Provincia;
        TxtDiasEntrega.Text = p.DiasEntrega.ToString();
        TxtDiasPago.Text = p.DiasVencimientoPago.ToString();
        TxtObs.Text = p.Observaciones;
        ChkActivo.IsChecked = p.Activo;
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtRazonSocial.Text))
        { MessageBox.Show("La razón social es obligatoria.", "Validación"); return; }

        int.TryParse(TxtDiasEntrega.Text, out var dias);
        int.TryParse(TxtDiasPago.Text, out var diasPago); if (diasPago <= 0) diasPago = 30;

        var p = _original ?? new Proveedor();
        p.RazonSocial = TxtRazonSocial.Text.Trim();
        p.Cuit = TxtCuit.Text.Trim();
        p.CodigoProveedor = TxtCodigo.Text.Trim().NullIfEmpty();
        p.Telefono = TxtTelefono.Text.Trim().NullIfEmpty();
        p.Celular = TxtCelular.Text.Trim().NullIfEmpty();
        p.Email = TxtEmail.Text.Trim().NullIfEmpty();
        p.Direccion = TxtDireccion.Text.Trim().NullIfEmpty();
        p.Localidad = TxtLocalidad.Text.Trim().NullIfEmpty();
        p.Provincia = TxtProvincia.Text.Trim().NullIfEmpty();
        p.DiasEntrega = dias;
        p.DiasVencimientoPago = diasPago;
        p.Observaciones = TxtObs.Text.Trim().NullIfEmpty();
        p.Activo = ChkActivo.IsChecked == true;

        try
        {
            if (_original is null) await App.Api.CrearProveedor(p);
            else await App.Api.ActualizarProveedor(p);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
