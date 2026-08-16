using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Views.Clientes;
using SuperPOS.Client.Views.Caja;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Proveedores;

public partial class ProveedorEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Proveedor? _original;
    private List<Articulo> _articulosVinculados = [];

    public ProveedorEditWindow(Proveedor? proveedor)
    {
        InitializeComponent();
        _original = proveedor;
        TitleBarCtrl.Title = proveedor is null ? "Nuevo Proveedor" : $"Editar: {proveedor.RazonSocial}";
        if (proveedor is not null)
        {
            CargarDatos(proveedor);
            TabArticulos.IsEnabled = true;
            Loaded += async (_, _) => await CargarArticulosVinculadosAsync();
        }
        else
        {
            TabArticulos.IsEnabled = false;
            TabArticulos.Header = "Artículos (Guardá primero)";
        }
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

    private async System.Threading.Tasks.Task CargarArticulosVinculadosAsync()
    {
        if (_original is null) return;
        try
        {
            _articulosVinculados = await App.Api.ListarArticulosProveedor(_original.Id) ?? [];
            TxtBuscarArticulo.Text = "";
            DgArticulosVinculados.ItemsSource = _articulosVinculados;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar artículos vinculados: {ex.Message}", "Error");
        }
    }

    private void TxtBuscarArticulo_TextChanged(object sender, TextChangedEventArgs e)
    {
        var busq = TxtBuscarArticulo.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(busq))
        {
            DgArticulosVinculados.ItemsSource = _articulosVinculados;
        }
        else
        {
            DgArticulosVinculados.ItemsSource = _articulosVinculados
                .Where(a => a.Descripcion.ToLower().Contains(busq)
                         || a.CodigoBarras.Contains(busq)
                         || a.CodigoInterno.Contains(busq))
                .ToList();
        }
    }

    private async void BtnVincularArticulo_Click(object sender, RoutedEventArgs e)
    {
        if (_original is null) return;
        var dlg = new BuscadorArticulosWindow("") { Owner = this };
        if (dlg.ShowDialog() == true && dlg.ArticuloSeleccionado is Articulo seleccionado)
        {
            try
            {
                seleccionado.IdProveedor = _original.Id;
                await App.Api.ActualizarArticulo(seleccionado);
                await CargarArticulosVinculadosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al vincular el artículo: {ex.Message}", "Error");
            }
        }
    }

    private async void BtnDesvincularArticulo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Articulo art)
        {
            if (MessageBox.Show($"¿Desvincular el artículo \"{art.Descripcion}\" de este proveedor?",
                                "Confirmar Desvinculación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var fallbackId = 1;
                    var provs = await App.Api.GetProveedoresLista();
                    if (provs != null && provs.Any())
                    {
                        var first = provs.FirstOrDefault(p => p.Id != _original?.Id);
                        if (first != null) fallbackId = first.Id;
                    }
                    art.IdProveedor = fallbackId;
                    await App.Api.ActualizarArticulo(art);
                    await CargarArticulosVinculadosAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al desvincular el artículo: {ex.Message}", "Error");
                }
            }
        }
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
