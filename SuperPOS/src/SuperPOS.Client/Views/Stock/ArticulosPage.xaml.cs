using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock;

public partial class ArticulosPage : Page
{
    private int _page = 1;
    private const int PageSize = 100;
    private int _total;

    public ArticulosPage() { InitializeComponent(); Loaded += OnLoaded; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await CargarFiltros();
        await CargarDatos();
    }

    private bool _cargandoFiltros;

    private async Task CargarFiltros()
    {
        _cargandoFiltros = true;
        try
        {
            var deptos = await App.Api.GetDepartamentos();
            deptos.Insert(0, new Departamento { Id = 0, Nombre = "Todos los deptos." });
            CmbDepto.ItemsSource = deptos;
            CmbDepto.SelectedIndex = 0;

            CmbFamilia.ItemsSource = new List<Familia> { new() { Id = 0, Nombre = "Todas las familias" } };
            CmbFamilia.SelectedIndex = 0;
        }
        finally
        {
            _cargandoFiltros = false;
        }
    }

    private async Task CargarDatos()
    {
        DgArticulos.ItemsSource = null;
        var buscar = TxtBuscar.Text.Trim();
        int? idDepto = (CmbDepto.SelectedValue is int d && d > 0) ? d : null;
        int? idFam = (CmbFamilia.SelectedValue is int f && f > 0) ? f : null;
        var inclInact = ChkIncluirInactivos.IsChecked == true;
        try
        {
            var (total, items) = await App.Api.GetArticulos(
                string.IsNullOrEmpty(buscar) ? null : buscar,
                idDepto,
                idProveedor: null,
                _page,
                PageSize,
                idFamilia: idFam,
                incluirInactivos: inclInact);

            _total = total;
            DgArticulos.ItemsSource = items;
            TxtTotal.Text = $"{total} artículo(s) encontrado(s)";
            TxtPagina.Text = $"Página {_page} de {Math.Max(1, (int)Math.Ceiling((double)total / PageSize))}";
            BtnPrev.IsEnabled = _page > 1;
            BtnNext.IsEnabled = _page * PageSize < total;
        }
        catch (Exception ex)
        {
            TxtTotal.Text = "Error al cargar (¿API encendida?)";
            TxtPagina.Text = "";
            MessageBox.Show($"No se pudo cargar el listado: {ex.Message}", "Artículos", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Filtro_DeptoOrFamilia_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargandoFiltros) return;
        if (CmbDepto.SelectedValue is int idDepto && idDepto > 0 && sender == CmbDepto)
        {
            var familias = await App.Api.GetFamilias(idDepto);
            familias.Insert(0, new Familia { Id = 0, Nombre = "Todas las familias" });
            CmbFamilia.ItemsSource = familias;
            CmbFamilia.SelectedIndex = 0;
        }
        _page = 1;
        await CargarDatos();
    }

    private async void ChkIncluirInactivos_Changed(object sender, RoutedEventArgs e)
    {
        _page = 1;
        await CargarDatos();
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) { _page = 1; await CargarDatos(); } }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    { _page = 1; await CargarDatos(); }

    private void BtnNuevo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ArticuloEditWindow(null);
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() == true)
            _ = CargarDatos();
    }

    private void BtnActualizacionMasiva_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ActualizacionPreciosWindow();
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() == true)
            _ = CargarDatos();
    }

    private void DgArticulos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgArticulos.SelectedItem is Articulo art) AbrirEdicion(art);
    }

    private void BtnEditar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Articulo art) AbrirEdicion(art);
    }

    private void AbrirEdicion(Articulo art)
    {
        var dlg = new ArticuloEditWindow(art);
        dlg.Owner = Window.GetWindow(this);
        if (dlg.ShowDialog() == true) _ = CargarDatos();
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not Articulo art) return;
        if (MessageBox.Show($"¿Eliminar \"{art.Descripcion}\"?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            await App.Api.EliminarArticulo(art.Id);
            await CargarDatos();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error"); }
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e) { _page--; await CargarDatos(); }
    private async void BtnNext_Click(object sender, RoutedEventArgs e) { _page++; await CargarDatos(); }
}
