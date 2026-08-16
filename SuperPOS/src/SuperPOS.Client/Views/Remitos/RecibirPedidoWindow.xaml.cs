using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Remitos;

public class ItemRecepcionVm
{
    public int IdArticulo { get; set; }
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal CantidadPedida { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal PrecioCosto { get; set; }
    public string? LoteNro { get; set; }
    public DateTime? FechaVenc { get; set; }
    public string? ObservacionDiferencia { get; set; }
}

public partial class RecibirPedidoWindow : Window
{
    private ObservableCollection<ItemRecepcionVm> _items = [];
    private int _idOCSeleccionada;

    public RecibirPedidoWindow(int preseleccionarOC = 0)
    {
        InitializeComponent();
        _idOCSeleccionada = preseleccionarOC;
        DgItems.ItemsSource = _items;
        Loaded += async (_, _) =>
        {
            await CargarOCs();
            if (_idOCSeleccionada > 0)
            {
                // Pre-seleccionar la OC si se pasó como parámetro
                var item = CboOC.Items.Cast<dynamic>().FirstOrDefault(x => Convert.ToInt32(x.Id) == _idOCSeleccionada);
                if (item != null) CboOC.SelectedItem = item;
                await Task.Delay(100);
                BtnCargarArticulos_Click(this, new RoutedEventArgs());
            }
        };
    }

    private async Task CargarOCs()
    {
        try
        {
            var ocs = await App.Api.GetOrdenesCompra() ?? [];
            var filtrada = ocs
                .Where(o => o.Estado is >= 0 and < 3)
                .OrderByDescending(o => o.Fecha)
                .ToList();

            if (_idOCSeleccionada > 0 && filtrada.All(o => o.Id != _idOCSeleccionada))
            {
                var det = await App.Api.GetOrdenCompraDetalle(_idOCSeleccionada);
                if (det is JsonElement root && root.ValueKind == JsonValueKind.Object)
                {
                    var st = TryGetInt(root, "estado", "Estado");
                    if (st is >= 0 and < 3)
                    {
                        filtrada.Insert(0, new OrdenCompraResumenDto
                        {
                            Id = TryGetInt(root, "id", "Id"),
                            NroOrden = TryGetInt(root, "nroOrden", "NroOrden"),
                            Fecha = DateTime.MinValue,
                            ProveedorNombre = TryGetString(root, "proveedorNombre", "ProveedorNombre"),
                            Total = 0,
                            Estado = st
                        });
                    }
                }
            }

            var ocsPendientes = filtrada
                .Select(o => new
                {
                    o.Id,
                    NroOrdenDisplay = $"OC-{o.NroOrden:D6}  ({o.ProveedorNombre})  [{o.EstadoNombre}]",
                    o.ProveedorNombre
                })
                .ToList();
            CboOC.ItemsSource = ocsPendientes;
        }
        catch (Exception ex) { MessageBox.Show($"Error cargando OCs: {ex.Message}"); }
    }

