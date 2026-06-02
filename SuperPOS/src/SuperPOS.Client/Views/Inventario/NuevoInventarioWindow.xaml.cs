using System.Windows;
using System.Windows.Controls;

namespace SuperPOS.Client.Views.Inventario;

public partial class NuevoInventarioWindow : Window
{
    public string DescripcionIngresada { get; private set; } = "";
    public int? IdSucursalElegida { get; private set; }

    public NuevoInventarioWindow()
    {
        InitializeComponent();
        Loaded += NuevoInventarioWindow_Loaded;
    }

    private async void NuevoInventarioWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TxtDescripcion.Text = $"Inventario {DateTime.Now:dd/MM/yyyy HH:mm}";
        var list = await App.Api.GetSucursales();
        CboSucursal.Items.Clear();
        if (list is { Count: > 0 })
        {
            foreach (var el in list)
            {
                var id = el.TryGetProperty("id", out var pId) ? pId.GetInt32() : el.TryGetProperty("Id", out var pI) ? pI.GetInt32() : 0;
                var nom = el.TryGetProperty("nombre", out var pN) ? pN.GetString() : el.TryGetProperty("Nombre", out var pN2) ? pN2.GetString() : "";
                var central = el.TryGetProperty("esCentral", out var pC) && pC.GetBoolean() ||
                              el.TryGetProperty("EsCentral", out var pC2) && pC2.GetBoolean();
                CboSucursal.Items.Add(new SucursalItem(id, nom ?? "—", central));
            }
            var def = CboSucursal.Items.Cast<SucursalItem>().FirstOrDefault(s => s.EsCentral) ?? CboSucursal.Items[0] as SucursalItem;
            if (def != null) CboSucursal.SelectedValue = def.Id;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnCrear_Click(object sender, RoutedEventArgs e)
    {
        var d = (TxtDescripcion.Text ?? "").Trim();
        if (string.IsNullOrEmpty(d)) { MessageBox.Show("Ingrese una descripción.", "Validación", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (CboSucursal.SelectedValue is not int idSuc) { MessageBox.Show("Seleccione una sucursal.", "Validación", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        DescripcionIngresada = d;
        IdSucursalElegida = idSuc;
        DialogResult = true;
        Close();
    }

    private sealed record SucursalItem(int Id, string Nombre, bool EsCentral);
}
