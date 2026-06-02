using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Presupuestos;

public partial class PresupuestadorWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly ApiService _api = App.Api;
    private readonly long? _presupuestoId;
    private readonly bool _soloLectura;
    
    private Presupuesto _presupuesto = new();
    private ObservableCollection<PresupuestoDetalle> _detalles = [];
    private Articulo? _articuloSeleccionado;
    
    private bool _updatingPrices = false;

    public PresupuestadorWindow(long? presupuestoId, bool soloLectura = false)
    {
        InitializeComponent();
        _presupuestoId = presupuestoId;
        _soloLectura = soloLectura;
        
        DgDetalles.ItemsSource = _detalles;

        Loaded += async (s, e) =>
        {
            await CargarClientes();
            if (_presupuestoId.HasValue)
            {
                await CargarPresupuestoExistente();
            }
            else
            {
                _presupuesto = new Presupuesto
                {
                    IdSucursal = App.SucursalId,
                    IdUsuario = App.IdUsuarioActual
                };
            }

            if (_soloLectura)
            {
                DeshabilitarControles();
            }
        };
    }

    private void DeshabilitarControles()
    {
        CmbCliente.IsEnabled = false;
        TxtContacto.IsEnabled = false;
        TxtDetalleTrabajo.IsEnabled = false;
        TxtValidez.IsEnabled = false;
        TxtFormaPago.IsEnabled = false;
        TxtObservaciones.IsEnabled = false;
        TxtBuscarArticulo.IsEnabled = false;
        TxtCantidad.IsEnabled = false;
        TxtCosto.IsEnabled = false;
        TxtMargen.IsEnabled = false;
        TxtPrecioFinal.IsEnabled = false;
        BtnGuardar.Visibility = Visibility.Collapsed;
        TitleBarCtrl.Title = "Ver Presupuesto (Solo Lectura)";
    }

    private async Task CargarClientes()
    {
        try
        {
            var (total, list) = await _api.GetClientes(pageSize: 500);
            CmbCliente.ItemsSource = list;
            if (list.Count > 0) CmbCliente.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CargarPresupuestoExistente()
    {
        try
        {
            var pres = await _api.GetPresupuesto(_presupuestoId!.Value);
            if (pres == null)
            {
                MessageBox.Show("No se encontró el presupuesto especificado.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            _presupuesto = pres;
            CmbCliente.SelectedValue = pres.IdCliente;
            TxtContacto.Text = pres.Contacto;
            TxtDetalleTrabajo.Text = pres.Detalle;
            TxtValidez.Text = pres.PlazoValidezDias.ToString();
            TxtFormaPago.Text = pres.FormaPago;
            TxtObservaciones.Text = pres.Observacion;

            _detalles.Clear();
            foreach (var d in pres.Detalles)
            {
                _detalles.Add(d);
            }

            CalcularTotales();
            TitleBarCtrl.Title = $"Editar Presupuesto #{pres.Numero}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar presupuesto: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CalcularTotales()
    {
        decimal subtotal = _detalles.Sum(d => d.SubtotalCalculado);
        _presupuesto.SubTotal = subtotal;
        _presupuesto.Total = subtotal;

        TxtSubtotalSum.Text = subtotal.ToString("C2");
        TxtTotalSum.Text = subtotal.ToString("C2");
    }

    private async void TxtBuscarArticulo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string query = TxtBuscarArticulo.Text.Trim();
            if (string.IsNullOrWhiteSpace(query)) return;

            try
            {
                Articulo? art = null;
                // Intentar buscar por código de barras primero
                art = await _api.BuscarArticuloPorCodigo(query);
                
                if (art == null)
                {
                    // Buscar por descripción
                    var (total, items) = await _api.GetArticulos(buscar: query, page: 1, pageSize: 2);
                    if (items.Count == 1)
                    {
                        art = items[0];
                    }
                    else if (items.Count > 1)
                    {
                        // Si hay múltiples coincidencias, podrías abrir un selector.
                        // Para simplificar, tomamos el primero y alertamos.
                        art = items[0];
                    }
                }

                if (art != null)
                {
                    SeleccionarArticulo(art);
                }
                else
                {
                    MessageBox.Show("No se encontró ningún artículo para: " + query, "Artículo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al buscar artículo: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SeleccionarArticulo(Articulo art)
    {
        _articuloSeleccionado = art;
        TxtBuscarArticulo.Text = art.Descripcion;
        
        _updatingPrices = true;
        TxtCosto.Text = art.PrecioCosto.ToString("F2");
        TxtMargen.Text = art.MargenGanancia.ToString("F2");
        TxtPrecioFinal.Text = art.PrecioVenta.ToString("F2");
        _updatingPrices = false;

        TxtCantidad.Focus();
        TxtCantidad.SelectAll();
    }

    private void MargenOCosto_Changed(object sender, TextChangedEventArgs e)
    {
        if (_updatingPrices) return;
        _updatingPrices = true;

        try
        {
            decimal costo = decimal.TryParse(TxtCosto.Text, out var c) ? c : 0m;
            decimal margen = decimal.TryParse(TxtMargen.Text, out var m) ? m : 0m;
            decimal precio = costo * (1 + (margen / 100m));
            TxtPrecioFinal.Text = precio.ToString("F2");
        }
        catch { }

        _updatingPrices = false;
    }

    private void PrecioFinal_Changed(object sender, TextChangedEventArgs e)
    {
        if (_updatingPrices) return;
        _updatingPrices = true;

        try
        {
            decimal costo = decimal.TryParse(TxtCosto.Text, out var c) ? c : 0m;
            decimal precio = decimal.TryParse(TxtPrecioFinal.Text, out var p) ? p : 0m;
            if (costo > 0)
            {
                decimal margen = ((precio - costo) / costo) * 100m;
                TxtMargen.Text = margen.ToString("F2");
            }
        }
        catch { }

        _updatingPrices = false;
    }

    private void BtnAgregarItem_Click(object sender, RoutedEventArgs e)
    {
        if (_articuloSeleccionado == null)
        {
            MessageBox.Show("Seleccione un artículo válido antes de agregarlo.", "Falta Artículo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtCantidad.Text, out var cant) || cant <= 0)
        {
            MessageBox.Show("Ingrese una cantidad válida y mayor a cero.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal costo = decimal.TryParse(TxtCosto.Text, out var c) ? c : 0m;
        decimal margen = decimal.TryParse(TxtMargen.Text, out var m) ? m : 0m;
        decimal precio = decimal.TryParse(TxtPrecioFinal.Text, out var p) ? p : 0m;

        // Comprobar si ya existe en la lista
        var itemExistente = _detalles.FirstOrDefault(d => d.IdArticulo == _articuloSeleccionado.Id);
        if (itemExistente != null)
        {
            itemExistente.Cantidad += cant;
            itemExistente.Precio = precio;
            itemExistente.Costo = costo;
            itemExistente.Margen = margen;
            
            // Forzar actualización de UI en Datagrid
            DgDetalles.Items.Refresh();
        }
        else
        {
            _detalles.Add(new PresupuestoDetalle
            {
                IdArticulo = _articuloSeleccionado.Id,
                Articulo = _articuloSeleccionado,
                ItemNro = _detalles.Count + 1,
                Descripcion = _articuloSeleccionado.Descripcion,
                Cantidad = cant,
                Costo = costo,
                Margen = margen,
                Precio = precio
            });
        }

        CalcularTotales();
        LimpiarBuscador();
    }

    private void LimpiarBuscador()
    {
        _articuloSeleccionado = null;
        TxtBuscarArticulo.Text = "";
        TxtCantidad.Text = "1";
        
        _updatingPrices = true;
        TxtCosto.Text = "0.00";
        TxtMargen.Text = "30.00";
        TxtPrecioFinal.Text = "0.00";
        _updatingPrices = false;

        TxtBuscarArticulo.Focus();
    }

    private void BtnQuitarItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PresupuestoDetalle det)
        {
            _detalles.Remove(det);
            // Re-indexar renglones
            for (int i = 0; i < _detalles.Count; i++)
            {
                _detalles[i].ItemNro = i + 1;
            }
            CalcularTotales();
        }
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (CmbCliente.SelectedValue is not int idCliente || idCliente <= 0)
        {
            MessageBox.Show("Seleccione un cliente para confeccionar el presupuesto.", "Falta Cliente", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtDetalleTrabajo.Text))
        {
            MessageBox.Show("Ingrese una descripción del detalle del trabajo/presupuesto.", "Falta Detalle", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_detalles.Count == 0)
        {
            MessageBox.Show("Debe agregar al menos un ítem al presupuesto.", "Presupuesto Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int validez = int.TryParse(TxtValidez.Text, out var v) ? v : 30;

        _presupuesto.IdCliente = idCliente;
        _presupuesto.Contacto = TxtContacto.Text;
        _presupuesto.Detalle = TxtDetalleTrabajo.Text;
        _presupuesto.PlazoValidezDias = validez;
        _presupuesto.FormaPago = TxtFormaPago.Text;
        _presupuesto.Observacion = TxtObservaciones.Text;
        
        _presupuesto.Detalles = _detalles.ToList();

        try
        {
            if (_presupuesto.Id == 0)
            {
                await _api.CrearPresupuesto(_presupuesto);
                MessageBox.Show("Presupuesto guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                await _api.ActualizarPresupuesto(_presupuesto);
                MessageBox.Show("Presupuesto actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al guardar presupuesto: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
