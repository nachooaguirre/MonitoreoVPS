using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Precios
{
    public partial class EtiquetasGondolaPage : Page
    {
        private List<EtiquetaColaItemDto> _cola = [];

        public EtiquetasGondolaPage()
        {
            InitializeComponent();
            Loaded += async (_, _) => await CargarColaAsync();
        }

        private async Task CargarColaAsync()
        {
            try
            {
                var apiItems = await App.Api.GetEtiquetasCola();
                if (apiItems == null || apiItems.Count == 0)
                {
                    _cola = [];
                    DgCola.ItemsSource = null;
                    DgCola.Visibility = Visibility.Collapsed;
                    PanelColaVacia.Visibility = Visibility.Visible;
                }
                else
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _cola = apiItems
                        .Select(x => System.Text.Json.JsonSerializer.Deserialize<EtiquetaColaItemDto>(x.GetRawText(), options)!)
                        .ToList();
                    DgCola.ItemsSource = _cola;
                    PanelColaVacia.Visibility = Visibility.Collapsed;
                    DgCola.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la cola de etiquetas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TxtBuscarArticulo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await EncolarManualAsync();
            }
        }

        private async void BtnEncolarManual_Click(object sender, RoutedEventArgs e)
        {
            await EncolarManualAsync();
        }

        private async Task EncolarManualAsync()
        {
            var texto = TxtBuscarArticulo.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            int.TryParse(TxtCantEtiquetas.Text, out int qty);
            if (qty <= 0) qty = 1;

            try
            {
                // Buscar por código o descripción para ver qué artículo es
                var (total, items) = await App.Api.GetArticulos(buscar: texto, pageSize: 1);
                if (total == 0)
                {
                    MessageBox.Show("Artículo no encontrado.", "Búsqueda", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var art = items.First();
                var res = await App.Api.EncolarEtiqueta(art.Id, null, qty);
                if (res != null)
                {
                    TxtBuscarArticulo.Clear();
                    TxtCantEtiquetas.Text = "1";
                    await CargarColaAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al encolar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminarCola_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EtiquetaColaItemDto item)
            {
                try
                {
                    var res = await App.Api.EliminarEtiquetaCola(item.Id);
                    if (res)
                    {
                        await CargarColaAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnLimpiarCola_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Seguro que deseás vaciar por completo la cola de etiquetas?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var res = await App.Api.LimpiarEtiquetasCola();
                    if (res)
                    {
                        await CargarColaAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al vaciar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void TxtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is EtiquetaColaItemDto item)
            {
                if (int.TryParse(tb.Text, out int val) && val > 0 && val != item.Cantidad)
                {
                    item.Cantidad = val;
                    // Opcional: Actualizar en la API (en este caso lo guardamos localmente y se usa en la impresión)
                }
            }
        }

        private async void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (_cola.Count == 0)
            {
                MessageBox.Show("La cola está vacía.", "Imprimir", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var formatStr = (CmbFormato.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                    bool isGondola = formatStr.Contains("Góndola");
                    bool isChica = formatStr.Contains("Chica");

                    // Formato física aproximado en Px (96 DPI)
                    // Góndola: 60x30mm -> 227x113
                    // Chica: 50x30mm -> 189x113
                    // Grande: 80x40mm -> 302x151
                    double width = isGondola ? 227 : (isChica ? 189 : 302);
                    double height = (isGondola || isChica) ? 113 : 151;

                    var doc = new FixedDocument();

                    foreach (var item in _cola)
                    {
                        for (int i = 0; i < item.Cantidad; i++)
                        {
                            var page = new FixedPage { Width = width, Height = height };
                            
                            // Crear el control visual de la etiqueta
                            var labelVisual = new VisualLabelElement(item.Articulo, width, height, isChica);
                            page.Children.Add(labelVisual);

                            var pageContent = new PageContent();
                            ((System.Windows.Markup.IAddChild)pageContent).AddChild(page);
                            doc.Pages.Add(pageContent);
                        }
                    }

                    // Imprimir documento completo
                    dialog.PrintDocument(doc.DocumentPaginator, "Etiquetas de Góndola");

                    // Eliminar las impresas de la API
                    var ids = _cola.Select(x => x.Id).ToList();
                    await App.Api.MarcarEtiquetasImpresas(ids);

                    MessageBox.Show("Impresión finalizada. Cola limpiada en servidor.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CargarColaAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // ===== ELEMENTO VISUAL VECTORIAL PARA CADA ETIQUETA =====
    public class VisualLabelElement : UserControl
    {
        private readonly ArticuloDto _articulo;
        private readonly double _width;
        private readonly double _height;
        private readonly bool _isChica;

        public VisualLabelElement(ArticuloDto articulo, double width, double height, bool isChica)
        {
            _articulo = articulo;
            _width = width;
            _height = height;
            _isChica = isChica;

            Width = width;
            Height = height;
            Background = Brushes.White;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Borde externo
            dc.DrawRectangle(null, new Pen(Brushes.Black, 1.5), new Rect(1, 1, _width - 2, _height - 2));

            // El contenido completo (precio sin impuesto + precio por kilo/litro) se usa tanto en
            // Góndola (60x30mm) como en Chica (50x30mm, el estándar de 5x3cm) — ambas comparten
            // el mismo alto de 30mm; solo "Grande" (40mm de alto) usa el layout simple de abajo.
            if (Math.Abs(_height - 113) < 5)
            {
                RenderGondola(dc);
                return;
            }

            // Nombre Supermercado
            var titleFont = new FormattedText(
                "LOS ANGELES SUPERMERCADO",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                _isChica ? 7.5 : 9,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(titleFont, new Point((_width - titleFont.Width) / 2, 4));

            // Línea divisoria superior
            dc.DrawLine(new Pen(Brushes.Black, 1), new Point(3, _isChica ? 16 : 20), new Point(_width - 3, _isChica ? 16 : 20));

            // Descripción de artículo
            string desc = _articulo?.Descripcion ?? "ARTICULO SIN DESCRIPCION";
            var descFont = new FormattedText(
                desc,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                _isChica ? 9.5 : 12,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            )
            {
                MaxTextWidth = _width - 12,
                MaxTextHeight = _isChica ? 26 : 38
            };
            dc.DrawText(descFont, new Point(6, _isChica ? 20 : 24));

            // Precio Venta
            decimal precio = _articulo?.PrecioVenta ?? 0m;
            string enteroPart = Math.Truncate(precio).ToString();
            string decimalPart = ((int)((precio - Math.Truncate(precio)) * 100)).ToString("D2");

            // Entero grande
            var priceIntegerFont = new FormattedText(
                $"$ {enteroPart}",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                _isChica ? 26 : 36,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            // Centavos chicos
            var priceCentsFont = new FormattedText(
                $".{decimalPart}",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                _isChica ? 13 : 18,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            double priceX = 6;
            double priceY = _isChica ? 45 : 62;
            dc.DrawText(priceIntegerFont, new Point(priceX, priceY));
            dc.DrawText(priceCentsFont, new Point(priceX + priceIntegerFont.Width + 2, priceY + (_isChica ? 2 : 4)));

            // Barcode y Código numérico EAN
            string barcode = _articulo?.CodigoBarras ?? _articulo?.CodigoInterno ?? "0000000000000";
            
            // Dibujar Barcode vector Code 39
            double barcodeWidth = _isChica ? 0.75 : 1.0;
            double barcodeHeight = _isChica ? 18 : 28;
            double barcodeX = _isChica ? 6 : 8;
            double barcodeY = _isChica ? _height - 30 : _height - 42;

            try
            {
                Code39BarcodeDrawer.DrawBarcode(dc, barcode, barcodeX, barcodeY, barcodeHeight, barcodeWidth);
            }
            catch
            {
                // Si falla el renderizado de barra por caracteres no admitidos, dibuja línea vacía
            }

            // Texto numérico del Barcode
            var barcodeTextFont = new FormattedText(
                barcode,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                _isChica ? 7 : 8.5,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(barcodeTextFont, new Point(barcodeX + 6, _height - (_isChica ? 11 : 14)));

            // Fecha
            var dateFont = new FormattedText(
                DateTime.Now.ToString("dd/MM/yy"),
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                _isChica ? 6.5 : 7.5,
                Brushes.Gray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(dateFont, new Point(_width - dateFont.Width - 6, _height - (_isChica ? 11 : 14)));
        }

        private void RenderGondola(DrawingContext dc)
        {
            // 1. Descripción de artículo (en mayúsculas)
            string desc = (_articulo?.Descripcion ?? "ARTICULO SIN DESCRIPCION").ToUpper();
            var descFont = new FormattedText(
                desc,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                10.5,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            )
            {
                MaxTextWidth = _width - 16,
                MaxTextHeight = 28
            };
            dc.DrawText(descFont, new Point(8, 4));

            // 2. Precio Venta Grande
            decimal precio = _articulo?.PrecioVenta ?? 0m;
            var priceFont = new FormattedText(
                $"$ {precio:N2}",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                32,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(priceFont, new Point(8, 20));

            // 3. Precio sin Impuestos
            decimal alicuota = _articulo?.AlicuotaIva ?? 21m;
            bool aplicaIva = _articulo?.AplicaIva ?? true;
            decimal precioSinImp = aplicaIva ? (precio / (1 + (alicuota / 100m))) : precio;
            var sinImpFont = new FormattedText(
                $"Precio sin Imp.: $ {precioSinImp:F3}",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                8.5,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(sinImpFont, new Point(24, 60));

            // 4. Código numérico EAN y Barcode
            string barcode = _articulo?.CodigoBarras ?? _articulo?.CodigoInterno ?? "0000000000000";
            double barcodeX = 8;
            double barcodeY = _height - 38;
            double barcodeHeight = 18;
            double barcodeWidth = 0.55; // narrowWidth más chico para que entre en 60mm

            try
            {
                Code39BarcodeDrawer.DrawBarcode(dc, barcode, barcodeX, barcodeY, barcodeHeight, barcodeWidth);
            }
            catch { }

            // Texto numérico del Barcode
            var barcodeTextFont = new FormattedText(
                barcode,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                7.5,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(barcodeTextFont, new Point(barcodeX + 6, _height - 14));

            // 5. Precio por Kilo/Litro (si corresponde)
            string valorReferenciaStr = "";
            decimal contValor = _articulo?.ContenidoValor ?? 1m;
            string contUnidad = _articulo?.ContenidoUnidad ?? "UN";

            // Pesables (verdulería/carnicería/fiambrería, EAN que arranca con "20"): el precio
            // cargado YA es por kilogramo, no hay que dividir por ningún contenido neto.
            bool esPesable = _articulo?.EsPesable == true || (_articulo?.CodigoBarras?.StartsWith("20") ?? false);
            if (esPesable)
            {
                valorReferenciaStr = $"Kilo: {precio:F2}";
            }
            else if (contValor > 0 && contUnidad != "UN")
            {
                if (contUnidad == "G")
                {
                    decimal pxKg = (precio * 1000m) / contValor;
                    valorReferenciaStr = $"Kilo: {pxKg:F2}";
                }
                else if (contUnidad == "KG")
                {
                    decimal pxKg = precio / contValor;
                    valorReferenciaStr = $"Kilo: {pxKg:F2}";
                }
                else if (contUnidad == "ML")
                {
                    decimal pxL = (precio * 1000m) / contValor;
                    valorReferenciaStr = $"Litro: {pxL:F2}";
                }
                else if (contUnidad == "L")
                {
                    decimal pxL = precio / contValor;
                    valorReferenciaStr = $"Litro: {pxL:F2}";
                }
            }

            if (!string.IsNullOrEmpty(valorReferenciaStr))
            {
                var refFont = new FormattedText(
                    valorReferenciaStr,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    9.5,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                // Dibujar en la parte inferior derecha, alineado con el código de barras
                dc.DrawText(refFont, new Point(_width - refFont.Width - 10, _height - 24));
            }
        }
    }

    // ===== DIBUJADOR DE BARCODE CODE 39 VECTORIAL =====
    public static class Code39BarcodeDrawer
    {
        private static readonly Dictionary<char, string> Code39Patterns = new()
        {
            { '0', "N N W W N N N W N" }, { '1', "W N N W N N N N W" }, { '2', "N N W W N N N N W" },
            { '3', "W N W W N N N N N" }, { '4', "N N N W W N N N W" }, { '5', "W N N W W N N N N" },
            { '6', "N N W W W N N N N" }, { '7', "N N N W N N W N W" }, { '8', "W N N W N N W N N" },
            { '9', "N N W W N N W N N" }, { 'A', "W N N N N W N N W" }, { 'B', "N N W N N W N N W" },
            { 'C', "W N W N N W N N N" }, { 'D', "N N N N W W N N W" }, { 'E', "W N N N W W N N N" },
            { 'F', "N N W N W W N N N" }, { 'G', "N N N N N W W N W" }, { 'H', "W N N N N W W N N" },
            { 'I', "N N W N N W W N N" }, { 'J', "N N N N W W W N N" }, { 'K', "W N N N N N N W W" },
            { 'L', "N N W N N N N W W" }, { 'M', "W N W N N N N W N" }, { 'N', "N N N N W N W W N" }, // Fixed to standard pattern
            { 'O', "W N N N W N N W N" }, { 'P', "N N W N W N N W N" }, { 'Q', "N N N N N N W W W" },
            { 'R', "W N N N N N W W N" }, { 'S', "N N W N N N W W N" }, { 'T', "N N N N W N W W N" },
            { 'U', "W W N N N N N N W" }, { 'V', "N W W N N N N N W" }, { 'W', "W W W N N N N N N" },
            { 'X', "N W N N W N N N W" }, { 'Y', "W W N N W N N N N" }, { 'Z', "N W W N N W N N N" },
            { '-', "N W N N N N W N W" }, { '.', "W W N N N N W N N" }, { ' ', "N W W N N N W N N" },
            { '*', "N W N N W N W N N" }
        };

        public static void DrawBarcode(DrawingContext dc, string text, double x, double y, double height, double narrowWidth = 1.0)
        {
            double wideWidth = narrowWidth * 2.5;
            double currentX = x;

            // Limpiar caracteres especiales de EAN para encajar en Code 39
            string sanitized = new string(text.Where(c => Code39Patterns.ContainsKey(char.ToUpper(c))).ToArray());
            string fullText = $"*{sanitized.ToUpper()}*";

            var blackBrush = Brushes.Black;

            foreach (char c in fullText)
            {
                if (!Code39Patterns.TryGetValue(c, out var pattern))
                    continue;

                string[] elements = pattern.Split(' ');

                for (int i = 0; i < elements.Length; i++)
                {
                    bool isBar = (i % 2 == 0);
                    double w = (elements[i] == "W") ? wideWidth : narrowWidth;

                    if (isBar)
                    {
                        dc.DrawRectangle(blackBrush, null, new Rect(currentX, y, w, height));
                    }

                    currentX += w;
                }

                currentX += narrowWidth; // gap
            }
        }
    }

    // ===== DTOs INTERNOS =====
    public class EtiquetaColaItemDto
    {
        public int Id { get; set; }
        public int IdArticulo { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaCreado { get; set; }
        public ArticuloDto? Articulo { get; set; }
    }

    public class ArticuloDto
    {
        public int Id { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string CodigoInterno { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal PrecioCosto { get; set; }
        public decimal StockActual { get; set; }
        public decimal AlicuotaIva { get; set; } = 21m;
        public bool AplicaIva { get; set; } = true;
        public decimal ContenidoValor { get; set; } = 1m;
        public string ContenidoUnidad { get; set; } = "UN";
        public bool EsPesable { get; set; }
    }
}
