using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using SuperPOS.Client.Services;
using SuperPOS.Client.Views.OrdenesCompra;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Compras;

public partial class ListasPrecioProveedorPage : Page
{
    private string? _rutaElegida;
    private int? _idListaSeleccionada;
    private ListaPrecioProveedor? _detalle;
    private List<ProveedorSimple> _proveedores = [];

    public ListasPrecioProveedorPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await CargarProveedoresYListasAsync();
    }

    private async System.Threading.Tasks.Task CargarProveedoresYListasAsync()
    {
        try
        {
            _proveedores = await App.Api.GetProveedoresLista() ?? [];
            CboProveedorImport.ItemsSource = _proveedores;
            if (_proveedores.Count > 0) CboProveedorImport.SelectedIndex = 0;

            var conTodos = new List<ProveedorSimple> { new() { Id = 0, RazonSocial = "(Todos los proveedores)" } };
            conTodos.AddRange(_proveedores);
            CboFiltroProveedor.ItemsSource = conTodos;
            CboFiltroProveedor.SelectedIndex = 0;
            CboFiltroProveedor.DisplayMemberPath = "RazonSocial";
            CboFiltroProveedor.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        await RefrescarListasAsync();
    }

    private async System.Threading.Tasks.Task RefrescarListasAsync()
    {
        int? f = CboFiltroProveedor.SelectedValue is int idF && idF > 0 ? idF : null;
        var listas = await App.Api.GetListasPrecioProveedor(f);
        DgListas.ItemsSource = listas ?? [];
        if (_idListaSeleccionada.HasValue && listas?.Any(x => x.Id == _idListaSeleccionada) == true)
        {
            DgListas.SelectedItem = listas?.FirstOrDefault(x => x.Id == _idListaSeleccionada);
        }
    }

    private void BtnExaminar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Hojas, PDF e imágenes|*.xlsx;*.xlsm;*.csv;*.txt;*.pdf;*.png;*.jpg;*.jpeg;*.jfif;*.jpe;*.webp;*.gif;*.bmp;*.tif;*.tiff;*.heic;*.heif;*.avif|Excel 97-2003|*.xls|Todos|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            _rutaElegida = dlg.FileName;
            TxtRutaArchivo.Text = dlg.FileName;
            if (string.IsNullOrWhiteSpace(TxtNombreLista.Text))
                TxtNombreLista.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
        }
    }

    private async void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        if (CboProveedorImport.SelectedValue is not int idProv || idProv <= 0)
        {
            MessageBox.Show("Elegí un proveedor.");
            return;
        }

        var pegado = (TxtPegadoLista.Text ?? "").Trim();
        if (string.IsNullOrEmpty(pegado))
        {
            if (string.IsNullOrEmpty(_rutaElegida) || !File.Exists(_rutaElegida))
            {
                MessageBox.Show("Elegí un archivo o pegá el texto de la lista (p. ej. un mensaje de WhatsApp) en el cuadro de abajo.", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        var nombre = (TxtNombreLista.Text ?? "").Trim();
        if (string.IsNullOrEmpty(nombre))
        {
            if (!string.IsNullOrEmpty(_rutaElegida)) nombre = Path.GetFileNameWithoutExtension(_rutaElegida);
            else if (!string.IsNullOrEmpty(pegado)) nombre = "Pegado manual";
        }

        BtnImportar.IsEnabled = false;
        TxtEstadoImport.Text = "Subiendo e interpretando con IA…";
        try
        {
            var r = string.IsNullOrEmpty(pegado)
                ? await App.Api.ImportarListaPrecioProveedor(idProv, nombre, _rutaElegida, null)
                : await App.Api.ImportarListaPrecioProveedor(idProv, nombre, null, pegado);
            if (!r.Exito)
            {
                MessageBox.Show(r.Error ?? "Error al importar", "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEstadoImport.Text = "";
                return;
            }

            TxtEstadoImport.Text = $"Listo. Lista #{r.IdNueva} — {r.LineasCreadas} línea(s).";
            if (r.IdNueva.HasValue) _idListaSeleccionada = r.IdNueva;
            TxtPegadoLista.Text = "";
            await RefrescarListasAsync();
            if (r.IdNueva.HasValue)
            {
                DgListas.SelectedItem = (DgListas.ItemsSource as List<ListaPrecioProveedorResumenDto>)?
                    .FirstOrDefault(x => x.Id == r.IdNueva);
            }

            TxtRecomIa.Text = "";
            BordeIa.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnImportar.IsEnabled = true;
        }
    }

    private async void CboFiltroProveedor_SelectionChanged(object sender, SelectionChangedEventArgs e) => await RefrescarListasAsync();

    private async void BtnRefrescar_Click(object sender, RoutedEventArgs e) => await RefrescarListasAsync();

    private async void DgListas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgListas.SelectedItem is not ListaPrecioProveedorResumenDto row)
        {
            _idListaSeleccionada = null;
            _detalle = null;
            OcultarDetalle();
            return;
        }

        _idListaSeleccionada = row.Id;
        try
        {
            _detalle = await App.Api.GetListaPrecioProveedor(row.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_detalle is null) return;

        var prov = _detalle.Proveedor?.RazonSocial
                   ?? _proveedores.FirstOrDefault(p => p.Id == _detalle.IdProveedor)?.RazonSocial
                   ?? $"Id {_detalle.IdProveedor}";
        TxtDetalleTitulo.Text = $"{_detalle.Nombre}  ·  {prov}";
        if (!string.IsNullOrEmpty(_detalle.Notas))
            TxtDetalleTitulo.Text += $"\nNotas: {_detalle.Notas}";

        PanelAcciones.Visibility = Visibility.Visible;
        DgLineas.Visibility = Visibility.Visible;
        TxtHintLineas.Visibility = Visibility.Visible;
        DgLineas.ItemsSource = _detalle.Lineas.ToList();
        TxtRecomIa.Text = "";
        BordeIa.Visibility = Visibility.Collapsed;
    }

    private void OcultarDetalle()
    {
        TxtDetalleTitulo.Text = "Seleccioná una lista a la izquierda.";
        PanelAcciones.Visibility = Visibility.Collapsed;
        DgLineas.Visibility = Visibility.Collapsed;
        TxtHintLineas.Visibility = Visibility.Collapsed;
        DgLineas.ItemsSource = null;
        BordeIa.Visibility = Visibility.Collapsed;
    }

    private void DgLineas_BeginningEdit(object _, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is not ListaPrecioProveedorLinea) e.Cancel = true;
    }

    private async void DgLineas_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not ListaPrecioProveedorLinea line) return;
        if (e.EditingElement is not TextBox tb) return;

        var header = e.Column.Header?.ToString() ?? "";
        var t = (tb.Text ?? "").Trim();
        var culture = CultureInfo.CurrentCulture;
        try
        {
            switch (header)
            {
                case "Cód. prov.": line.CodigoProveedor = t; break;
                case "Descripción": line.Descripcion = t; break;
                case "P. unit.":
                    if (!decimal.TryParse(t, NumberStyles.Any, culture, out var p))
                    {
                        e.Cancel = true;
                        return;
                    }
                    line.PrecioUnitario = Math.Round(p, 4, MidpointRounding.AwayFromZero);
                    break;
                case "IVA %":
                    if (string.IsNullOrEmpty(t)) line.IvaPorcentaje = null;
                    else if (decimal.TryParse(t, NumberStyles.Any, culture, out var iva)) line.IvaPorcentaje = iva;
                    else e.Cancel = true;
                    return;
                case "Id art.":
                    if (string.IsNullOrEmpty(t)) line.IdArticulo = null;
                    else if (int.TryParse(t, NumberStyles.Integer, culture, out var idA)) line.IdArticulo = idA;
                    else e.Cancel = true;
                    return;
                case "Bonif. (JSON)": line.BonificacionesJson = string.IsNullOrEmpty(t) ? "[]" : t; break;
                default: return;
            }
        }
        catch
        {
            e.Cancel = true;
            return;
        }

        var dto = new ListaLineaUpdateDto
        {
            CodigoProveedor = line.CodigoProveedor,
            Descripcion = line.Descripcion,
            PrecioUnitario = line.PrecioUnitario,
            IvaPorcentaje = line.IvaPorcentaje,
            BonificacionesJson = line.BonificacionesJson,
            IdArticulo = line.IdArticulo
        };
        try
        {
            var ok = await App.Api.UpdateListaProveedorLinea(line.Id, dto);
            if (!ok) MessageBox.Show("No se pudo guardar la línea en el servidor.", "Guardar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnMatchear_Click(object sender, RoutedEventArgs e)
    {
        if (!_idListaSeleccionada.HasValue) return;
        try
        {
            var r = await App.Api.MatchearListaProveedor(_idListaSeleccionada.Value);
            var v = 0;
            var tot = 0;
            if (r is { } j)
            {
                if (j.TryGetProperty("vinculados", out var vj)) v = vj.GetInt32();
                if (j.TryGetProperty("total", out var tj)) tot = tj.GetInt32();
            }
            MessageBox.Show(
                $"Vinculados: {v} de {tot} líneas. La importación ya usó al proveedor elegido; " +
                "«Matchear» vincula cada fila a un artículo de stock. Se prueba: cód. de proveedor, EAN, cód. interno y descripción (normalizada). " +
                "Si el artículo en depósito no tenía aún a ese proveedor, se reintenta por EAN o cód. único en todo el catálogo.",
                "Matchear", MessageBoxButton.OK, MessageBoxImage.Information);
            _detalle = await App.Api.GetListaPrecioProveedor(_idListaSeleccionada.Value);
            if (_detalle != null) DgLineas.ItemsSource = _detalle.Lineas.ToList();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Matchear", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void BtnRecomendarIa_Click(object sender, RoutedEventArgs e)
    {
        if (!_idListaSeleccionada.HasValue) return;
        if (!int.TryParse((TxtDiasProy.Text ?? "10").Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var dias) || dias < 1) dias = 10;
        TxtDiasProy.Text = dias.ToString(CultureInfo.CurrentCulture);
        TxtRecomIa.Text = "Consultando a la IA…";
        BordeIa.Visibility = Visibility.Visible;
        try
        {
            var inst = (TxtInstruccionIa.Text ?? "").Trim();
            var r = await App.Api.AiRecomendarListaProveedor(_idListaSeleccionada.Value, dias, string.IsNullOrEmpty(inst) ? null : inst);
            if (r is null) { TxtRecomIa.Text = "Sin respuesta. Verificá la API y la clave DeepSeek."; return; }
            TxtRecomIa.Text = r.Exito ? r.Texto : (r.Error ?? "Error") + (string.IsNullOrEmpty(r.Texto) ? "" : $"\n\n{r.Texto}");
        }
        catch (Exception ex) { TxtRecomIa.Text = ex.Message; }
    }

    private void BtnNuevaOc_Click(object sender, RoutedEventArgs e)
    {
        if (_detalle is null) return;
        var provName = _detalle.Proveedor?.RazonSocial ?? _proveedores.FirstOrDefault(p => p.Id == _detalle.IdProveedor)?.RazonSocial ?? $"Id {_detalle.IdProveedor}";
        var lineas = _detalle.Lineas.Where(l => l.IdArticulo.HasValue)
            .Select(l => new NuevaOCLineaInicial(
                l.IdArticulo!.Value,
                l.Descripcion,
                l.Articulo?.CodigoBarras ?? "",
                1m,
                l.PrecioUnitario,
                l.IvaPorcentaje ?? l.Articulo?.AlicuotaIva ?? 21m,
                _detalle.IdProveedor,
                provName,
                l.Articulo?.Marca?.Nombre ?? ""))
            .ToList();
        if (lineas.Count == 0)
        {
            MessageBox.Show("Ninguna línea tiene artículo vinculado. Usá «Matchear códigos» o indicá el Id de artículo en la grilla.",
                "Nueva OC", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var w = new NuevaOCWindow(_detalle.IdProveedor, lineas);
        if (Window.GetWindow(this) is Window o) w.Owner = o;
        w.ShowDialog();
    }

    private async void BtnBorrarLista_Click(object sender, RoutedEventArgs e)
    {
        if (!_idListaSeleccionada.HasValue) return;
        if (MessageBox.Show("¿Dar de baja esta lista? (queda inactiva en el sistema.)", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await App.Api.EliminarListaPrecioProveedor(_idListaSeleccionada.Value);
            _idListaSeleccionada = null;
            _detalle = null;
            OcultarDetalle();
            await RefrescarListasAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Borrar", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
