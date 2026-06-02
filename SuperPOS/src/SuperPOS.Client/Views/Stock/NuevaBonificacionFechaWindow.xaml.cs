using System;
using System.Windows;
using SuperPOS.Client.Services;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Stock;

public partial class NuevaBonificacionFechaWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly int _idArticulo;

    public NuevaBonificacionFechaWindow(int idArticulo)
    {
        InitializeComponent();
        _idArticulo = idArticulo;
        TitleBarCtrl.Title = "Nueva Bonificación por Fecha";

        DpDesde.SelectedDate = DateTime.Today;
        DpHasta.SelectedDate = DateTime.Today.AddDays(7); // Default 1 week promotion
    }

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDetalle.Text))
        {
            MessageBox.Show("El detalle/nombre de la promoción es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DpDesde.SelectedDate is null || DpHasta.SelectedDate is null)
        {
            MessageBox.Show("Debe seleccionar las fechas de inicio y fin.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var desde = DpDesde.SelectedDate.Value.Date;
        var hasta = DpHasta.SelectedDate.Value.Date;

        if (desde > hasta)
        {
            MessageBox.Show("La fecha de inicio no puede ser posterior a la fecha de fin.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string pctTxt = TxtPorcentaje.Text.Replace(',', '.');
        if (!decimal.TryParse(pctTxt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pct) || pct <= 0 || pct > 100)
        {
            MessageBox.Show("Ingrese un porcentaje de descuento válido (entre 0 y 100).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var bonif = new BonificacionFecha
        {
            IdArticulo = _idArticulo,
            Detalle = TxtDetalle.Text.Trim(),
            FechaDesde = new DateTime(desde.Year, desde.Month, desde.Day, 0, 0, 0, DateTimeKind.Local).ToUniversalTime(),
            FechaHasta = new DateTime(hasta.Year, hasta.Month, hasta.Day, 23, 59, 59, DateTimeKind.Local).ToUniversalTime(),
            Porcentaje = pct,
            Aplicado = ChkHabilitada.IsChecked == true
        };

        try
        {
            await App.Api.CrearBonificacionFecha(bonif);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar la promoción: {ex.Message}", "Error");
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
