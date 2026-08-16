using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Auditoria;

public partial class AuditoriaPage : Page
{
    public AuditoriaPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        DpDesde.SelectedDate = DateTime.Today.AddDays(-7);
        DpHasta.SelectedDate = DateTime.Today;

        var entidades = await App.Api.GetAuditoriaEntidades();
        CmbEntidad.ItemsSource = new List<string> { "(Todas)" }.Concat(entidades).ToList();
        CmbEntidad.SelectedIndex = 0;

        await Buscar();
    }

    private async Task Buscar()
    {
        var entidad = CmbEntidad.SelectedItem as string;
        if (entidad == "(Todas)") entidad = null;

        var (_, items) = await App.Api.GetAuditoria(
            entidad: entidad,
            buscar: string.IsNullOrWhiteSpace(TxtBuscar.Text) ? null : TxtBuscar.Text.Trim(),
            desde: DpDesde.SelectedDate,
            hasta: DpHasta.SelectedDate,
            pageSize: 300);

        DgAuditoria.ItemsSource = items;
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) => await Buscar();

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await Buscar();
    }

    private void DgAuditoria_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgAuditoria.SelectedItem is not AuditLog log) return;

        var detalle = $"Fecha: {log.Fecha:dd/MM/yyyy HH:mm:ss}\n" +
                      $"Usuario: {log.NombreUsuario ?? "(sistema)"} (id {log.IdUsuario?.ToString() ?? "-"})\n" +
                      $"Entidad: {log.Entidad} #{log.EntidadId}\n" +
                      $"Acción: {log.Accion}\n\n" +
                      $"Descripción:\n{log.Descripcion ?? "(sin detalle de campos — registro nuevo o eliminado)"}\n\n" +
                      $"Cambios (JSON):\n{log.CambiosJson ?? "-"}";

        MessageBox.Show(detalle, "Detalle de auditoría", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
