using System.Windows;
using System.Windows.Controls;
using SuperPOS.Client.Models;

namespace SuperPOS.Client.Views.Sucursales;

public partial class TerminalesPage : Page
{
    public TerminalesPage() { InitializeComponent(); Loaded += OnLoaded; }
    private async void OnLoaded(object sender, RoutedEventArgs e) => await Cargar();

    private async Task Cargar()
    {
        var estados = await App.Api.GetEstadoTerminales();
        DgTerminales.ItemsSource = estados.Select(e => new TerminalFila(e)).ToList();
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e) => await Cargar();

    private class TerminalFila(CajaEstadoDto e)
    {
        public string Nombre => e.Nombre;
        public string? SucursalNombre => e.SucursalNombre;
        public bool Activo => e.Activo;
        public string UltimaVentaTexto => e.UltimaVenta?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Sin ventas registradas";
        public string EstadoTexto => e.EnLinea ? "🟢 En línea" : "⚪ Inactiva";
        public System.Windows.Media.Brush EstadoColor => e.EnLinea
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0xD0, 0x80))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x90, 0x90, 0x90));
    }
}
