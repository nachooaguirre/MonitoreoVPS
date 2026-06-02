using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock;

public partial class ActualizacionPreciosWindow : Wpf.Ui.Controls.FluentWindow
{
    private bool _loading = true;
    private int _conteoArticulos = 0;

    public ActualizacionPreciosWindow()
    {
        InitializeComponent();
        TitleBarCtrl.Title = "Actualización Masiva de Precios";
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _loading = true;

            var deptos = await App.Api.GetDepartamentos();
            deptos.Insert(0, new Departamento { Id = 0, Nombre = "Todos los Deptos." });
            CmbDepto.ItemsSource = deptos;
            CmbDepto.SelectedIndex = 0;

            CmbFamilia.ItemsSource = new List<Familia> { new Familia { Id = 0, Nombre = "Todas las Familias" } };
            CmbFamilia.SelectedIndex = 0;

            var marcas = await App.Api.GetMarcas();
            marcas.Insert(0, new Marca { Id = 0, Nombre = "Todas las Marcas" });
            CmbMarca.ItemsSource = marcas;
            CmbMarca.SelectedIndex = 0;

            var provs = await App.Api.GetProveedoresLista();
            var provsList = provs?.Where(p => p.Id > 0).ToList() ?? new List<ProveedorSimple>();
            provsList.Insert(0, new ProveedorSimple { Id = 0, RazonSocial = "Todos los Proveedores" });
            CmbProveedor.ItemsSource = provsList;
            CmbProveedor.SelectedIndex = 0;

            _loading = false;
            await ActualizarConteoPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar filtros: {ex.Message}", "Error");
            _loading = false;
        }
    }

    private async void Filtro_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;

        // Si cambió el departamento, actualizar familias correspondientes
        if (sender == CmbDepto)
        {
            _loading = true;
            try
            {
                if (CmbDepto.SelectedValue is int idDepto && idDepto > 0)
                {
                    var familias = await App.Api.GetFamilias(idDepto);
                    familias.Insert(0, new Familia { Id = 0, Nombre = "Todas las Familias" });
                    CmbFamilia.ItemsSource = familias;
                    CmbFamilia.SelectedIndex = 0;
                }
                else
                {
                    CmbFamilia.ItemsSource = new List<Familia> { new Familia { Id = 0, Nombre = "Todas las Familias" } };
                    CmbFamilia.SelectedIndex = 0;
                }
            }
            catch { }
            _loading = false;
        }

        await ActualizarConteoPreview();
    }

    private async Task ActualizarConteoPreview()
    {
        if (_loading) return;

        int? idDepto = (CmbDepto.SelectedValue is int d && d > 0) ? d : null;
        int? idFam = (CmbFamilia.SelectedValue is int f && f > 0) ? f : null;
        int? idMarca = (CmbMarca.SelectedValue is int m && m > 0) ? m : null;
        int? idProv = (CmbProveedor.SelectedValue is int p && p > 0) ? p : null;

        try
        {
            var req = new
            {
                IdDepartamento = idDepto,
                IdFamilia = idFam,
                IdMarca = idMarca,
                IdProveedor = idProv
            };
            
            _conteoArticulos = await App.Api.GetConteoArticulosLote(req);
            TxtPreview.Text = $"Se modificarán {_conteoArticulos} artículos activos.";
        }
        catch
        {
            TxtPreview.Text = "No se pudo obtener la vista previa del conteo.";
            _conteoArticulos = 0;
        }
    }

    private async void BtnAplicar_Click(object sender, RoutedEventArgs e)
    {
        if (_conteoArticulos == 0)
        {
            MessageBox.Show("No hay artículos seleccionados que coincidan con los filtros.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string valorTxt = TxtValor.Text.Replace(',', '.');
        if (!decimal.TryParse(valorTxt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valor) || valor == 0)
        {
            MessageBox.Show("Ingrese un valor de ajuste válido (distinto de cero).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string campo = (CmbCampo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Costo";
        string metodo = (CmbMetodo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Porcentaje";
        string campoDesc = (CmbCampo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Costo";
        string metodoDesc = (CmbMetodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Porcentaje";

        var confirmacion = MessageBox.Show(
            $"¿Desea aplicar el ajuste de {campoDesc} ({metodoDesc}: {valor}) a los {_conteoArticulos} artículos activos que coinciden con los filtros?\n\nEsta operación no se puede deshacer.",
            "Confirmar Ajuste Masivo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmacion != MessageBoxResult.Yes) return;

        int? idDepto = (CmbDepto.SelectedValue is int d && d > 0) ? d : null;
        int? idFam = (CmbFamilia.SelectedValue is int f && f > 0) ? f : null;
        int? idMarca = (CmbMarca.SelectedValue is int m && m > 0) ? m : null;
        int? idProv = (CmbProveedor.SelectedValue is int p && p > 0) ? p : null;

        var req = new
        {
            IdDepartamento = idDepto,
            IdFamilia = idFam,
            IdMarca = idMarca,
            IdProveedor = idProv,
            Campo = campo,
            Metodo = metodo,
            Valor = valor,
            IdUsuario = App.IdUsuarioActual,
            IdSucursal = App.SucursalId
        };

        try
        {
            int modificados = await App.Api.ActualizarPreciosLote(req);
            MessageBox.Show($"Operación finalizada. Se actualizaron los precios de {modificados} artículos.", "Ajuste Masivo", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al aplicar los cambios: {ex.Message}", "Error");
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
