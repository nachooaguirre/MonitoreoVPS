using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Presupuestos;

public partial class PresupuestosPage : Page
{
    private readonly ApiService _api = App.Api;
    private int _pagina = 1;
    private const int PageSize = 30;

    public PresupuestosPage()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            await CargarClientes();
            CmbEstado.SelectedIndex = 0; // "Todos"
            await CargarDatos();
        };
    }

    private async Task CargarClientes()
    {
        try
        {
            var (total, list) = await _api.GetClientes(pageSize: 500);
            var todos = new List<Cliente> { new() { Id = 0, RazonSocial = "Todos los Clientes" } };
            todos.AddRange(list);
            CmbCliente.ItemsSource = todos;
            CmbCliente.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CargarDatos()
    {
        try
        {
            DateTime? desde = DpDesde.SelectedDate;
            DateTime? hasta = DpHasta.SelectedDate;
            
            int? idCliente = null;
            if (CmbCliente.SelectedValue is int id && id > 0)
            {
                idCliente = id;
            }

            EstadoPresupuesto? estado = null;
            if (CmbEstado.SelectedItem is ComboBoxItem item && !string.IsNullOrEmpty(item.Tag?.ToString()))
            {
                estado = (EstadoPresupuesto)int.Parse(item.Tag.ToString()!);
            }

            var (total, items) = await _api.GetPresupuestos(desde, hasta, idCliente, estado, _pagina, PageSize);
            DgPresupuestos.ItemsSource = items;

            TxtTotal.Text = $"Total: {total} registros";
            TxtPagina.Text = $"{_pagina}";
            BtnPrev.IsEnabled = _pagina > 1;
            BtnNext.IsEnabled = _pagina * PageSize < total;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al cargar presupuestos: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        CmbCliente.SelectedIndex = 0;
        CmbEstado.SelectedIndex = 0;
        _pagina = 1;
        await CargarDatos();
    }

    private async void BtnNuevo_Click(object sender, RoutedEventArgs e)
    {
        var win = new PresupuestadorWindow(null)
        {
            Owner = Window.GetWindow(this)
        };
        if (win.ShowDialog() == true)
        {
            await CargarDatos();
        }
    }

    private async void BtnEditar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Presupuesto pres)
        {
            if (pres.Estado == EstadoPresupuesto.Facturado)
            {
                MessageBox.Show("No se puede editar un presupuesto que ya ha sido facturado.", "Presupuesto Facturado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new PresupuestadorWindow(pres.Id)
            {
                Owner = Window.GetWindow(this)
            };
            if (win.ShowDialog() == true)
            {
                await CargarDatos();
            }
        }
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Presupuesto pres)
        {
            if (pres.Estado == EstadoPresupuesto.Facturado)
            {
                MessageBox.Show("No se puede eliminar un presupuesto que ya ha sido facturado.", "Presupuesto Facturado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"¿Está seguro de que desea eliminar el presupuesto #{pres.Numero}?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _api.EliminarPresupuesto(pres.Id);
                    await CargarDatos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void BtnFacturar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Presupuesto pres)
        {
            if (pres.Estado == EstadoPresupuesto.Facturado)
            {
                MessageBox.Show("Este presupuesto ya ha sido facturado.", "Presupuesto Facturado", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"¿Desea facturar y convertir el presupuesto #{pres.Numero} en una venta real? (Esto decrementará stock)", "Confirmar Facturación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Enviar llamada a facturar con parámetros por defecto (Caja=1, Efectivo=1, Auto-determinar Factura A/B, Punto Venta=1)
                    bool exito = await _api.FacturarPresupuesto(pres.Id, idCaja: 1, idMedioPago: 1, idTipoComprobante: 0, letra: null, puntoVenta: 1);
                    if (exito)
                    {
                        MessageBox.Show("Presupuesto facturado correctamente. Stock actualizado.", "Venta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CargarDatos();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo facturar el presupuesto. Verifique la conexión.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al facturar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void DgPresupuestos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgPresupuestos.SelectedItem is Presupuesto pres)
        {
            if (pres.Estado == EstadoPresupuesto.Facturado)
            {
                // Solo ver (se podría deshabilitar el guardado dentro de la ventana)
                var win = new PresupuestadorWindow(pres.Id, soloLectura: true)
                {
                    Owner = Window.GetWindow(this)
                };
                win.ShowDialog();
            }
            else
            {
                var win = new PresupuestadorWindow(pres.Id)
                {
                    Owner = Window.GetWindow(this)
                };
                if (win.ShowDialog() == true)
                {
                    CargarDatos();
                }
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
