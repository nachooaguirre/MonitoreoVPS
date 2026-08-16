using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.OrdenesCompra;

public record NuevaOCLineaInicial(
    int IdArticulo, string Descripcion, string CodigoBarras, decimal CantidadPedida, decimal PrecioCosto, decimal AlicuotaIva, int IdProveedor, string ProveedorNombre, string MarcaNombre);

public partial class NuevaOCWindow : Window
{
    private readonly ObservableCollection<OcLineItem> _items = [];
    private readonly ObservableCollection<OcProveedorInfo> _proveedores = [];
    private List<Articulo> _catalogoCompleto = [];
    private readonly int? _iaProveedor;
    private readonly IReadOnlyList<NuevaOCLineaInicial>? _iaLineas;
    private readonly int? _idOcEdit;
    private readonly int? _idOcOrigen;

    public NuevaOCWindow() : this(null, null, null, null) { }

    public NuevaOCWindow(int? idProveedor, IReadOnlyList<NuevaOCLineaInicial>? lineasIa, int? idOcEdit = null, int? idOcOrigen = null)
    {
        _iaProveedor = idProveedor;
        _iaLineas = lineasIa;
        _idOcEdit = idOcEdit;
        _idOcOrigen = idOcOrigen;
        InitializeComponent();
        DgDetalle.ItemsSource = _items;
        DgProveedores.ItemsSource = _proveedores;
        _items.CollectionChanged += (_, _) => { ActualizarTotal(); ActualizarProveedores(); };
        
        if (_idOcEdit.HasValue)
            Title = $"Editar Orden de Compra (OC-{_idOcEdit.Value:D6})";
        else if (_idOcOrigen.HasValue)
            Title = $"Nueva Orden de Compra por Diferencias (Origen: OC-{_idOcOrigen.Value:D6})";
        else
            Title = _iaLineas is { Count: > 0 } ? "Nueva Orden de Compra (desde IA)" : "Nueva Orden de Compra";
        
        Loaded += OnLoadedNueva;
    }

