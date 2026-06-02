using System.Windows;
using SuperPOS.Client.Models;
using SuperPOS.Client.Services;

namespace SuperPOS.Client.Views.OrdenesCompra;

public partial class SugerenciaOCWindow : Window
{
    public bool SeCreo { get; private set; }
    private SugerenciaOCDto? _sugerencia;

    public SugerenciaOCWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            try
            {
                var proveedores = await App.Api.GetProveedoresLista();
                CboProveedor.DisplayMemberPath = "RazonSocial";
                CboProveedor.SelectedValuePath = "Id";
                CboProveedor.ItemsSource = proveedores;
                if (proveedores == null || proveedores.Count == 0)
                    MessageBox.Show("No hay proveedores cargados en la base de datos.\nVaya a la sección Proveedores para agregar uno.", "Sin proveedores", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
    }

    private async void BtnVer_Click(object sender, RoutedEventArgs e)
    {
        if (CboProveedor.SelectedItem is not ProveedorSimple prov) { MessageBox.Show("Seleccione un proveedor."); return; }
        var idProv = prov.Id;
        try
        {
            _sugerencia = await App.Api.GetSugerenciaOC(idProv);
            DgItems.ItemsSource = _sugerencia?.Items;
            TxtCantItems.Text = _sugerencia?.CantidadArticulos.ToString() ?? "0";
            TxtTotalEst.Text = _sugerencia?.TotalEstimado.ToString("$ #,##0.00") ?? "$ 0";
            BtnCrear.IsEnabled = (_sugerencia?.Items?.Count ?? 0) > 0;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private async void BtnCrear_Click(object sender, RoutedEventArgs e)
    {
        if (_sugerencia is null || CboProveedor.SelectedItem is not ProveedorSimple prov2) return;
        try
        {
            await App.Api.CrearOrdenCompraDesde(_sugerencia, prov2.Id);
            SeCreo = true;
            MessageBox.Show("✅ Orden de compra creada correctamente.", "OK");
            Close();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
}
