using System.Windows;
using System.Windows.Controls;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Clientes;

public partial class ClienteEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Cliente? _original;

    public ClienteEditWindow(Cliente? cliente)
    {
        InitializeComponent();
        _original = cliente;
        TitleBarCtrl.Title = cliente is null ? "Nuevo Cliente" : $"Editar: {cliente.RazonSocial}";
        if (cliente is not null) CargarDatos(cliente);
    }

    private void CargarDatos(Cliente c)
    {
        TxtRazonSocial.Text = c.RazonSocial;
        TxtCuit.Text = c.Cuit;
        TxtTelefono.Text = c.Telefono;
        TxtCelular.Text = c.Celular;
        TxtEmail.Text = c.Email;
        TxtDireccion.Text = c.Direccion;
        TxtLocalidad.Text = c.Localidad;
        TxtProvincia.Text = c.Provincia;
        TxtCP.Text = c.CodigoPostal;
        ChkCtaCte.IsChecked = c.TieneCtaCte;
        TxtLimite.Text = c.LimiteCredito.ToString("N2");
        TxtDiasVto.Text = c.DiasVencimientoCtaCte.ToString();
        TxtDescuento.Text = c.PorcentajeDescuento.ToString("N2");
        ChkActivo.IsChecked = c.Activo;
        SetCondIva(c.CondicionIva);
    }

    private void SetCondIva(int cond)
    {
        foreach (ComboBoxItem item in CmbCondIva.Items)
            if (item.Tag?.ToString() == cond.ToString()) { CmbCondIva.SelectedItem = item; return; }
    }

    private int GetCondIva()
    {
        if (CmbCondIva.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out var v)) return v;
        return 5;
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtRazonSocial.Text))
        { MessageBox.Show("La razón social es obligatoria.", "Validación"); return; }

        decimal.TryParse(TxtLimite.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var limite);
        int.TryParse(TxtDiasVto.Text, out var diasVto); if (diasVto <= 0) diasVto = 30;
        decimal.TryParse(TxtDescuento.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var descuento);

        var c = _original ?? new Cliente();
        c.RazonSocial = TxtRazonSocial.Text.Trim();
        c.Cuit = TxtCuit.Text.Trim();
        c.CondicionIva = GetCondIva();
        c.Telefono = TxtTelefono.Text.Trim().NullIfEmpty();
        c.Celular = TxtCelular.Text.Trim().NullIfEmpty();
        c.Email = TxtEmail.Text.Trim().NullIfEmpty();
        c.Direccion = TxtDireccion.Text.Trim().NullIfEmpty();
        c.Localidad = TxtLocalidad.Text.Trim().NullIfEmpty();
        c.Provincia = TxtProvincia.Text.Trim().NullIfEmpty();
        c.CodigoPostal = TxtCP.Text.Trim().NullIfEmpty();
        c.TieneCtaCte = ChkCtaCte.IsChecked == true;
        c.LimiteCredito = limite;
        c.DiasVencimientoCtaCte = diasVto;
        c.PorcentajeDescuento = descuento;
        c.Activo = ChkActivo.IsChecked == true;

        try
        {
            if (_original is null) await App.Api.CrearCliente(c);
            else await App.Api.ActualizarCliente(c);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { MessageBox.Show($"Error al guardar: {ex.Message}", "Error"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
