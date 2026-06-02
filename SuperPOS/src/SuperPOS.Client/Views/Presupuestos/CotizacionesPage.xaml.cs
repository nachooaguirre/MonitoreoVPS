using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Presupuestos;

public partial class CotizacionesPage : Page
{
    private readonly ApiService _api = App.Api;
    private int _pagina = 1;
    private const int PageSize = 30;

    public CotizacionesPage()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            await CargarProveedores();
            await CargarDatos();
        };
    }

    private async Task CargarProveedores()
    {
        try
        {
            var provs = await _api.GetProveedoresLista();
            var list = new List<ProveedorSimple> { new() { Id = 0, RazonSocial = "Todos los Proveedores" } };
            if (provs != null)
            {
                list.AddRange(provs);
            }
            CmbProveedor.ItemsSource = list;
            CmbProveedor.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar proveedores: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CargarDatos()
    {
        try
        {
            DateTime? desde = DpDesde.SelectedDate;
            DateTime? hasta = DpHasta.SelectedDate;
            
            int? idProv = null;
            if (CmbProveedor.SelectedValue is int id && id > 0)
            {
                idProv = id;
            }

            var (total, items) = await _api.GetCotizaciones(desde, hasta, idProv, _pagina, PageSize);
            DgCotizaciones.ItemsSource = items;

            TxtTotal.Text = $"Total: {total} registros";
            TxtPagina.Text = $"{_pagina}";
            BtnPrev.IsEnabled = _pagina > 1;
            BtnNext.IsEnabled = _pagina * PageSize < total;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar cotizaciones: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Filtro_Changed(object sender, SelectionChangedEventArgs e)
    {
        _pagina = 1;
        await CargarDatos();
    }

    private async void BtnLimpiar_Click(object sender, RoutedEventArgs e)
    {
        DpDesde.SelectedDate = null;
        DpHasta.SelectedDate = null;
        CmbProveedor.SelectedIndex = 0;
        _pagina = 1;
        await CargarDatos();
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Cotizacion cot)
        {
            if (MessageBox.Show($"¿Desea eliminar la cotización #{cot.Numero} del proveedor {cot.Proveedor?.RazonSocial}?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _api.EliminarCotizacion(cot.Id);
                    await CargarDatos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void DgCotizaciones_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgCotizaciones.SelectedItem is Cotizacion cot)
        {
            try
            {
                var det = await _api.GetCotizacion(cot.Id);
                if (det == null) return;

                var sb = new StringBuilder();
                sb.AppendLine($"Cotización #{det.Numero} - {det.Proveedor?.RazonSocial}");
                sb.AppendLine($"Fecha: {det.Fecha:dd/MM/yyyy}");
                sb.AppendLine($"Plazo de entrega: {det.PlazoEntrega ?? "No especificado"}");
                sb.AppendLine($"Descripción: {det.Descripcion}");
                if (!string.IsNullOrEmpty(det.Observacion))
                {
                    sb.AppendLine($"Observaciones: {det.Observacion}");
                }
                sb.AppendLine();
                sb.AppendLine("Artículos cotizados:");
                sb.AppendLine("--------------------------------------------------");
                
                foreach (var d in det.Detalles)
                {
                    sb.AppendLine($"- {d.Articulo?.Descripcion ?? "Artículo desconocido"} (Cant: {d.Cantidad:N2}) - Precio: {d.Precio:C2}");
                }

                MessageBox.Show(sb.ToString(), $"Detalle de Cotización #{det.Numero}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar detalles de la cotización: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_pagina > 1)
        {
            _pagina--;
            await CargarDatos();
        }
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        _pagina++;
        await CargarDatos();
    }
}
