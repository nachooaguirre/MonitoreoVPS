using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SuperPOS.Client.Views.Compras;

namespace SuperPOS.Client.Views.Remitos;

public sealed class DetalleRemitoWindow : Window
{
    private readonly int _idRemito;
    private int _idProveedor;
    private string _proveedorNombre = "";
    private int _estado;
    private readonly Button _btnConciliar;
    private readonly TextBlock _txtHeader = new()
    {
        FontSize = 14,
        Foreground = Brushes.LightGray,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 10)
    };
    private readonly DataGrid _dg;

    public DetalleRemitoWindow(int idRemito)
    {
        _idRemito = idRemito;
        Title = $"Detalle remito #{idRemito}";
        Width = 860;
        Height = 580;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var bg = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        Background = bg;

        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = $"Remito interno #{idRemito}",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 180, 255)),
            Margin = new Thickness(0, 0, 0, 6)
        };
        var headStack = new StackPanel();
        headStack.Children.Add(title);
        headStack.Children.Add(_txtHeader);
        Grid.SetRow(headStack, 0);
        grid.Children.Add(headStack);

        _dg = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            Background = bg,
            Foreground = Brushes.LightGray,
            BorderThickness = new Thickness(0),
            RowBackground = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        _dg.Columns.Add(new DataGridTextColumn { Header = "EAN", Binding = new System.Windows.Data.Binding("CodigoBarras"), Width = 120 });
        _dg.Columns.Add(new DataGridTextColumn { Header = "Descripción", Binding = new System.Windows.Data.Binding("Descripcion"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _dg.Columns.Add(new DataGridTextColumn { Header = "Remitido", Binding = new System.Windows.Data.Binding("CantidadRemitida") { StringFormat = "N3" }, Width = 90 });
        _dg.Columns.Add(new DataGridTextColumn { Header = "Recibido", Binding = new System.Windows.Data.Binding("CantidadRecibida") { StringFormat = "N3" }, Width = 90 });
        _dg.Columns.Add(new DataGridTextColumn { Header = "Costo", Binding = new System.Windows.Data.Binding("PrecioCosto") { StringFormat = "N2" }, Width = 90 });
        _dg.Columns.Add(new DataGridTextColumn { Header = "Lote", Binding = new System.Windows.Data.Binding("LoteNro"), Width = 80 });
        Grid.SetRow(_dg, 1);
        grid.Children.Add(_dg);

        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };

        _btnConciliar = new Button { Content = "Conciliar con Factura", Height = 36, Width = 160, Margin = new Thickness(0, 0, 8, 0), IsEnabled = false };
        _btnConciliar.Click += async (_, _) => await ConciliarAsync();
        footer.Children.Add(_btnConciliar);

        var btnCerrar = new Button { Content = "Cerrar", Height = 36, Width = 120 };
        btnCerrar.Click += (_, _) => Close();
        footer.Children.Add(btnCerrar);

        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        Content = grid;
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            var remito = await App.Api.GetRemitoDetalle(_idRemito);
            if (remito is null)
            {
                MessageBox.Show("Remito no encontrado.");
                return;
            }

            var r = remito.Value;
            var nro = TryGetProp(r, "nroRemito", "NroRemito");
            var fecha = TryGetProp(r, "fecha", "Fecha");
            var est = TryGetProp(r, "estado", "Estado");
            var prov = TryGetString(r, "proveedorNombre", "ProveedorNombre");
            var ext = TryGetString(r, "nroRemitoExterno", "NroRemitoExterno");
            var transp = TryGetString(r, "transportista", "Transportista");

            _idProveedor = (int)TryGetDecimal(r, "idProveedor", "IdProveedor");
            _proveedorNombre = prov ?? "";
            _estado = (int)TryGetDecimal(r, "estado", "Estado");
            _btnConciliar.IsEnabled = _idProveedor > 0 && _estado == 1; // EstadoRemito.Confirmado

            Title = $"Remito n° {JsonElementToDisplay(nro)}";
            _txtHeader.Text = $"{FmtFecha(fecha)} · Estado: {JsonElementToDisplay(est)} · {prov ?? "—"}" +
                (string.IsNullOrEmpty(ext) ? "" : $"\nRemito proveedor: {ext}") +
                (string.IsNullOrEmpty(transp) ? "" : $" · Transporte: {transp}");

            var filas = new List<DetalleRow>();
            if (TryGetPropElement(r, "detalles", "Detalles", out var detArr))
            {
                foreach (var d in detArr.EnumerateArray())
                {
                    filas.Add(new DetalleRow
                    {
                        CodigoBarras = TryGetString(d, "codigoBarras", "CodigoBarras") ?? "",
                        Descripcion = TryGetString(d, "descripcion", "Descripcion") ?? "",
                        CantidadRemitida = TryGetDecimal(d, "cantidadRemitida", "CantidadRemitida"),
                        CantidadRecibida = TryGetDecimal(d, "cantidadRecibida", "CantidadRecibida"),
                        PrecioCosto = TryGetDecimal(d, "precioCosto", "PrecioCosto"),
                        LoteNro = TryGetString(d, "loteNro", "LoteNro") ?? ""
                    });
                }
            }

            _dg.ItemsSource = filas;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private async Task ConciliarAsync()
    {
        var picker = new SeleccionarCompraWindow(_idProveedor) { Owner = this };
        if (picker.ShowDialog() != true || picker.IdCompraSeleccionada is not { } idCompra) return;

        try
        {
            var resultado = await App.Api.ConciliarRemitoConCompra(_idRemito, (int)idCompra);
            var conciliada = resultado.TryGetProperty("conciliada", out var c) && c.GetBoolean();
            if (conciliada)
            {
                MessageBox.Show("✅ Conciliado: la factura coincide con lo recibido. La compra queda habilitada para pago.");
                return;
            }

            var diffs = resultado.GetProperty("diferencias");
            var msg = "⚠ Se encontraron diferencias entre lo facturado y lo recibido:\n\n";
            decimal totalDiferencia = 0;
            foreach (var d in diffs.EnumerateArray())
            {
                var desc = TryGetString(d, "descripcion", "Descripcion") ?? $"Artículo {TryGetDecimal(d, "idArticulo", "IdArticulo")}";
                var monto = TryGetDecimal(d, "montoDiferencia", "MontoDiferencia");
                totalDiferencia += monto;
                var difCant = TryGetDecimal(d, "diferenciaCantidad", "DiferenciaCantidad");
                var difPrecio = TryGetDecimal(d, "diferenciaPrecioUnitario", "DiferenciaPrecioUnitario");
                var detalle = new List<string>();
                if (difCant != 0) detalle.Add($"cant. facturada {TryGetDecimal(d, "cantidadFacturada", "CantidadFacturada")} vs. recibida {TryGetDecimal(d, "cantidadRecibida", "CantidadRecibida")}");
                if (difPrecio != 0) detalle.Add($"precio facturado {TryGetDecimal(d, "precioFacturado", "PrecioFacturado"):C2} vs. pactado {TryGetDecimal(d, "precioPactado", "PrecioPactado"):C2}");
                msg += $"• {desc}: {string.Join(" | ", detalle)} ({monto:C2})\n";
            }
            msg += $"\nDiferencia total: {totalDiferencia:C2}\n\n¿Registrar la Nota de Crédito/Débito del proveedor ahora?";

            if (MessageBox.Show(msg, "Diferencias encontradas", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                var esDebito = totalDiferencia < 0;
                var dlg = new NotaProveedorWindow(_idProveedor, _proveedorNombre, idCompra, Math.Abs(totalDiferencia), esDebito) { Owner = this };
                dlg.ShowDialog();
            }
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private static string FmtFecha(JsonElement? el)
    {
        if (el is null) return "—";
        var e = el.Value;
        string? raw = e.ValueKind switch
        {
            JsonValueKind.String => e.GetString(),
            _ => e.GetRawText().Trim('"')
        };
        if (string.IsNullOrEmpty(raw)) return "—";
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-AR"));
        return raw;
    }

    private static JsonElement? TryGetProp(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a)) return a;
        if (r.TryGetProperty(pascal, out var b)) return b;
        return null;
    }

    private static bool TryGetPropElement(JsonElement r, string camel, string pascal, out JsonElement arr)
    {
        if (r.TryGetProperty(camel, out arr) && arr.ValueKind == JsonValueKind.Array) return true;
        if (r.TryGetProperty(pascal, out arr) && arr.ValueKind == JsonValueKind.Array) return true;
        arr = default;
        return false;
    }

    private static string? TryGetString(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.String) return a.GetString();
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.String) return b.GetString();
        return null;
    }

    private static decimal TryGetDecimal(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var d)) return d;
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetDecimal(out var d2)) return d2;
        return 0;
    }

    private static string JsonElementToDisplay(JsonElement? el)
    {
        if (el is null) return "—";
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.String => e.GetString() ?? "—",
            JsonValueKind.Number => e.TryGetInt64(out var l) ? l.ToString(CultureInfo.InvariantCulture)
                : e.TryGetDecimal(out var d) ? d.ToString(CultureInfo.InvariantCulture) : e.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "—",
            _ => e.GetRawText()
        };
    }

    private sealed class DetalleRow
    {
        public string CodigoBarras { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal CantidadRemitida { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal PrecioCosto { get; set; }
        public string LoteNro { get; set; } = "";
    }
}
