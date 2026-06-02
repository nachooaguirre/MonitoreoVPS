using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Remitos;

/// <summary>Alta manual de remito de entrada (sin OC).</summary>
public sealed class NuevoRemitoWindow : Window
{
    private readonly ObservableCollection<LineaRemitoVm> _lineas = [];
    private readonly ComboBox _cboProv;
    private readonly TextBox _txtBuscar;
    private readonly TextBox _txtCant;
    private readonly TextBox _txtNroExt;
    private readonly TextBox _txtTransp;

    public NuevoRemitoWindow()
    {
        Title = "Nuevo remito de entrada";
        Width = 780;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

        var root = new Grid { Margin = new Thickness(14) };
        for (var i = 0; i < 6; i++) root.RowDefinitions.Add(new RowDefinition { Height = i == 4 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        var hdr = new TextBlock
        {
            Text = "Remito manual — entrada de mercadería",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 180, 255)),
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(hdr, 0);
        root.Children.Add(hdr);

        var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        row1.Children.Add(MkLabel("Proveedor:"));
        _cboProv = new ComboBox { Width = 360, Height = 32, DisplayMemberPath = "RazonSocial", SelectedValuePath = "Id" };
        row1.Children.Add(_cboProv);
        Grid.SetRow(row1, 1);
        root.Children.Add(row1);

        var row2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        row2.Children.Add(MkLabel("N° remito proveedor:"));
        _txtNroExt = Txt(140);
        row2.Children.Add(_txtNroExt);
        row2.Children.Add(MkLabel("  Transporte:"));
        _txtTransp = Txt(200);
        row2.Children.Add(_txtTransp);
        Grid.SetRow(row2, 2);
        root.Children.Add(row2);

        var rowAdd = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        rowAdd.Children.Add(MkLabel("Artículo (EAN o código):"));
        _txtBuscar = Txt(220);
        _txtBuscar.KeyDown += TxtBuscar_KeyDown;
        rowAdd.Children.Add(_txtBuscar);
        rowAdd.Children.Add(MkLabel("  Cant.:"));
        _txtCant = Txt(70);
        _txtCant.Text = "1";
        rowAdd.Children.Add(_txtCant);
        var btnAdd = new Button { Content = "Agregar", Width = 80, Height = 28, Margin = new Thickness(8, 0, 0, 0) };
        btnAdd.Click += (_, _) => _ = AgregarArticuloAsync();
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
        dg.Columns.Add(new DataGridTextColumn { Header = "EAN", Binding = new System.Windows.Data.Binding("CodigoBarras"), Width = 120 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Descripción", Binding = new System.Windows.Data.Binding("Descripcion"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        dg.Columns.Add(new DataGridTextColumn { Header = "Cant.", Binding = new System.Windows.Data.Binding("Cantidad") { StringFormat = "N2" }, Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Costo", Binding = new System.Windows.Data.Binding("PrecioCosto") { StringFormat = "N2" }, Width = 70 });
        Grid.SetRow(dg, 4);
        root.Children.Add(dg);

        var foot = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var btnCancel = new Button { Content = "Cancelar", Width = 100, Height = 36 };
        btnCancel.Click += (_, _) => { DialogResult = false; Close(); };
        var btnOk = new Button { Content = "Guardar remito", Width = 140, Height = 36, Margin = new Thickness(10, 0, 0, 0) };
        btnOk.Click += async (_, _) => await GuardarAsync();
        foot.Children.Add(btnCancel);
        foot.Children.Add(btnOk);
        Grid.SetRow(foot, 5);
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
            var cant = decimal.TryParse(_txtCant.Text, out var c) ? c : 1;
            _lineas.Add(new LineaRemitoVm
            {
                IdArticulo   = art.Id,
                CodigoBarras = art.CodigoBarras ?? "",
                Descripcion  = art.Descripcion,
                Cantidad     = cant,
                PrecioCosto  = art.PrecioCosto
            });
            _txtBuscar.Clear();
            _txtCant.Text = "1";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async Task GuardarAsync()
    {
        if (_cboProv.SelectedItem is not ProveedorSimple prov) { MessageBox.Show("Seleccione un proveedor."); return; }
        if (_lineas.Count == 0) { MessageBox.Show("Agregue al menos un artículo."); return; }

        var remito = new Remito
        {
            Tipo         = TipoRemito.Entrada,
            IdProveedor  = prov.Id,
            IdUsuario    = App.IdUsuarioActual,
            NroRemitoExterno = string.IsNullOrWhiteSpace(_txtNroExt.Text) ? null : _txtNroExt.Text.Trim(),
            Transportista    = string.IsNullOrWhiteSpace(_txtTransp.Text) ? null : _txtTransp.Text.Trim(),
            Detalles = _lineas.Select(l => new RemitoDetalle
            {
                IdArticulo       = l.IdArticulo,
                CantidadRemitida = l.Cantidad,
                CantidadRecibida = 0,
                PrecioCosto      = l.PrecioCosto
            }).ToList()
        };

        try
        {
            await App.Api.CrearRemitoManual(remito);
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { MessageBox.Show($"No se pudo guardar:\n{ex.Message}"); }
    }

    private sealed class LineaRemitoVm
    {
        public int IdArticulo { get; set; }
        public string CodigoBarras { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioCosto { get; set; }
    }
}
