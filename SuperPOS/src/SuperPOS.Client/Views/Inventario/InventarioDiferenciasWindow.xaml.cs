using System.Globalization;
using System.Windows;

namespace SuperPOS.Client.Views.Inventario;

public partial class InventarioDiferenciasWindow : Window
{
    public InventarioDiferenciasWindow(int idInventario, string? titulo = null)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(titulo)) TxtTitulo.Text = titulo;
        Loaded += async (_, _) => await Cargar(idInventario);
    }

    private async Task Cargar(int idInventario)
    {
        try
        {
            var data = await App.Api.GetInventarioDiferencias(idInventario);
            if (data is null) { MessageBox.Show("No se pudieron cargar las diferencias."); Close(); return; }
            TxtCant.Text = data.TotalDiferencias.ToString(CultureInfo.GetCultureInfo("es-AR"));
            TxtValor.Text = data.ValorDiferencia.ToString("$ #,##0.00", CultureInfo.GetCultureInfo("es-AR"));
            Dg.ItemsSource = data.Detalle ?? [];
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); Close(); }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();
}
