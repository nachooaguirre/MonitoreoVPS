using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Compras;

/// <summary>Alta manual de una factura de proveedor (Compra), con opción de adjuntar el PDF/foto original.</summary>
public sealed class NuevaCompraWindow : Window
{
    private readonly ObservableCollection<LineaCompraVm> _lineas = [];
    private readonly ComboBox _cboProv;
    private readonly ComboBox _cboTipo;
    private readonly TextBox _txtNumero;
    private readonly DatePicker _dpFecha;
    private readonly TextBox _txtBuscar;
    private readonly TextBox _txtCant;
    private readonly TextBox _txtPrecio;
    private readonly TextBox _txtBonif;
    private readonly TextBox _txtIva;
    private readonly TextBlock _txtArchivo;
    private readonly TextBlock _txtTotal;
    private string? _rutaArchivo;

    private static readonly (int Id, string Letra, string Nombre)[] TiposFactura =
    [
        (1, "A", "Factura A"),
        (2, "B", "Factura B"),
        (3, "C", "Factura C")
    ];

    public NuevaCompraWindow()
    {
        Title = "Nueva factura de proveedor";
        Width = 860;
        Height = 620;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

        var root = new Grid { Margin = new Thickness(14) };
        for (var i = 0; i < 7; i++) root.RowDefinitions.Add(new RowDefinition { Height = i == 5 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        var hdr = new TextBlock
        {
            Text = "Factura de proveedor",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 180, 255)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(hdr, 0);
        root.Children.Add(hdr);

        var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        row1.Children.Add(MkLabel("Proveedor:"));
        _cboProv = new ComboBox { Width = 300, Height = 32, DisplayMemberPath = "RazonSocial", SelectedValuePath = "Id" };
        row1.Children.Add(_cboProv);
        row1.Children.Add(MkLabel("  Fecha:"));
        _dpFecha = new DatePicker { Width = 130, Height = 32, SelectedDate = DateTime.Today };
        row1.Children.Add(_dpFecha);
        Grid.SetRow(row1, 1);
        root.Children.Add(row1);

        var row2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        row2.Children.Add(MkLabel("Tipo:"));
        _cboTipo = new ComboBox { Width = 140, Height = 32, DisplayMemberPath = "Nombre", SelectedValuePath = "Id", ItemsSource = TiposFactura, SelectedIndex = 1 };
        row2.Children.Add(_cboTipo);
        row2.Children.Add(MkLabel("  N° factura:"));
        _txtNumero = Txt(140);
        row2.Children.Add(_txtNumero);
        var btnAdjuntar = new Button { Content = "📎 Adjuntar factura (PDF/foto)", Height = 30, Margin = new Thickness(16, 0, 0, 0) };
        btnAdjuntar.Click += BtnAdjuntar_Click;
        row2.Children.Add(btnAdjuntar);
        _txtArchivo = new TextBlock { Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Text = "(sin adjuntar)" };
        row2.Children.Add(_txtArchivo);
        Grid.SetRow(row2, 2);
        root.Children.Add(row2);

        var rowAdd = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
        rowAdd.Children.Add(MkLabel("Artículo (EAN o código):"));
        _txtBuscar = Txt(200);
        _txtBuscar.KeyDown += TxtBuscar_KeyDown;
        rowAdd.Children.Add(_txtBuscar);
        rowAdd.Children.Add(MkLabel("  Cant.:"));
        _txtCant = Txt(60);
        _txtCant.Text = "1";
        rowAdd.Children.Add(_txtCant);
        rowAdd.Children.Add(MkLabel("  Precio:"));
        _txtPrecio = Txt(80);
        rowAdd.Children.Add(_txtPrecio);
        rowAdd.Children.Add(MkLabel("  Bonif.%:"));
        _txtBonif = Txt(60);
        _txtBonif.Text = "0";
        rowAdd.Children.Add(_txtBonif);
        rowAdd.Children.Add(MkLabel("  IVA%:"));
        _txtIva = Txt(60);
        _txtIva.Text = "21";
        rowAdd.Children.Add(_txtIva);
        var btnAdd = new Button { Content = "Agregar", Width = 80, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
        btnAdd.Click += (_, _) => AgregarLinea();
        rowAdd.Children.Add(btnAdd);
        Grid.SetRow(rowAdd, 3);
        root.Children.Add(rowAdd);

        var dg = new DataGrid
        {
            ItemsSource = _lineas,
            AutoGenerateColumns = false,
            IsReadOnly = true,
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            Foreground = Brushes.LightGray,
            RowBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            BorderThickness = new Thickness(0),
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        dg.Columns.Add(new DataGridTextColumn { Header = "EAN", Binding = new System.Windows.Data.Binding("CodigoBarras"), Width = 110 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Descripción", Binding = new System.Windows.Data.Binding("Descripcion"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        dg.Columns.Add(new DataGridTextColumn { Header = "Cant.", Binding = new System.Windows.Data.Binding("Cantidad") { StringFormat = "N2" }, Width = 60 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Precio", Binding = new System.Windows.Data.Binding("PrecioCosto") { StringFormat = "N2" }, Width = 80 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Bonif.%", Binding = new System.Windows.Data.Binding("Bonificacion") { StringFormat = "N2" }, Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "IVA%", Binding = new System.Windows.Data.Binding("AlicuotaIva") { StringFormat = "N2" }, Width = 60 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Subtotal", Binding = new System.Windows.Data.Binding("SubtotalEstimado") { StringFormat = "N2" }, Width = 90 });
        Grid.SetRow(dg, 4);
        root.Children.Add(dg);
        _lineas.CollectionChanged += (_, _) => ActualizarTotal();

        _txtTotal = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 200, 120)),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0),
            Text = "Total: $0,00"
        };
        Grid.SetRow(_txtTotal, 5);
        root.Children.Add(_txtTotal);

        var foot = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var btnCancel = new Button { Content = "Cancelar", Width = 100, Height = 36 };
        btnCancel.Click += (_, _) => { DialogResult = false; Close(); };
        var btnOk = new Button { Content = "Guardar factura", Width = 150, Height = 36, Margin = new Thickness(10, 0, 0, 0) };
        btnOk.Click += async (_, _) => await GuardarAsync();
        foot.Children.Add(btnCancel);
        foot.Children.Add(btnOk);
        Grid.SetRow(foot, 6);
        root.Children.Add(foot);

        Content = root;

        Loaded += async (_, _) =>
        {
            try
            {
                var provs = await App.Api.GetProveedoresLista();
                _cboProv.ItemsSource = provs;
            }
            catch (Exception ex) { MessageBox.Show($"Proveedores: {ex.Message}"); }
        };
    }

