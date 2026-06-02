using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Models;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Inventario;

public partial class InventarioPage : Page
{
    private InventarioResumenDto? _inventarioActivo;
    private List<InventarioDetalleDto> _conteoActual = [];

    public InventarioPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarInventarios();
    }

    private async Task CargarInventarios()
    {
        try
        {
            var inventarios = await App.Api.GetInventarios();
            DgInventarios.ItemsSource = inventarios;

            _inventarioActivo = inventarios?.FirstOrDefault(i => i.Estado == 0);
            ActualizarPanelActivo();
            if (_inventarioActivo != null)
                await SincronizarConteoConApi();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void ActualizarPanelActivo()
    {
        if (_inventarioActivo is null)
        {
            PanelSinInventario.Visibility = Visibility.Visible;
            PanelInventarioActivo.Visibility = Visibility.Collapsed;
        }
        else
        {
            PanelSinInventario.Visibility = Visibility.Collapsed;
            PanelInventarioActivo.Visibility = Visibility.Visible;
            TxtInvDescripcion.Text = _inventarioActivo.Descripcion;
            TxtInvSucursal.Text = string.IsNullOrWhiteSpace(_inventarioActivo.SucursalNombre)
                ? $"Sucursal #{_inventarioActivo.IdSucursal}"
                : $"Sucursal: {_inventarioActivo.SucursalNombre}";
            TxtContados.Text = _inventarioActivo.ArticulosContados.ToString();
            TxtTotalArts.Text = _inventarioActivo.TotalArticulos.ToString() + " artículos";
        }
    }

    private async Task SincronizarConteoConApi()
    {
        if (_inventarioActivo is null) return;
        try
        {
            var inv = await App.Api.GetInventarioById(_inventarioActivo.Id);
            if (inv is null) return;
            _inventarioActivo.ArticulosContados = inv.ArticulosContados;
            _inventarioActivo.TotalArticulos = inv.TotalArticulos;
            TxtContados.Text = inv.ArticulosContados.ToString();
            TxtTotalArts.Text = inv.TotalArticulos + " artículos";

            _conteoActual = (inv.Detalles ?? [])
                .Where(d => d.FueConteado)
                .OrderByDescending(d => d.FechaConteo)
                .Select(d => new InventarioDetalleDto
                {
                    Id = d.Id,
                    IdInventario = d.IdInventario,
                    IdArticulo = d.IdArticulo,
                    CodigoBarras = d.Articulo?.CodigoBarras,
                    Descripcion = d.Articulo?.Descripcion ?? "",
                    StockSistema = d.StockSistema,
                    StockContado = d.StockContado,
                    FueConteado = d.FueConteado,
                    PrecioCosto = d.PrecioCosto
                })
                .ToList();
            DgConteo.ItemsSource = null;
            DgConteo.ItemsSource = _conteoActual;
        }
        catch
        {
            // Si falla el detalle, el panel sigue con datos de lista
        }
    }

    private async void BtnNuevo_Click(object sender, RoutedEventArgs e)
    {
        if (_inventarioActivo != null)
        {
            MessageBox.Show("Ya hay un inventario en proceso. Ciérrelo antes de crear uno nuevo.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new NuevoInventarioWindow { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true || dlg.IdSucursalElegida is not int idSuc) return;

        try
        {
            await App.Api.CrearInventario(new
            {
                Descripcion = dlg.DescripcionIngresada,
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                IdSucursal = idSuc
            });
            await CargarInventarios();
            TabInventario.SelectedIndex = 1;
            TxtEan.Focus();
            MessageBox.Show("Inventario iniciado. Puede comenzar la toma de datos en la sucursal elegida.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void TxtEan_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        await RegistrarConteo();
    }

    private async void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        => await RegistrarConteo();

    private async Task RegistrarConteo()
    {
        if (_inventarioActivo is null) { MessageBox.Show("No hay inventario activo.", "Inventario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var codigo = (TxtEan.Text ?? "").Trim();
        if (string.IsNullOrEmpty(codigo)) return;

        if (!decimal.TryParse((TxtConteo.Text ?? "1").Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cant))
            cant = 1;
        if (cant < 0) { MessageBox.Show("La cantidad no puede ser negativa.", "Validación", MessageBoxButton.OK, MessageBoxImage.Information); return; }

        try
        {
            var art = await App.Api.BuscarArticuloPorCodigo(codigo);
            if (art is null) { MessageBox.Show($"No se encontró un artículo con código: {codigo}"); TxtEan.SelectAll(); TxtEan.Focus(); return; }

            var acum = ChkAcumulativo.IsChecked == true;
            await App.Api.ContarInventario(_inventarioActivo.Id, art.Id, cant, acumulativo: acum, observaciones: null);

            await SincronizarConteoConApi();
            TxtEan.Clear();
            TxtConteo.Text = "1";
            TxtEan.Focus();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnDiferencias_Click(object sender, RoutedEventArgs e)
    {
        if (_inventarioActivo is null) return;
        new InventarioDiferenciasWindow(_inventarioActivo.Id, $"Diferencias — {_inventarioActivo.Descripcion}")
        { Owner = Window.GetWindow(this) }
            .ShowDialog();
    }

    private async void BtnCerrarInventario_Click(object sender, RoutedEventArgs e)
    {
        if (_inventarioActivo is null) return;
        await SincronizarConteoConApi();

        var res = MessageBox.Show(
            "¿Cómo desea cerrar el inventario?\n\n" +
            "· Sí: aplicar al stock (solo artículos con conteo registrado; el resto no se modifica)\n" +
            "· No: cerrar sin ajustar stock\n" +
            "· Cancelar: volver",
            "Cerrar inventario",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);

        if (res == MessageBoxResult.Cancel) return;
        bool aplicar = res == MessageBoxResult.Yes;

        try
        {
            await App.Api.CerrarInventario(_inventarioActivo.Id, aplicar);
            await CargarInventarios();
            _conteoActual.Clear();
            DgConteo.ItemsSource = null;
            MessageBox.Show(
                aplicar
                    ? "Inventario cerrado y se aplicaron los ajustes al stock (según conteos registrados)."
                    : "Inventario cerrado sin modificar el stock.",
                "Inventario", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void DgInventarios_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgInventarios.SelectedItem is not InventarioResumenDto row) return;
        if (row.Estado == 0)
        {
            TabInventario.SelectedIndex = 1;
            TxtEan.Focus();
        }
        else
        {
            new InventarioDetalleViewWindow(row.Id) { Owner = Window.GetWindow(this) }.ShowDialog();
        }
    }
}