    private async void OnLoadedNueva(object sender, RoutedEventArgs e)
    {
        try
        {
            var proveedores = await App.Api.GetProveedoresLista();
            if (proveedores != null)
            {
                var lista = proveedores.ToList();
                lista.Insert(0, new ProveedorSimple { Id = 0, RazonSocial = "(Todos los proveedores)" });
                CboProveedor.DisplayMemberPath = "RazonSocial";
                CboProveedor.SelectedValuePath = "Id";
                CboProveedor.ItemsSource = lista;
                CboProveedor.SelectedIndex = 0;
            }

            if (_iaProveedor.HasValue && _iaProveedor.Value > 0)
            {
                CboProveedor.SelectedValue = _iaProveedor.Value;
                await RecargarCatalogoAsync();
            }

            if (_idOcOrigen.HasValue)
            {
                BrdDiferencias.Visibility = Visibility.Visible;
                CboMotivo.SelectedIndex = 0;
            }

            if (_iaLineas is { Count: > 0 })
            {
                foreach (var l in _iaLineas)
                {
                    var line = new OcLineItem(ActualizarTotal);
                    line.InitializeFromIa(l);
                    _items.Add(line);
                }
                DgDetalle.Items.Refresh();
                ActualizarTotal();
                ActualizarProveedores();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CboProveedor_Changed(object sender, SelectionChangedEventArgs e)
    {
        TxtFiltroCatalogo.Clear();
        await RecargarCatalogoAsync();
    }

    private void TxtFiltroCatalogo_TextChanged(object sender, TextChangedEventArgs e)
        => AplicarFiltroCatalogo();

    private async Task RecargarCatalogoAsync()
    {
        LstCatalogo.ItemsSource = null;
        _catalogoCompleto = [];
        int? idProv = null;
        if (CboProveedor.SelectedItem is ProveedorSimple prov && prov.Id > 0)
            idProv = prov.Id;

        // Si no hay proveedor seleccionado, no cargamos el catálogo completo porque puede ser gigante.
        // Solo cargamos si hay un proveedor o si el usuario busca explícitamente.
        if (idProv == null)
        {
            TxtCatalogoHint.Visibility = Visibility.Visible;
            TxtCatalogoHint.Text = "Elegí un proveedor para ver su catálogo, o usá la búsqueda global arriba.";
            LstCatalogo.Visibility = Visibility.Collapsed;
            return;
        }

        TxtCatalogoHint.Visibility = Visibility.Collapsed;
        LstCatalogo.Visibility = Visibility.Visible;
        try
        {
            _catalogoCompleto = await App.Api.ListarArticulosProveedor(idProv.Value, buscar: null, pageSize: 500);
            if (_catalogoCompleto.Count == 0)
                MessageBox.Show("No hay artículos con este proveedor asignado.", "Sin artículos", MessageBoxButton.OK, MessageBoxImage.Information);
            AplicarFiltroCatalogo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar catálogo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AplicarFiltroCatalogo()
    {
        var filtro = TxtFiltroCatalogo.Text.Trim();
        if (_catalogoCompleto.Count == 0)
        {
            LstCatalogo.ItemsSource = null;
            return;
        }

        if (string.IsNullOrEmpty(filtro))
        {
            LstCatalogo.ItemsSource = _catalogoCompleto;
            return;
        }

        LstCatalogo.ItemsSource = _catalogoCompleto
            .Where(a =>
                a.Descripcion.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || a.CodigoBarras.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || a.CodigoInterno.Contains(filtro, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async void TxtBuscarArt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        await BuscarEnCatalogoAsync();
    }

    private async void BtnAgregar_Click(object sender, RoutedEventArgs e)
        => await BuscarEnCatalogoAsync();

    private async Task BuscarEnCatalogoAsync()
    {
        var text = TxtBuscarArt.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        int? idProv = null;
        if (CboProveedor.SelectedItem is ProveedorSimple prov && prov.Id > 0)
            idProv = prov.Id;

        try
        {
            var (_, encontrados) = await App.Api.GetArticulos(buscar: text, idProveedor: idProv, page: 1, pageSize: 200);
            if (encontrados.Count == 0)
            {
                MessageBox.Show("No hay artículos que coincidan con la búsqueda.");
                return;
            }

            _catalogoCompleto = encontrados.ToList();
            TxtFiltroCatalogo.Clear();
            AplicarFiltroCatalogo();
            TxtCatalogoHint.Visibility = Visibility.Collapsed;
            LstCatalogo.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void LstCatalogo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstCatalogo.SelectedItem is Articulo art)
            AgregarDesdeArticulo(art);
    }

    private void BtnAgregarSeleccion_Click(object sender, RoutedEventArgs e)
    {
        if (LstCatalogo.SelectedItem is Articulo art)
            AgregarDesdeArticulo(art);
        else
            MessageBox.Show("Seleccione un artículo en el catálogo.");
    }

    private async void AgregarDesdeArticulo(Articulo art)
    {
        var cant = decimal.TryParse(TxtCant.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var c) ? c : 1m;
        var costoDef = art.PrecioCosto;
        var costo = decimal.TryParse(TxtCosto.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var co) ? co : costoDef;

        var existente = _items.FirstOrDefault(x => x.IdArticulo == art.Id);
        if (existente != null)
        {
            existente.CantidadPedida += cant;
            DgDetalle.Items.Refresh();
            TxtBuscarArt.Clear();
            return;
        }

        // Si el artículo viene sin marca/proveedor desde el DTO reducido, los traemos
        if (art.Proveedor == null && art.IdProveedor > 0)
        {
            var provDb = await App.Api.GetProveedor(art.IdProveedor);
            art.Proveedor = provDb;
        }

        _items.Add(OcLineItem.FromArticulo(ActualizarTotal, art, cant, costo));

        TxtBuscarArt.Clear();
        TxtCant.Text = "1";
        TxtCosto.Clear();
    }

    private void BtnQuitar_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is OcLineItem item)
        {
            _items.Remove(item);
            ActualizarProveedores();
        }
    }

    private void ActualizarTotal()
    {
        var total = _items.Sum(i => i.Subtotal);
        TxtTotal.Text = total.ToString("C2", CultureInfo.CurrentCulture);
        var uds = _items.Sum(i => i.CantidadPedida);
        if (_items.Count == 0)
        {
            TxtResumenLineas.Text = "0 artículos (sin líneas)";
            return;
        }
        var n = _items.Count == 1 ? "1 línea" : $"{_items.Count} líneas";
        TxtResumenLineas.Text = $"{n} · {FormatQty(uds)} uds. · {total.ToString("C2", CultureInfo.CurrentCulture)}";
    }

    private void ActualizarProveedores()
    {
        var provsEnGrid = _items.Select(i => i.IdProveedor).Distinct().ToList();
        var toRemove = _proveedores.Where(p => !provsEnGrid.Contains(p.IdProveedor)).ToList();
        foreach (var r in toRemove) _proveedores.Remove(r);

        foreach (var pId in provsEnGrid)
        {
            if (!_proveedores.Any(p => p.IdProveedor == pId))
            {
                var refItem = _items.First(i => i.IdProveedor == pId);
                var pInfo = new OcProveedorInfo { IdProveedor = pId, RazonSocial = refItem.ProveedorNombre };
                _proveedores.Add(pInfo);
                
                // Cargar DiasEntrega real de forma asincrónica sin bloquear
                _ = CargarDiasEntregaAsync(pInfo);
            }
        }
    }

    private async Task CargarDiasEntregaAsync(OcProveedorInfo pInfo)
    {
        var p = await App.Api.GetProveedor(pInfo.IdProveedor);
        if (p != null)
        {
            pInfo.DiasEntrega = p.DiasEntrega;
            if (p.DiasEntrega > 0)
                pInfo.FechaEsperada = DateTime.Today.AddDays(p.DiasEntrega);
        }
    }

    private static string FormatQty(decimal q)
        => q == Math.Truncate(q) ? ((int)q).ToString(CultureInfo.CurrentCulture) : q.ToString("0.##", CultureInfo.CurrentCulture);

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Agregue al menos un artículo.");
            return;
        }

        try
        {
            foreach (var pInfo in _proveedores)
            {
                // Actualizar frecuencia en el proveedor si hace falta
                var prov = await App.Api.GetProveedor(pInfo.IdProveedor);
                if (prov != null && prov.DiasEntrega != pInfo.DiasEntrega)
                {
                    prov.DiasEntrega = pInfo.DiasEntrega;
                    await App.Api.ActualizarProveedor(prov);
                }

                var itemsProv = _items.Where(i => i.IdProveedor == pInfo.IdProveedor).ToList();
                if (itemsProv.Count == 0) continue;

                var oc = new OrdenCompra
                {
                    IdProveedor = pInfo.IdProveedor,
                    IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                    FechaEntregaEsperada = pInfo.FechaEsperada?.ToUniversalTime(),
                    Estado = (_iaLineas is { Count: > 0 } || _idOcOrigen.HasValue) ? EstadoOrdenCompra.Borrador : EstadoOrdenCompra.Pendiente,
                    IdOrdenCompraOriginal = _idOcOrigen,
                    MotivoDiferencia = _idOcOrigen.HasValue ? CboMotivo.Text : null,
                    Observaciones = _idOcOrigen.HasValue ? TxtObservaciones.Text : null,
                    Detalles = itemsProv.Select(i => new OrdenCompraDetalle
                    {
                        IdArticulo = i.IdArticulo,
                        CantidadPedida = i.CantidadPedida,
                        PrecioCosto = i.PrecioCosto,
                        AlicuotaIva = i.AlicuotaIva,
                        Subtotal = i.Subtotal,
                        CantidadRecibida = 0
                    }).ToList()
                };

                if (_idOcEdit.HasValue && _proveedores.Count == 1)
                {
                    await App.Api.ActualizarOrdenCompra(_idOcEdit.Value, oc);
                    MessageBox.Show("Orden de compra actualizada correctamente.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                    return;
                }
                else
                {
                    await App.Api.CrearOrdenCompra(oc);
                }
            }
            
            MessageBox.Show($"Se generaron {_proveedores.Count} orden(es) de compra correctamente.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void TxtNumeric_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void TxtNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9\.\,]+$");
    }

    private void BtnMinusCant_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(TxtCant.Text, out var val))
        {
            TxtCant.Text = Math.Max(1m, val - 1m).ToString("G");
        }
        else
        {
            TxtCant.Text = "1";
        }
    }

    private void BtnPlusCant_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(TxtCant.Text, out var val))
        {
            TxtCant.Text = (val + 1m).ToString("G");
        }
        else
        {
            TxtCant.Text = "1";
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void DgDetalle_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.EditingElement is TextBox tb)
        {
            tb.Dispatcher.BeginInvoke(new Action(() => { tb.SelectAll(); tb.Focus(); }));
        }
    }

    private void DgDetalle_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is not OcLineItem) e.Cancel = true;
    }

    private void DgDetalle_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row?.Item is not OcLineItem line) return;
        if (e.EditingElement is not TextBox tb) return;

        var header = e.Column.Header?.ToString() ?? string.Empty;
        var text = (tb.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text) || !decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var parsed))
        {
            e.Cancel = true;
            return;
        }

        if (header == "Cantidad") { if (parsed > 0) line.CantidadPedida = parsed; else e.Cancel = true; }
        else if (header == "Costo unit.") { if (parsed >= 0) line.PrecioCosto = parsed; else e.Cancel = true; }
        else if (header == "IVA %") { if (parsed >= 0) line.AlicuotaIva = parsed; else e.Cancel = true; }
    }

    public class OcProveedorInfo : INotifyPropertyChanged
    {
        public int IdProveedor { get; set; }
        public string RazonSocial { get; set; } = "";
        
        private int _diasEntrega;
        public int DiasEntrega { get => _diasEntrega; set { _diasEntrega = value; OnPropertyChanged(); } }
        
        private DateTime? _fechaEsperada = DateTime.Today;
        public DateTime? FechaEsperada { get => _fechaEsperada; set { _fechaEsperada = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class OcLineItem : INotifyPropertyChanged
    {
        private readonly Action _onTotals;

        public OcLineItem(Action onTotals) => _onTotals = onTotals;

        public int IdArticulo { get; set; }
        public string Descripcion { get; set; } = "";
        public string CodigoBarras { get; set; } = "";
        public int IdProveedor { get; set; }
        public string ProveedorNombre { get; set; } = "";
        public int IdMarca { get; set; }
        public string MarcaNombre { get; set; } = "";

        private decimal _cantidadPedida;
        public decimal CantidadPedida
        {
            get => _cantidadPedida;
            set
            {
                var v = Math.Round(value <= 0 ? 0.0001m : value, 4, MidpointRounding.AwayFromZero);
                if (_cantidadPedida == v) return;
                _cantidadPedida = v;
                OnPropertyChanged();
                RecomputeSubtotal();
            }
        }

        private decimal _precioCosto;
        public decimal PrecioCosto
        {
            get => _precioCosto;
            set
            {
                var v = Math.Round(value < 0 ? 0 : value, 4, MidpointRounding.AwayFromZero);
                if (_precioCosto == v) return;
                _precioCosto = v;
                OnPropertyChanged();
                RecomputeSubtotal();
            }
        }

        private decimal _alicuotaIva;
        public decimal AlicuotaIva
        {
            get => _alicuotaIva;
            set
            {
                var v = Math.Round(value < 0 ? 0 : value, 2, MidpointRounding.AwayFromZero);
                if (_alicuotaIva == v) return;
                _alicuotaIva = v;
                OnPropertyChanged();
            }
        }

        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal;
            private set { if (_subtotal != value) { _subtotal = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void InitializeFromIa(NuevaOCLineaInicial l)
        {
            IdArticulo = l.IdArticulo;
            Descripcion = l.Descripcion;
            CodigoBarras = l.CodigoBarras;
            IdProveedor = l.IdProveedor;
            ProveedorNombre = string.IsNullOrEmpty(l.ProveedorNombre) ? "Sin proveedor" : l.ProveedorNombre;
            MarcaNombre = string.IsNullOrEmpty(l.MarcaNombre) ? "" : l.MarcaNombre;
            _cantidadPedida = l.CantidadPedida <= 0 ? 0.0001m : l.CantidadPedida;
            _precioCosto = l.PrecioCosto < 0 ? 0 : l.PrecioCosto;
            _alicuotaIva = l.AlicuotaIva;
            RecomputeSubtotal();
        }

        public static OcLineItem FromArticulo(Action onTotals, Articulo art, decimal cant, decimal costo)
        {
            var o = new OcLineItem(onTotals)
            {
                IdArticulo = art.Id,
                Descripcion = art.Descripcion,
                CodigoBarras = art.CodigoBarras,
                IdProveedor = art.IdProveedor,
                ProveedorNombre = art.Proveedor?.RazonSocial ?? $"Prov #{art.IdProveedor}",
                IdMarca = art.IdMarca,
                MarcaNombre = art.Marca?.Nombre ?? ""
            };
            o._alicuotaIva = art.AlicuotaIva;
            o._precioCosto = costo < 0 ? 0 : Math.Round(costo, 4, MidpointRounding.AwayFromZero);
            o._cantidadPedida = cant <= 0 ? 0.0001m : Math.Round(cant, 4, MidpointRounding.AwayFromZero);
            o.RecomputeSubtotal();
            return o;
        }

        private void RecomputeSubtotal()
        {
            Subtotal = Math.Round(_cantidadPedida * _precioCosto, 2, MidpointRounding.AwayFromZero);
            _onTotals();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