    private static TextBlock MkLabel(string text) =>
        new() { Text = text, Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };

    private static TextBox Txt(double w) =>
        new() { Width = w, Height = 28, Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58)) };

    private void BtnAdjuntar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "PDF e imágenes|*.pdf;*.jpg;*.jpeg;*.png;*.webp|Todos|*.*" };
        if (dlg.ShowDialog() != true) return;
        _rutaArchivo = dlg.FileName;
        _txtArchivo.Text = Path.GetFileName(dlg.FileName);
        _txtArchivo.Foreground = new SolidColorBrush(Color.FromRgb(96, 200, 120));
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        await AgregarArticuloAsync();
    }

    private async Task AgregarArticuloAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtBuscar.Text)) return;
        try
        {
            var arts = await App.Api.GetArticulos(_txtBuscar.Text.Trim());
            if (arts?.Count == 0) { MessageBox.Show("No se encontró el artículo."); return; }
            var art = arts![0];
            _txtPrecio.Text = art.PrecioCosto.ToString("0.00", CultureInfo.InvariantCulture);
            AgregarLinea(art.Id, art.CodigoBarras ?? "", art.Descripcion);
            _txtBuscar.Clear();
            _txtCant.Text = "1";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private void AgregarLinea(int? idArticulo = null, string codigoBarras = "", string descripcion = "")
    {
        if (idArticulo is null) return; // el alta manual siempre parte de un artículo encontrado por búsqueda
        var cant = ParseDecimal(_txtCant.Text, 1);
        var precio = ParseDecimal(_txtPrecio.Text, 0);
        var bonif = ParseDecimal(_txtBonif.Text, 0);
        var iva = ParseDecimal(_txtIva.Text, 21);
        _lineas.Add(new LineaCompraVm
        {
            IdArticulo = idArticulo.Value,
            CodigoBarras = codigoBarras,
            Descripcion = descripcion,
            Cantidad = cant,
            PrecioCosto = precio,
            Bonificacion = bonif,
            AlicuotaIva = iva
        });
        ActualizarTotal();
    }

    private static decimal ParseDecimal(string s, decimal fallback) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            || decimal.TryParse(s, out v) ? v : fallback;

    private void ActualizarTotal()
    {
        var total = _lineas.Sum(l => l.SubtotalEstimado);
        _txtTotal.Text = $"Total: {total:C2}";
    }

    private async Task GuardarAsync()
    {
        if (_cboProv.SelectedItem is not ProveedorSimple prov) { MessageBox.Show("Seleccioná un proveedor."); return; }
        if (string.IsNullOrWhiteSpace(_txtNumero.Text)) { MessageBox.Show("Ingresá el número de factura."); return; }
        if (_lineas.Count == 0) { MessageBox.Show("Agregá al menos un artículo."); return; }
        var tipo = ((int Id, string Letra, string Nombre))_cboTipo.SelectedItem!;

        var compra = new Compra
        {
            IdProveedor = prov.Id,
            IdUsuario = App.IdUsuarioActual,
            Fecha = _dpFecha.SelectedDate ?? DateTime.Today,
            NumeroFactura = _txtNumero.Text.Trim(),
            LetraFactura = tipo.Letra,
            IdTipoComprobante = tipo.Id,
            Detalles = _lineas.Select(l => new CompraDetalle
            {
                IdArticulo = l.IdArticulo,
                Cantidad = l.Cantidad,
                PrecioCosto = l.PrecioCosto,
                Bonificacion = l.Bonificacion,
                AlicuotaIva = l.AlicuotaIva
            }).ToList()
        };

        try
        {
            var creada = await App.Api.CrearCompra(compra);
            if (!string.IsNullOrEmpty(_rutaArchivo))
                await App.Api.SubirFacturaArchivo(creada.Id, _rutaArchivo);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { MessageBox.Show($"No se pudo guardar:\n{ex.Message}"); }
    }

    private sealed class LineaCompraVm
    {
        public int IdArticulo { get; set; }
        public string CodigoBarras { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioCosto { get; set; }
        public decimal Bonificacion { get; set; }
        public decimal AlicuotaIva { get; set; } = 21;
        public decimal SubtotalEstimado => Cantidad * PrecioCosto * (1 - Bonificacion / 100) * (1 + AlicuotaIva / 100);
    }
}
