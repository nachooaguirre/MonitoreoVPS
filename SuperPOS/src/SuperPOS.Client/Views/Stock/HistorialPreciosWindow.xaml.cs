using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock;

public partial class HistorialPreciosWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly Articulo _articulo;

    public HistorialPreciosWindow(Articulo articulo)
    {
        InitializeComponent();
        _articulo = articulo;
        TxtArticuloNombre.Text = $"{articulo.CodigoInterno} - {articulo.Descripcion}";
        TitleBarCtrl.Title = $"Historial de Precios y Costos: {articulo.Descripcion}";

        Loaded += OnLoaded;
        PreviewKeyDown += HistorialPreciosWindow_PreviewKeyDown;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = await App.Api.GetHistorialPrecios(_articulo.Id);
            if (logs != null)
            {
                var costos = logs.Where(l => l.Campo == "C").ToList();
                var precios = logs.Where(l => l.Campo == "V").ToList();
                var alta = logs.FirstOrDefault(l => l.Campo == "A");

                DgCostos.ItemsSource = costos;
                DgPrecios.ItemsSource = precios;

                TxtNoCostos.Visibility = costos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                TxtNoPrecios.Visibility = precios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                if (alta != null)
                {
                    var d = alta.Fecha.Kind == DateTimeKind.Utc ? alta.Fecha.ToLocalTime() : alta.Fecha;
                    TxtAltaFecha.Text = d.ToString("dd/MM/yyyy HH:mm:ss");
                    TxtAltaUsuario.Text = alta.Usuario != null ? alta.Usuario.NombreCompleto : "Sistema";
                    TxtAltaSucursal.Text = alta.Sucursal != null ? alta.Sucursal.Nombre : "Casa Central";
                }
                else
                {
                    var d = _articulo.FechaAlta.Kind == DateTimeKind.Utc ? _articulo.FechaAlta.ToLocalTime() : _articulo.FechaAlta;
                    TxtAltaFecha.Text = d.ToString("dd/MM/yyyy HH:mm:ss");
                    TxtAltaUsuario.Text = "Sistema";
                    TxtAltaSucursal.Text = "Casa Central";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar el historial: {ex.Message}", "Error");
        }
    }

    private void HistorialPreciosWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void BtnSalir_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
