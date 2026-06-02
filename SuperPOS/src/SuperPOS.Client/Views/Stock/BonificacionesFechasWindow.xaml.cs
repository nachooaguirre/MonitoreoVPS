using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock;

public partial class BonificacionesFechasWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Articulo _articulo;

    public BonificacionesFechasWindow(Articulo articulo)
    {
        InitializeComponent();
        _articulo = articulo;
        TxtArticuloNombre.Text = $"{articulo.CodigoInterno} - {articulo.Descripcion}";
        TitleBarCtrl.Title = $"Promociones por Fechas: {articulo.Descripcion}";

        Loaded += OnLoaded;
        PreviewKeyDown += BonificacionesFechasWindow_PreviewKeyDown;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await CargarPromociones();
    }

    private async Task CargarPromociones()
    {
        try
        {
            var list = await App.Api.GetBonificacionesFechas(_articulo.Id);
            if (list != null)
            {
                // Localize dates for display
                foreach (var item in list)
                {
                    item.FechaDesde = item.FechaDesde.Kind == DateTimeKind.Utc ? item.FechaDesde.ToLocalTime() : item.FechaDesde;
                    item.FechaHasta = item.FechaHasta.Kind == DateTimeKind.Utc ? item.FechaHasta.ToLocalTime() : item.FechaHasta;
                }
                
                DgPromociones.ItemsSource = list;
                TxtNoPromos.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar las promociones: {ex.Message}", "Error");
        }
    }

    private void BonificacionesFechasWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private async void BtnAgregar_Click(object sender, RoutedEventArgs e)
    {
        var win = new NuevaBonificacionFechaWindow(_articulo.Id) { Owner = this };
        if (win.ShowDialog() == true)
        {
            await CargarPromociones();
        }
    }

    private async void BtnQuitar_Click(object sender, RoutedEventArgs e)
    {
        if (DgPromociones.SelectedItem is not BonificacionFecha selected)
        {
            MessageBox.Show("Por favor, seleccione la promoción que desea eliminar.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"¿Está seguro de que desea eliminar la promoción \"{selected.Detalle}\"?",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            try
            {
                await App.Api.EliminarBonificacionFecha(selected.Id);
                await CargarPromociones();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar la promoción: {ex.Message}", "Error");
            }
        }
    }

    private void BtnSalir_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
