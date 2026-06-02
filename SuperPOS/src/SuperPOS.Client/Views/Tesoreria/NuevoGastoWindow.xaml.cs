using System.Windows;
using System.Windows.Controls;

namespace SuperPOS.Client.Views.Tesoreria;

public partial class NuevoGastoWindow : Window
{
    private ComboBox _cboCategoria = null!;
    private System.Windows.Controls.TextBox _txtDesc = null!, _txtMonto = null!, _txtNroComp = null!, _txtObs = null!;

    public NuevoGastoWindow()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        Title = "Registrar Gasto por Caja";
        Width = 480; Height = 380;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20));
        ResizeMode = ResizeMode.NoResize;

        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(Lbl("CATEGORÍA"));
        _cboCategoria = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 10),
            Foreground = System.Windows.Media.Brushes.WhiteSmoke,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)) };
        var cats = new[] { "🧹 Limpieza", "📦 Insumos", "💡 Servicios", "🔧 Mantenimiento", "👷 Personal", "🚚 Logística", "📋 Varios" };
        foreach (var c in cats) _cboCategoria.Items.Add(new ComboBoxItem { Content = c });
        _cboCategoria.SelectedIndex = 0;
        sp.Children.Add(_cboCategoria);

        sp.Children.Add(Lbl("DESCRIPCIÓN *"));
        _txtDesc = Txt("Detalle del gasto...");
        sp.Children.Add(_txtDesc);

        var gr = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        gr.ColumnDefinitions.Add(new ColumnDefinition());
        gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        gr.ColumnDefinitions.Add(new ColumnDefinition());
        var sp1 = new StackPanel(); sp1.Children.Add(Lbl("MONTO *")); _txtMonto = Txt("0.00"); _txtMonto.Margin = new Thickness(0, 4, 0, 0); sp1.Children.Add(_txtMonto);
        var sp2 = new StackPanel(); sp2.Children.Add(Lbl("N° COMPROBANTE")); _txtNroComp = Txt("Factura/ticket"); _txtNroComp.Margin = new Thickness(0, 4, 0, 0); sp2.Children.Add(_txtNroComp);
        Grid.SetColumn(sp2, 2);
        gr.Children.Add(sp1); gr.Children.Add(sp2);
        sp.Children.Add(gr);

        sp.Children.Add(Lbl("OBSERVACIONES"));
        _txtObs = Txt("Observaciones...");
        sp.Children.Add(_txtObs);

        var btnGr = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        btnGr.ColumnDefinitions.Add(new ColumnDefinition());
        btnGr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var btnC = new Button { Content = "Cancelar", Height = 36 };
        btnC.Click += (_, _) => Close();
        var btnO = new Button { Content = "✓  Registrar", Height = 36, Width = 140,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 20, 20)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 80, 80)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 30, 30)),
            BorderThickness = new Thickness(1) };
        btnO.Click += Guardar_Click;
        Grid.SetColumn(btnO, 1);
        btnGr.Children.Add(btnC); btnGr.Children.Add(btnO);
        sp.Children.Add(btnGr);

        Content = sp;
    }

    private static System.Windows.Controls.TextBlock Lbl(string t) => new()
    { Text = t, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 112, 128)) };

    private static System.Windows.Controls.TextBox Txt(string ph) => new()
    { Height = 34, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)),
      Foreground = System.Windows.Media.Brushes.WhiteSmoke,
      BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58)),
      Margin = new Thickness(0, 4, 0, 10), Padding = new Thickness(8, 6, 8, 6), BorderThickness = new Thickness(1) };

    private static int[] _categoriaValues = { 0, 1, 2, 3, 4, 5, 9 };

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtDesc.Text) || !decimal.TryParse(_txtMonto.Text, out var monto) || monto <= 0)
        { MessageBox.Show("Complete descripción y monto válido."); return; }

        try
        {
            await App.Api.RegistrarGastoCaja(new
            {
                Categoria = _categoriaValues[_cboCategoria.SelectedIndex],
                Descripcion = _txtDesc.Text,
                Monto = monto,
                IdCajaOrigen = App.CajaId,
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                NroComprobante = _txtNroComp.Text,
                Observaciones = _txtObs.Text
            });
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }
}