    private void CboOC_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CboOC.SelectedItem is null) return;
        dynamic sel = CboOC.SelectedItem;
        _idOCSeleccionada = Convert.ToInt32(sel.Id);
        TxtProveedorOC.Text = Convert.ToString(sel.ProveedorNombre) ?? "";
    }

    private async void BtnCargarArticulos_Click(object sender, RoutedEventArgs e)
    {
        if (_idOCSeleccionada == 0) { MessageBox.Show("Seleccione una Orden de Compra."); return; }
        try
        {
            var ocEl = await App.Api.GetOrdenCompraDetalle(_idOCSeleccionada);
            if (ocEl is null) return;
            var root = ocEl.Value;
            if (!TryGetArray(root, "detalles", "Detalles", out var detalles)) return;

            _items.Clear();
            var detallesList = detalles.EnumerateArray().ToList();
            var estadoOC = TryGetInt(root, "estado", "Estado");
            bool wasAudited = (estadoOC == 2 || estadoOC == 3) || detallesList.Any(d => TryGetDecimal(d, "cantidadRecibida", "CantidadRecibida") > 0);

            foreach (var det in detallesList)
            {
                var idArt = TryGetInt(det, "idArticulo", "IdArticulo");
                var cantPed = TryGetDecimal(det, "cantidadPedida", "CantidadPedida");
                var cantRec = TryGetDecimal(det, "cantidadRecibida", "CantidadRecibida");
                var precio = TryGetDecimal(det, "precioCosto", "PrecioCosto");
                var obsDif = TryGetString(det, "observacionDiferencia", "ObservacionDiferencia");
                var cod = "";
                var desc = "";
                if (TryGetObject(det, "articulo", "Articulo", out var art))
                {
                    cod = TryGetString(art, "codigoBarras", "CodigoBarras") ?? "";
                    desc = TryGetString(art, "descripcion", "Descripcion") ?? "";
                }

                _items.Add(new ItemRecepcionVm
                {
                    IdArticulo = idArt,
                    CodigoBarras = cod,
                    Descripcion = desc,
                    CantidadPedida = cantPed,
                    CantidadRecibida = wasAudited ? cantRec : cantPed,
                    PrecioCosto = precio,
                    ObservacionDiferencia = obsDif
                });
            }

            ActualizarTotales();
            BtnConfirmar.IsEnabled = _items.Count > 0;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void ActualizarTotales()
    {
        TxtCantItems.Text = $"{_items.Count} artículos  |";
        TxtTotalCosto.Text = _items.Sum(i => i.CantidadRecibida * i.PrecioCosto).ToString("$ #,##0.00");
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        BtnConfirmar.IsEnabled = false;
        try
        {
            // Crear remito desde la OC
            var idRemito = await App.Api.CrearRemitoDesdeOC(_idOCSeleccionada, new
            {
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                NroRemitoExterno = TxtNroRemitoExt.Text,
                Transportista = TxtTransportista.Text,
                Observaciones = TxtObs.Text
            });

            // Confirmar con cantidades reales
            await App.Api.ConfirmarRemito(idRemito, new
            {
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                Items = _items.Select(i => new
                {
                    i.IdArticulo,
                    i.CantidadRecibida,
                    i.PrecioCosto,
                    i.LoteNro,
                    FechaVencimiento = i.FechaVenc
                }).ToList()
            });

            MessageBox.Show($"✅ Recepción confirmada.\nRemito REM-{idRemito:D6} generado.\nStock actualizado.", "Recepción Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
            BtnConfirmar.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();

    private static bool TryGetArray(JsonElement r, string camel, string pascal, out JsonElement arr)
    {
        if (r.TryGetProperty(camel, out arr) && arr.ValueKind == JsonValueKind.Array) return true;
        if (r.TryGetProperty(pascal, out arr) && arr.ValueKind == JsonValueKind.Array) return true;
        arr = default;
        return false;
    }

    private static bool TryGetObject(JsonElement r, string camel, string pascal, out JsonElement obj)
    {
        if (r.TryGetProperty(camel, out obj) && obj.ValueKind == JsonValueKind.Object) return true;
        if (r.TryGetProperty(pascal, out obj) && obj.ValueKind == JsonValueKind.Object) return true;
        obj = default;
        return false;
    }

    private static string? TryGetString(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.String) return a.GetString();
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.String) return b.GetString();
        return null;
    }

    private static int TryGetInt(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetInt32(out var i)) return i;
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetInt32(out var j)) return j;
        return 0;
    }

    private static decimal TryGetDecimal(JsonElement r, string camel, string pascal)
    {
        if (r.TryGetProperty(camel, out var a) && a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var d)) return d;
        if (r.TryGetProperty(pascal, out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetDecimal(out var d2)) return d2;
        return 0;
    }
}
