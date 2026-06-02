using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Services;
using SuperPOS.Client.Views;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock;

public partial class ArticuloEditWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Articulo? _original;
    private bool _calculando;

    public ArticuloEditWindow(Articulo? articulo)
    {
        InitializeComponent();
        _original = articulo;
        TitleBarCtrl.Title = articulo is null ? "Nuevo Artículo" : $"Editar: {articulo.Descripcion}";
        Loaded += OnLoaded;
        PreviewKeyDown += ArticuloEditWindow_PreviewKeyDown;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var deptos = await App.Api.GetDepartamentos();
        CmbDepto.ItemsSource = deptos;

        var marcas = await App.Api.GetMarcas();
        CmbMarca.ItemsSource = marcas;

        var provs = (await App.Api.GetProveedoresLista())?.Where(p => p.Id > 0).ToList() ?? [];
        CmbProveedor.ItemsSource = provs;

        if (_original is not null)
        {
            TxtDescripcion.Text = _original.Descripcion;
            TxtEAN.Text = _original.CodigoBarras;
            TxtCodigoInterno.Text = _original.CodigoInterno;
            TxtCosto.Text = _original.PrecioCosto.ToString("N4");
            TxtMargen.Text = _original.MargenGanancia.ToString("N2");
            TxtPrecioVenta.Text = _original.PrecioVenta.ToString("N4");
            TxtPrecioOferta.Text = _original.PrecioOferta > 0 ? _original.PrecioOferta.ToString("N4") : "";
            TxtUxB.Text = _original.UnidadesPorBulto.ToString("N0");
            TxtCxB.Text = _original.CajasPorBulto.ToString("N0");
            TxtStock.Text = _original.StockActual.ToString("N3");
            TxtStockMin.Text = _original.StockMinimo.ToString("N3");
            TxtStockMax.Text = _original.StockMaximo.ToString("N3");
            ChkPesable.IsChecked = _original.EsPesable;
            ChkActivo.IsChecked = _original.Activo;
            ChkAplicaIva.IsChecked = _original.AplicaIva;
            ChkRequiereLote.IsChecked = _original.RequiereNroLote;
            ChkRequiereVenc.IsChecked = _original.RequiereFechaVencimiento;
            ChkRequiereSerie.IsChecked = _original.RequiereNroSerie;
            if (_original.VencimientoReferencia is { } vref)
            {
                var d = vref.Kind == DateTimeKind.Utc ? vref.ToLocalTime() : vref;
                DpVencimiento.SelectedDate = d.Date;
            }
            else
                DpVencimiento.SelectedDate = null;
            SetIva(_original.AlicuotaIva);
            CmbDepto.SelectedValue = _original.IdDepartamento;
            CmbMarca.SelectedValue = _original.IdMarca;
            CmbProveedor.SelectedValue = _original.IdProveedor > 0 ? _original.IdProveedor : provs.FirstOrDefault()?.Id;

            // Cargar bonificaciones, recargos e impuesto interno
            TxtBonif1.Text = _original.Bonificacion1 > 0 ? _original.Bonificacion1.ToString("G") : "";
            TxtBonif2.Text = _original.Bonificacion2 > 0 ? _original.Bonificacion2.ToString("G") : "";
            TxtBonif3.Text = _original.Bonificacion3 > 0 ? _original.Bonificacion3.ToString("G") : "";
            TxtBonif4.Text = _original.Bonificacion4 > 0 ? _original.Bonificacion4.ToString("G") : "";
            TxtBonif5.Text = _original.Bonificacion5 > 0 ? _original.Bonificacion5.ToString("G") : "";
            TxtRecargo1.Text = _original.Recargo1 > 0 ? _original.Recargo1.ToString("G") : "";
            TxtImpuestoInterno.Text = _original.ImpuestoInterno > 0 ? _original.ImpuestoInterno.ToString("G") : "";

            var familias = await App.Api.GetFamilias(_original.IdDepartamento);
            CmbFamilia.ItemsSource = familias;
            CmbFamilia.SelectedValue = _original.IdFamilia;
            BtnHistorial.Visibility = Visibility.Visible;
            BtnPromociones.Visibility = Visibility.Visible;
        }
        else
        {
            CmbDepto.SelectedIndex = 0;
            CmbMarca.SelectedIndex = 0;
            if (provs.Count > 0) CmbProveedor.SelectedValue = provs[0].Id;

            // Inicializar vacíos
            TxtBonif1.Text = "";
            TxtBonif2.Text = "";
            TxtBonif3.Text = "";
            TxtBonif4.Text = "";
            TxtBonif5.Text = "";
            TxtRecargo1.Text = "";
            TxtImpuestoInterno.Text = "";
        }
    }

    private void SetIva(decimal alicuota)
    {
        foreach (ComboBoxItem item in CmbIva.Items)
            if (item.Tag?.ToString() == alicuota.ToString())
            { CmbIva.SelectedItem = item; return; }
        CmbIva.SelectedIndex = 2; // default 21%
    }

    private async void CmbDepto_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CmbDepto.SelectedValue is int idDepto)
        {
            var familias = await App.Api.GetFamilias(idDepto);
            CmbFamilia.ItemsSource = familias;
            CmbFamilia.SelectedIndex = 0;
        }
    }

    private void Precio_Changed(object sender, object e)
    {
        if (_calculando) return;
        _calculando = true;
        try
        {
            decimal.TryParse(TxtCosto.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var costoBruto);
            decimal.TryParse(TxtBonif1.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif1);
            decimal.TryParse(TxtBonif2.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif2);
            decimal.TryParse(TxtBonif3.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif3);
            decimal.TryParse(TxtBonif4.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif4);
            decimal.TryParse(TxtBonif5.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif5);
            decimal.TryParse(TxtRecargo1.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var recargo1);
            decimal.TryParse(TxtImpuestoInterno.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var impInterno);
            decimal.TryParse(TxtMargen.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var margen);

            // Calcular Costo Neto
            decimal costoNeto = costoBruto;
            costoNeto -= costoNeto * (bonif1 / 100);
            costoNeto -= costoNeto * (bonif2 / 100);
            costoNeto -= costoNeto * (bonif3 / 100);
            costoNeto -= costoNeto * (bonif4 / 100);
            costoNeto -= costoNeto * (bonif5 / 100);

            decimal bonifMonto = costoBruto - costoNeto;
            decimal bonifTotalPorc = costoBruto > 0 ? (bonifMonto / costoBruto) * 100 : 0;

            decimal costoDespuesBonif = costoBruto - bonifMonto;
            decimal recargoMonto = costoDespuesBonif * (recargo1 / 100);
            decimal recargoTotalPorc = costoDespuesBonif > 0 ? (recargoMonto / costoDespuesBonif) * 100 : 0;

            costoNeto += recargoMonto;

            // Precio de venta neto (sin IVA)
            decimal precioVentaSinIva = costoNeto * (1 + margen / 100);

            // Precio con IVA
            decimal precioConIva;
            if (ChkAplicaIva.IsChecked == true)
            {
                var iva = GetIvaSeleccionado();
                precioConIva = precioVentaSinIva * (1 + iva / 100);
            }
            else
            {
                precioConIva = precioVentaSinIva;
            }

            // Precio Final (con Impuesto Interno)
            decimal precioVentaFinal = precioConIva + impInterno;

            TxtPrecioVenta.Text = precioVentaFinal.ToString("N2");

            // Actualizar etiquetas de totales
            if (LblBonifTotal != null) LblBonifTotal.Text = $"{bonifTotalPorc:N2} %";
            if (LblRecargoTotal != null) LblRecargoTotal.Text = $"{recargoTotalPorc:N2} %";
        }
        catch (Exception) { /* Ignorar errores temporales */ }
        finally { _calculando = false; }
    }

    private void Venta_Changed(object sender, object e)
    {
        if (_calculando) return;
        _calculando = true;
        try
        {
            if (decimal.TryParse(TxtPrecioVenta.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var precioVentaFinal))
            {
                decimal.TryParse(TxtCosto.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var costoBruto);
                decimal.TryParse(TxtBonif1.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif1);
                decimal.TryParse(TxtBonif2.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif2);
                decimal.TryParse(TxtBonif3.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif3);
                decimal.TryParse(TxtBonif4.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif4);
                decimal.TryParse(TxtBonif5.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif5);
                decimal.TryParse(TxtRecargo1.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var recargo1);
                decimal.TryParse(TxtImpuestoInterno.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var impInterno);

                // Calcular Costo Neto
                decimal costoNeto = costoBruto;
                costoNeto -= costoNeto * (bonif1 / 100);
                costoNeto -= costoNeto * (bonif2 / 100);
                costoNeto -= costoNeto * (bonif3 / 100);
                costoNeto -= costoNeto * (bonif4 / 100);
                costoNeto -= costoNeto * (bonif5 / 100);
                costoNeto += costoNeto * (recargo1 / 100);

                if (costoNeto > 0)
                {
                    // Precio sin impuesto interno
                    decimal precioConIva = precioVentaFinal - impInterno;

                    // Precio sin IVA
                    decimal precioVentaSinIva;
                    if (ChkAplicaIva.IsChecked == true)
                    {
                        var iva = GetIvaSeleccionado();
                        precioVentaSinIva = precioConIva / (1 + iva / 100);
                    }
                    else
                    {
                        precioVentaSinIva = precioConIva;
                    }

                    // Margen de ganancia
                    decimal margen = ((precioVentaSinIva / costoNeto) - 1) * 100;
                    TxtMargen.Text = margen.ToString("N2");
                }
            }
        }
        catch (Exception) { /* Ignorar errores temporales */ }
        finally { _calculando = false; }
    }

    private decimal GetIvaSeleccionado()
    {
        if (CmbIva.SelectedItem is ComboBoxItem item && decimal.TryParse(item.Tag?.ToString()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return 21m;
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDescripcion.Text))
        { MessageBox.Show("La descripción es obligatoria.", "Validación"); return; }

        if (!decimal.TryParse(TxtPrecioVenta.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var precio))
        { MessageBox.Show("El precio de venta no es válido.", "Validación"); return; }

        decimal.TryParse(TxtCosto.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var costo);
        decimal.TryParse(TxtMargen.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var margen);
        decimal.TryParse(TxtPrecioOferta.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var oferta);
        decimal.TryParse(TxtUxB.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var uxb); if (uxb <= 0) uxb = 1;
        decimal.TryParse(TxtCxB.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cxb); if (cxb <= 0) cxb = 1;
        decimal.TryParse(TxtStock.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var stock);
        decimal.TryParse(TxtStockMin.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var stockMin);
        decimal.TryParse(TxtStockMax.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var stockMax);

        decimal.TryParse(TxtBonif1.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif1);
        decimal.TryParse(TxtBonif2.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif2);
        decimal.TryParse(TxtBonif3.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif3);
        decimal.TryParse(TxtBonif4.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif4);
        decimal.TryParse(TxtBonif5.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bonif5);
        decimal.TryParse(TxtRecargo1.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var recargo1);
        decimal.TryParse(TxtImpuestoInterno.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var impInterno);

        var art = _original ?? new Articulo();
        art.Descripcion = TxtDescripcion.Text.Trim();
        art.CodigoBarras = TxtEAN.Text.Trim();
        art.CodigoInterno = TxtCodigoInterno.Text.Trim();
        art.PrecioCosto = costo;
        art.MargenGanancia = margen;
        art.PrecioVenta = precio;
        art.PrecioOferta = oferta;
        art.Bonificacion1 = bonif1;
        art.Bonificacion2 = bonif2;
        art.Bonificacion3 = bonif3;
        art.Bonificacion4 = bonif4;
        art.Bonificacion5 = bonif5;
        art.Recargo1 = recargo1;
        art.ImpuestoInterno = impInterno;
        art.AlicuotaIva = GetIvaSeleccionado();
        art.AplicaIva = ChkAplicaIva.IsChecked == true;
        art.UnidadesPorBulto = uxb;
        art.CajasPorBulto = cxb;
        art.StockActual = stock;
        art.StockMinimo = stockMin;
        art.StockMaximo = stockMax;
        art.EsPesable = ChkPesable.IsChecked == true;
        art.Activo = ChkActivo.IsChecked == true;
        art.RequiereNroLote = ChkRequiereLote.IsChecked == true;
        art.RequiereFechaVencimiento = ChkRequiereVenc.IsChecked == true;
        art.RequiereNroSerie = ChkRequiereSerie.IsChecked == true;
        if (DpVencimiento.SelectedDate is { } d)
        {
            var local = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Local);
            art.VencimientoReferencia = local.ToUniversalTime();
        }
        else
            art.VencimientoReferencia = null;
        art.IdDepartamento = CmbDepto.SelectedValue is int dep && dep > 0 ? dep : 1;
        art.IdFamilia = CmbFamilia.SelectedValue is int f && f > 0 ? f : 1;
        art.IdMarca = CmbMarca.SelectedValue is int mc && mc > 0 ? mc : 1;
        if (CmbProveedor.ItemsSource is IEnumerable<ProveedorSimple> listaP && listaP.Any())
        {
            art.IdProveedor = CmbProveedor.SelectedValue is int pr && pr > 0
                ? pr
                : listaP.First().Id;
        }
        else
        {
            MessageBox.Show("Tenés que tener al menos un proveedor cargado (menú Proveedores) para poder guardar un artículo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            if (_original is null)
                await App.Api.CrearArticulo(art);
            else
                await App.Api.ActualizarArticulo(art);

            if (Application.Current.MainWindow is MainWindow main)
                main.RefrescarAlertasStock();

            DialogResult = true;
            Close();
        }
        catch (Exception ex) { MessageBox.Show($"Error al guardar: {ex.Message}", "Error"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void ArticuloEditWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F8 && _original is not null)
        {
            AbrirHistorialPrecios();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F7 && _original is not null)
        {
            AbrirPromocionesFechas();
            e.Handled = true;
        }
    }

    private void BtnHistorial_Click(object sender, RoutedEventArgs e)
    {
        AbrirHistorialPrecios();
    }

    private void AbrirHistorialPrecios()
    {
        if (_original is null) return;
        var win = new HistorialPreciosWindow(_original) { Owner = this };
        win.ShowDialog();
    }

    private void BtnPromociones_Click(object sender, RoutedEventArgs e)
    {
        AbrirPromocionesFechas();
    }

    private void AbrirPromocionesFechas()
    {
        if (_original is null) return;
        var win = new BonificacionesFechasWindow(_original) { Owner = this };
        win.ShowDialog();
    }
}
