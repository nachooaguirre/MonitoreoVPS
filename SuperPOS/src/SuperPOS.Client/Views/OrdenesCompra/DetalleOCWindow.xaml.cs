using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SuperPOS.Client.Views.Remitos;

namespace SuperPOS.Client.Views.OrdenesCompra;

public partial class DetalleOCWindow : Window
{
    private readonly int _idOC;
    private int _nroOrden;
    private string _proveedorNombre = "";
    private string? _proveedorEmail;
    private decimal _total;
    private string _fechaEmisionTxt = "";
    private string _fechaEntregaTxt = "-";
    private readonly List<FilaImpresionOc> _filasImpresion = [];
    private int? _idOcOriginal;

    public DetalleOCWindow(int idOC)
    {
        _idOC = idOC;
        InitializeComponent();
        Loaded += async (_, _) => await Cargar();
    }

    private async Task Cargar()
    {
        try
        {
            var ocNullable = await App.Api.GetOrdenCompraDetalle(_idOC);
            if (ocNullable is null) { MessageBox.Show("No se encontró la OC."); Close(); return; }

            var oc = ocNullable.Value;

            _filasImpresion.Clear();
            _nroOrden = oc.TryGetProperty("nroOrden", out var nro) ? nro.GetInt32() : oc.TryGetProperty("NroOrden", out var nro2) ? nro2.GetInt32() : 0;
            _proveedorNombre = ReadString(oc, "proveedorNombre", "ProveedorNombre") ?? "";

            var idOriginal = ReadInt(oc, "idOrdenCompraOriginal", "IdOrdenCompraOriginal");
            var motivoDiferencia = ReadString(oc, "motivoDiferencia", "MotivoDiferencia");
            var nroOriginal = ReadInt(oc, "nroOrdenOriginal", "NroOrdenOriginal");

            if (idOriginal > 0)
            {
                BrdOriginalOcAlert.Visibility = Visibility.Visible;
                RunOriginalOcLink.Text = $"OC-{nroOriginal:D6}";
                _idOcOriginal = idOriginal;

                if (!string.IsNullOrEmpty(motivoDiferencia))
                    TxtMotivoAlerta.Text = $" · Motivo: {motivoDiferencia}";
                else
                    TxtMotivoAlerta.Text = "";
            }
            else
            {
                BrdOriginalOcAlert.Visibility = Visibility.Collapsed;
                _idOcOriginal = null;
            }
            _proveedorEmail = ReadString(oc, "proveedorEmail", "ProveedorEmail");
            var observaciones = ReadString(oc, "observaciones", "Observaciones");
            if (!string.IsNullOrEmpty(observaciones))
            {
                BrdObservaciones.Visibility = Visibility.Visible;
                TxtObservaciones.Text = observaciones;
            }
            else
            {
                BrdObservaciones.Visibility = Visibility.Collapsed;
            }
            var estado = ReadInt(oc, "estado", "Estado");
            var total = ReadDecimal(oc, "total", "Total");
            var sinIva = ReadDecimal(oc, "totalSinIva", "TotalSinIva");
            var iva = ReadDecimal(oc, "totalIva", "TotalIva");
            _total = total;

            TxtTitulo.Text = $"Orden de Compra  OC-{_nroOrden:D6}";
            TxtProveedor.Text = _proveedorNombre;
            TxtTotal.Text = total.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"));
            TxtTotalPie.Text = TxtTotal.Text;
            TxtSinIva.Text = sinIva.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"));
            TxtIva.Text = iva.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"));

            _fechaEmisionTxt = "-";
            if (TryGetDate(oc, "fecha", "Fecha", out var fe)) _fechaEmisionTxt = fe.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-AR"));
            TxtFecha.Text = _fechaEmisionTxt;

            _fechaEntregaTxt = "-";
            if (TryGetDate(oc, "fechaEntregaEsperada", "FechaEntregaEsperada", out var fent))
                _fechaEntregaTxt = fent.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-AR"));
            TxtFechaEntrega.Text = _fechaEntregaTxt;

            TxtFechaRecepcion.Text = "-";
            if (TryGetDate(oc, "fechaRecepcion", "FechaRecepcion", out var fr))
                TxtFechaRecepcion.Text = fr.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-AR"));

            (string label, string bg, string fg) = estado switch
            {
                0 => ("PENDIENTE", "#201800", "#C0A020"),
                1 => ("ENVIADA", "#0A1828", "#4090C0"),
                2 => ("RECEP. PARCIAL", "#200A00", "#C06020"),
                3 => ("RECIBIDA", "#0A2010", "#40C060"),
                4 => ("ANULADA", "#200808", "#C04040"),
                5 => ("BORRADOR (IA)", "#1A1028", "#B080E8"),
                6 => ("DEVUELTA AL PROV.", "#2D1000", "#E06020"),
                _ => ("—", "#202020", "#808080")
            };
            TxtEstado.Text = label;
            TxtEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)!);
            BrdEstado.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);

            BtnAnular.IsEnabled = estado != 3 && estado != 4 && estado != 6;
            BtnEditar.IsEnabled = estado == 0 || estado == 5; // Solo PENDIENTE o BORRADOR
            BtnConfirmarBorrador.Visibility = estado == 5 ? Visibility.Visible : Visibility.Collapsed;
            BtnRecibir.Visibility = (estado is >= 0 and <= 2 && estado != 5) ? Visibility.Visible : Visibility.Collapsed;
            BtnRecibir.IsEnabled = estado is >= 0 and <= 2 && estado != 5;

            BtnDevolver.Visibility = (estado is >= 0 and <= 2 && estado != 5) ? Visibility.Visible : Visibility.Collapsed;
            BtnDevolver.IsEnabled = estado is >= 0 and <= 2 && estado != 5;

            bool tieneDiferencias = false;
            if (oc.TryGetProperty("detalles", out var detPropCheck) || oc.TryGetProperty("Detalles", out detPropCheck))
            {
                tieneDiferencias = detPropCheck.EnumerateArray().Any(d =>
                    ReadDecimal(d, "cantidadRecibida", "CantidadRecibida") != ReadDecimal(d, "cantidadPedida", "CantidadPedida")
                );
            }
            BtnOCDiferencias.IsEnabled = (estado == 2 || estado == 3) && tieneDiferencias;

            if (oc.TryGetProperty("detalles", out var detProp) || oc.TryGetProperty("Detalles", out detProp))
            {
                var detalles = detProp.EnumerateArray().Select(d =>
                {
                    var cod = "";
                    var desc = "";
                    if (d.TryGetProperty("articulo", out var a) && a.ValueKind != JsonValueKind.Null)
                    {
                        if (a.TryGetProperty("codigoBarras", out var cb)) cod = cb.GetString() ?? "";
                        else if (a.TryGetProperty("CodigoBarras", out var cb2)) cod = cb2.GetString() ?? "";
                        if (a.TryGetProperty("descripcion", out var ds)) desc = ds.GetString() ?? "";
                        else if (a.TryGetProperty("Descripcion", out var ds2)) desc = ds2.GetString() ?? "";
                    }

                    var cantPed = ReadDecimal(d, "cantidadPedida", "CantidadPedida");
                    var cantRec = ReadDecimal(d, "cantidadRecibida", "CantidadRecibida");
                    var pCosto = ReadDecimal(d, "precioCosto", "PrecioCosto");
                    var alic = ReadDecimal(d, "alicuotaIva", "AlicuotaIva");
                    var sub = ReadDecimal(d, "subtotal", "Subtotal");
                    var obsDif = ReadString(d, "observacionDiferencia", "ObservacionDiferencia");

                    _filasImpresion.Add(new FilaImpresionOc(cod, desc, cantPed, cantRec, pCosto, alic, sub));

                    return new
                    {
                        CodigoBarras = cod,
                        Descripcion = desc,
                        CantidadPedida = cantPed,
                        CantidadRecibida = cantRec,
                        PrecioCosto = pCosto,
                        AlicuotaIva = alic,
                        Subtotal = sub,
                        ObservacionDiferencia = obsDif
                    };
                }).ToList();

                DgDetalle.ItemsSource = detalles;
                TxtCantItems.Text = $"{detalles.Count} artículo(s)";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando OC: {ex.Message}");
        }
    }

    private static string? ReadString(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.String) return a.GetString();
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.String) return b.GetString();
        return null;
    }

    private static int ReadInt(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetInt32(out var i)) return i;
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetInt32(out var j)) return j;
        return 0;
    }

    private static decimal ReadDecimal(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var d)) return d;
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetDecimal(out var d2)) return d2;
        return 0;
    }

    private static bool TryGetDate(JsonElement r, string camel, string pascal, out DateTime local)
    {
        JsonElement? el = null;
        if (r.TryGetProperty(camel, out var a)) el = a;
        else if (r.TryGetProperty(pascal, out var b)) el = b;
        if (el is null || el.Value.ValueKind == JsonValueKind.Null) { local = default; return false; }

        var e = el.Value;
        string? raw = e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText().Trim('"');
        if (string.IsNullOrEmpty(raw)) { local = default; return false; }
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            local = dt.ToLocalTime();
            return true;
        }

        local = default;
        return false;
    }

    private async void BtnRecibir_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new RecibirPedidoWindow(_idOC);
        dlg.ShowDialog();
        await Cargar();
    }

    private async void BtnOCDiferencias_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ocNullable = await App.Api.GetOrdenCompraDetalle(_idOC);
            if (ocNullable == null) return;
            var oc = ocNullable.Value;

            var idProv = ReadInt(oc, "idProveedor", "IdProveedor");
            var provNombre = ReadString(oc, "proveedorNombre", "ProveedorNombre") ?? "";

            var lineasDiferencias = new List<NuevaOCLineaInicial>();
            if (oc.TryGetProperty("detalles", out var detProp) || oc.TryGetProperty("Detalles", out detProp))
            {
                foreach (var d in detProp.EnumerateArray())
                {
                    var cPedida = ReadDecimal(d, "cantidadPedida", "CantidadPedida");
                    var cRecibida = ReadDecimal(d, "cantidadRecibida", "CantidadRecibida");
                    if (cPedida == cRecibida) continue;

                    var delta = Math.Abs(cRecibida - cPedida);
                    if (delta <= 0) continue;

                    var idArt = ReadInt(d, "idArticulo", "IdArticulo");
                    var costo = ReadDecimal(d, "precioCosto", "PrecioCosto");
                    var iva = ReadDecimal(d, "alicuotaIva", "AlicuotaIva");

                    var cod = ""; var desc = ""; var mNombre = "";
                    if (d.TryGetProperty("articulo", out var a) && a.ValueKind != JsonValueKind.Null)
                    {
                        if (a.TryGetProperty("codigoBarras", out var cb)) cod = cb.GetString() ?? "";
                        else if (a.TryGetProperty("CodigoBarras", out var cb2)) cod = cb2.GetString() ?? "";
                        if (a.TryGetProperty("descripcion", out var ds)) desc = ds.GetString() ?? "";
                        else if (a.TryGetProperty("Descripcion", out var ds2)) desc = ds2.GetString() ?? "";
                    }

                    lineasDiferencias.Add(new NuevaOCLineaInicial(idArt, desc, cod, delta, costo, iva, idProv, provNombre, mNombre));
                }
            }

            if (lineasDiferencias.Count == 0)
            {
                MessageBox.Show("No se detectaron diferencias entre lo pedido y lo recibido.", "Sin diferencias", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new NuevaOCWindow(idProv, lineasDiferencias, idOcEdit: null, idOcOrigen: _idOC);
            if (dlg.ShowDialog() == true)
            {
                await Cargar();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar OC por diferencias: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnAnular_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Anular esta Orden de Compra?", "Confirmar", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try
        {
            await App.Api.AnularOrdenCompra(_idOC);
            await Cargar();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        var doc = CrearDocumentoImpresion();
        var win = new Window
        {
            Title = $"Vista previa – OC-{_nroOrden:D6}",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 800,
            Height = 700,
            MinWidth = 500,
            MinHeight = 400,
            Background = Brushes.White
        };
        var root = new DockPanel { LastChildFill = true };
        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8)
        };
        var btnPrint = new Button { Content = "Imprimir…", MinWidth = 110, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var btnCerrar = new Button { Content = "Cerrar", MinWidth = 80, IsCancel = true };
        btnPrint.Click += (_, _) => EjecutarImpresion(doc);
        btnCerrar.Click += (_, _) => win.Close();
        bar.Children.Add(btnPrint);
        bar.Children.Add(btnCerrar);
        var scroll = new FlowDocumentScrollViewer
        {
            Document = doc,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        DockPanel.SetDock(bar, Dock.Bottom);
        root.Children.Add(bar);
        root.Children.Add(scroll);
        win.Content = root;
        win.ShowDialog();
    }

    private FlowDocument CrearDocumentoImpresion()
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(40),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            Background = Brushes.White
        };
        var titulo = new Paragraph(new Run($"Orden de Compra OC-{_nroOrden:D6}"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black
        };
        doc.Blocks.Add(titulo);
        doc.Blocks.Add(new Paragraph(new Run($"Proveedor: {_proveedorNombre}\nEmisión: {_fechaEmisionTxt}\nEntrega esperada: {_fechaEntregaTxt}\nTotal: {_total.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"))}"))
        {
            Foreground = new SolidColorBrush(Color.FromRgb(32, 32, 32))
        });

        var tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
        for (var c = 0; c < 7; c++) tabla.Columns.Add(new TableColumn());
        var header = new TableRowGroup();
        var hr = new TableRow { Background = new SolidColorBrush(Color.FromRgb(220, 220, 220)) };
        hr.Cells.Add(CellTh("EAN"));
        hr.Cells.Add(CellTh("Descripción"));
        hr.Cells.Add(CellTh("Pedido"));
        hr.Cells.Add(CellTh("Recib."));
        hr.Cells.Add(CellTh("P.Unit"));
        hr.Cells.Add(CellTh("IVA%"));
        hr.Cells.Add(CellTh("Subtotal"));
        header.Rows.Add(hr);
        tabla.RowGroups.Add(header);

        var body = new TableRowGroup();
        foreach (var f in _filasImpresion)
        {
            var row = new TableRow();
            row.Cells.Add(CellTd(f.CodigoBarras));
            row.Cells.Add(CellTd(f.Descripcion));
            row.Cells.Add(CellTd(f.CantidadPedida.ToString("N2", CultureInfo.GetCultureInfo("es-AR"))));
            row.Cells.Add(CellTd(f.CantidadRecibida.ToString("N2", CultureInfo.GetCultureInfo("es-AR"))));
            row.Cells.Add(CellTd(f.PrecioCosto.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"))));
            row.Cells.Add(CellTd(f.AlicuotaIva.ToString("N1", CultureInfo.GetCultureInfo("es-AR"))));
            row.Cells.Add(CellTd(f.Subtotal.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"))));
            body.Rows.Add(row);
        }

        tabla.RowGroups.Add(body);
        doc.Blocks.Add(tabla);
        return doc;
    }

    private void EjecutarImpresion(FlowDocument doc)
    {
        PrintDialog pd;
        try
        {
            pd = new PrintDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo abrir el asistente de impresión. " +
                "Comprobá en Windows que haya al menos una impresora o \"Microsoft Imprimir a PDF\" (Configuración > Bluetooth e dispositivos > Impresoras).\n\n" +
                ex.Message,
                "Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool? dlg;
        try
        {
            dlg = pd.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al abrir el diálogo de impresión: " + ex.Message, "Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (dlg != true) return;

        var wPrev = doc.ColumnWidth;
        try
        {
            doc.ColumnWidth = pd.PrintableAreaWidth;
            pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"OC-{_nroOrden:D6}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al imprimir: " + ex.Message, "Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            doc.ColumnWidth = wPrev;
        }
    }

    private void BtnPdf_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Archivos PDF (*.pdf)|*.pdf",
            FileName = $"OC-{_nroOrden:D6}.pdf",
            Title = "Guardar Orden de Compra como PDF"
        };
        
        if (dlg.ShowDialog() != true) return;

        try
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"Orden de Compra OC-{_nroOrden:D6}").SemiBold().FontSize(20);
                        col.Item().Text($"Proveedor: {_proveedorNombre}");
                        col.Item().Text($"Emisión: {_fechaEmisionTxt}");
                        col.Item().Text($"Entrega esperada: {_fechaEntregaTxt}");
                    });

                    page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("EAN").Bold();
                            header.Cell().Text("Descripción").Bold();
                            header.Cell().Text("Pedido").Bold();
                            header.Cell().Text("Recib.").Bold();
                            header.Cell().Text("P.Unit").Bold();
                            header.Cell().Text("IVA%").Bold();
                            header.Cell().Text("Subtotal").Bold();
                        });

                        foreach (var f in _filasImpresion)
                        {
                            table.Cell().Text(f.CodigoBarras);
                            table.Cell().Text(f.Descripcion);
                            table.Cell().Text(f.CantidadPedida.ToString("N2", CultureInfo.GetCultureInfo("es-AR")));
                            table.Cell().Text(f.CantidadRecibida.ToString("N2", CultureInfo.GetCultureInfo("es-AR")));
                            table.Cell().Text(f.PrecioCosto.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR")));
                            table.Cell().Text(f.AlicuotaIva.ToString("N1", CultureInfo.GetCultureInfo("es-AR")));
                            table.Cell().Text(f.Subtotal.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR")));
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Total: ").SemiBold();
                        x.Span(_total.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR")));
                    });
                });
            })
            .GeneratePdf(dlg.FileName);

            MessageBox.Show("PDF descargado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al generar PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnEditar_Click(object sender, RoutedEventArgs e)
    {
        var ocNullable = await App.Api.GetOrdenCompraDetalle(_idOC);
        if (ocNullable == null) return;
        var oc = ocNullable.Value;

        var idProv = ReadInt(oc, "idProveedor", "IdProveedor");
        
        var lineas = new List<NuevaOCLineaInicial>();
        if (oc.TryGetProperty("detalles", out var detProp) || oc.TryGetProperty("Detalles", out detProp))
        {
            foreach (var d in detProp.EnumerateArray())
            {
                var idArt = ReadInt(d, "idArticulo", "IdArticulo");
                var cPedida = ReadDecimal(d, "cantidadPedida", "CantidadPedida");
                var costo = ReadDecimal(d, "precioCosto", "PrecioCosto");
                var iva = ReadDecimal(d, "alicuotaIva", "AlicuotaIva");
                
                var cod = ""; var desc = ""; var mNombre = "";
                if (d.TryGetProperty("articulo", out var a) && a.ValueKind != JsonValueKind.Null)
                {
                    if (a.TryGetProperty("codigoBarras", out var cb)) cod = cb.GetString() ?? "";
                    else if (a.TryGetProperty("CodigoBarras", out var cb2)) cod = cb2.GetString() ?? "";
                    if (a.TryGetProperty("descripcion", out var ds)) desc = ds.GetString() ?? "";
                    else if (a.TryGetProperty("Descripcion", out var ds2)) desc = ds2.GetString() ?? "";
                }
                
                lineas.Add(new NuevaOCLineaInicial(idArt, desc, cod, cPedida, costo, iva, idProv, _proveedorNombre, mNombre));
            }
        }

        var dlg = new NuevaOCWindow(idProv, lineas, _idOC);
        if (dlg.ShowDialog() == true)
        {
            await Cargar();
        }
    }

    private static TableCell CellTh(string text)
    {
        var p = new Paragraph(new Run(text)) { FontWeight = FontWeights.Bold, Foreground = Brushes.Black, Margin = new Thickness(4) };
        return new TableCell(p);
    }

    private static TableCell CellTd(string text)
    {
        var p = new Paragraph(new Run(text)) { Foreground = Brushes.Black, Margin = new Thickness(4) };
        return new TableCell(p);
    }

    private void BtnEmail_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_proveedorEmail))
        {
            MessageBox.Show("Este proveedor no tiene email cargado (datos del proveedor).");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Orden de compra OC-{_nroOrden:D6}");
        sb.AppendLine($"Proveedor: {_proveedorNombre}");
        sb.AppendLine($"Emisión: {_fechaEmisionTxt}");
        sb.AppendLine($"Entrega esperada: {_fechaEntregaTxt}");
        sb.AppendLine($"Total: {_total.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"))}");
        sb.AppendLine();
        foreach (var f in _filasImpresion)
            sb.AppendLine($"{f.CodigoBarras}\t{f.Descripcion}\t{f.CantidadPedida:N2}\t{f.PrecioCosto:$0.00}\t{f.Subtotal:$0.00}");

        var subject = Uri.EscapeDataString($"Orden de compra OC-{_nroOrden:D6}");
        var body = Uri.EscapeDataString(sb.ToString());
        var mailto = $"mailto:{Uri.EscapeDataString(_proveedorEmail)}?subject={subject}&body={body}";

        try
        {
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir el cliente de correo: {ex.Message}");
        }
    }

    private async void BtnExportarTxt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Texto (*.txt)|*.txt",
                FileName = $"OC-{_nroOrden:D6}.txt"
            };
            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine($"OC-{_nroOrden:D6}");
            sb.AppendLine($"Proveedor: {_proveedorNombre}");
            sb.AppendLine($"Emisión: {_fechaEmisionTxt}");
            sb.AppendLine($"Total: {_total.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"))}");
            sb.AppendLine();
            foreach (var f in _filasImpresion)
                sb.AppendLine($"{f.CodigoBarras};{f.Descripcion};{f.CantidadPedida.ToString(CultureInfo.InvariantCulture)};{f.PrecioCosto.ToString(CultureInfo.InvariantCulture)};{f.Subtotal.ToString(CultureInfo.InvariantCulture)}");

            await File.WriteAllTextAsync(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Archivo guardado.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void TxtOriginalOcLink_Click(object sender, RoutedEventArgs e)
    {
        if (_idOcOriginal.HasValue)
        {
            var dlg = new DetalleOCWindow(_idOcOriginal.Value);
            dlg.Owner = this;
            dlg.ShowDialog();
        }
    }

    private async void BtnConfirmarBorrador_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Confirmar y enviar esta Orden de Compra?", "Confirmar Orden", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            await App.Api.EnviarOrdenCompra(_idOC);
            MessageBox.Show("Orden de compra confirmada y enviada con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            await Cargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al confirmar la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnDevolver_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Marcar esta Orden de Compra como devuelta al proveedor? Esto evitará que se reciba la mercadería.", "Confirmar Devolución", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await App.Api.DevolverOrdenCompra(_idOC);
            MessageBox.Show("Orden de compra marcada como devuelta con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            await Cargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al devolver la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();

    private readonly record struct FilaImpresionOc(
        string CodigoBarras,
        string Descripcion,
        decimal CantidadPedida,
        decimal CantidadRecibida,
        decimal PrecioCosto,
        decimal AlicuotaIva,
        decimal Subtotal);
}
