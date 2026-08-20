using System.Windows;
using System.Windows.Input;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Sucursales;

public partial class SeleccionarPuntoVentaWindow : Wpf.Ui.Controls.FluentWindow
{
    public int IdCajaElegida { get; private set; }
    public int IdSucursalElegida { get; private set; }

    public SeleccionarPuntoVentaWindow(System.Collections.Generic.List<CajaDisponibleDto> cajas)
    {
        InitializeComponent();
        LstCajas.ItemsSource = cajas;
        if (cajas.Count > 0) LstCajas.SelectedIndex = 0;
    }

    private void LstCajas_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Confirmar();
    private void BtnAbrir_Click(object sender, RoutedEventArgs e) => Confirmar();

    private void Confirmar()
    {
        if (LstCajas.SelectedItem is not CajaDisponibleDto c)
        {
            MessageBox.Show("Elegí un punto de venta.", "Falta selección", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        IdCajaElegida = c.Id;
        IdSucursalElegida = c.IdSucursal;
        DialogResult = true;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Enter) Confirmar();
    }
}
