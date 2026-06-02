using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SuperPOS.Client.Views.Tesoreria;

public partial class NuevoMovimientoWindow : Window
{
    public NuevoMovimientoWindow()
    {
        BuildUI();
        Loaded += async (_, _) => await CargarCuentas();
    }

    private ComboBox _cboCuenta = null!, _cboCuentaDestino = null!, _cboTipo = null!, _cboConceptoSugerido = null!;
    private System.Windows.Controls.TextBlock _lblCuentaDestino = null!;
    private System.Windows.Controls.TextBox _txtConcepto = null!, _txtMonto = null!, _txtDocumento = null!, _txtBeneficiario = null!;
    private List<dynamic>? _cuentas;

    private void BuildUI()
    {
        Title = "Nuevo Movimiento de Tesorería";
        Width = 520; Height = 540;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20,20,20));
        ResizeMode = ResizeMode.NoResize;

        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(Label("TIPO DE MOVIMIENTO"));
        _cboTipo = Combo(new[] { "Ingreso", "Egreso", "Transferencia entre cuentas", "Ajuste positivo", "Ajuste negativo" });
        _cboTipo.SelectionChanged += Tipo_SelectionChanged;
        sp.Children.Add(_cboTipo);

        sp.Children.Add(Label("CUENTA ORIGEN / PRINCIPAL"));
        _cboCuenta = new ComboBox { Height = 34, Foreground = System.Windows.Media.Brushes.WhiteSmoke, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42,42,42)), Margin = new Thickness(0,4,0,10) };
        sp.Children.Add(_cboCuenta);

        _lblCuentaDestino = Label("CUENTA DESTINO");
        _lblCuentaDestino.Visibility = Visibility.Collapsed;
        sp.Children.Add(_lblCuentaDestino);

        _cboCuentaDestino = new ComboBox { Height = 34, Foreground = System.Windows.Media.Brushes.WhiteSmoke, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42,42,42)), Margin = new Thickness(0,4,0,10) };
        _cboCuentaDestino.Visibility = Visibility.Collapsed;
        sp.Children.Add(_cboCuentaDestino);

        sp.Children.Add(Label("CONCEPTO COMÚN / SUGERIDO"));
        _cboConceptoSugerido = Combo(new[] {
            "Concepto Personalizado (escribir abajo)",
            "🏦 Comisión Bancaria",
            "🏦 Impuesto Ley 25.413 (Imp. al Cheque)",
            "🏦 Intereses Deudores",
            "🏦 Intereses Acreedores",
            "🏦 Acreditación de Cupones Tarjetas",
            "📊 Ajuste de Saldo"
        });
        _cboConceptoSugerido.SelectionChanged += ConceptoSugerido_SelectionChanged;
        sp.Children.Add(_cboConceptoSugerido);

        sp.Children.Add(Label("CONCEPTO / DETALLE *"));
        _txtConcepto = Txt("Descripción del movimiento");
        sp.Children.Add(_txtConcepto);

        var gr = new Grid { Margin = new Thickness(0,0,0,10) };
        gr.ColumnDefinitions.Add(new ColumnDefinition());
        gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        gr.ColumnDefinitions.Add(new ColumnDefinition());

        var sp1 = new StackPanel();
        sp1.Children.Add(Label("MONTO *"));
        _txtMonto = Txt("0.00"); _txtMonto.Margin = new Thickness(0,4,0,0);
        sp1.Children.Add(_txtMonto);
        Grid.SetColumn(sp1, 0); gr.Children.Add(sp1);

        var sp2 = new StackPanel();
        sp2.Children.Add(Label("N° DOCUMENTO"));
        _txtDocumento = Txt("Nro. cheque / transf."); _txtDocumento.Margin = new Thickness(0,4,0,0);
        sp2.Children.Add(_txtDocumento);
        Grid.SetColumn(sp2, 2); gr.Children.Add(sp2);
        sp.Children.Add(gr);

        sp.Children.Add(Label("BENEFICIARIO / LIBRADOR"));
        _txtBeneficiario = Txt("Proveedor, empleado, etc.");
        sp.Children.Add(_txtBeneficiario);

        var btnGrid = new Grid { Margin = new Thickness(0,16,0,0) };
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var btnCancel = new Button { Content = "Cancelar", Height = 38 };
        btnCancel.Click += (_, _) => Close();
        var btnOk = new Button { Content = "✓  Guardar", Height = 38, Width = 140,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26,64,40)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64,208,128)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42,80,56)),
            BorderThickness = new Thickness(1) };
        btnOk.Click += Guardar_Click;
        Grid.SetColumn(btnCancel, 0); Grid.SetColumn(btnOk, 1);
        btnGrid.Children.Add(btnCancel); btnGrid.Children.Add(btnOk);
        sp.Children.Add(btnGrid);

        Content = sp;
    }

    private static System.Windows.Controls.TextBlock Label(string text) => new()
    { Text = text, FontSize = 10, FontWeight = FontWeights.Bold,
      Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96,112,128)) };

    private static System.Windows.Controls.TextBox Txt(string placeholder) => new()
    { Height = 34, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42,42,42)),
      Foreground = System.Windows.Media.Brushes.WhiteSmoke, BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58,58,58)),
      Margin = new Thickness(0,4,0,10), Padding = new Thickness(8,6,8,6), BorderThickness = new Thickness(1) };

    private static ComboBox Combo(string[] items)
    {
        var c = new ComboBox { Height = 34, Foreground = System.Windows.Media.Brushes.WhiteSmoke, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42,42,42)), Margin = new Thickness(0,4,0,10) };
        foreach (var i in items) c.Items.Add(new ComboBoxItem { Content = i });
        c.SelectedIndex = 0;
        return c;
    }

    private void Tipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_lblCuentaDestino == null || _cboCuentaDestino == null) return;
        if (_cboTipo.SelectedIndex == 2)
        {
            _lblCuentaDestino.Visibility = Visibility.Visible;
            _cboCuentaDestino.Visibility = Visibility.Visible;
        }
        else
        {
            _lblCuentaDestino.Visibility = Visibility.Collapsed;
            _cboCuentaDestino.Visibility = Visibility.Collapsed;
        }
    }

    private void ConceptoSugerido_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cboConceptoSugerido == null || _txtConcepto == null || _cboTipo == null) return;

        int idx = _cboConceptoSugerido.SelectedIndex;
        if (idx == 0)
        {
            _txtConcepto.Text = "";
            _txtConcepto.IsEnabled = true;
        }
        else
        {
            var comboItem = (ComboBoxItem)_cboConceptoSugerido.SelectedItem;
            var text = comboItem.Content.ToString()!.Substring(3); // Quitar emoji
            _txtConcepto.Text = text;
            _txtConcepto.IsEnabled = true;

            // Auto-seleccionar tipo de movimiento coherente
            if (idx == 1 || idx == 2 || idx == 3)
            {
                _cboTipo.SelectedIndex = 1; // Egreso
            }
            else if (idx == 4 || idx == 5)
            {
                _cboTipo.SelectedIndex = 0; // Ingreso
            }
            else if (idx == 6)
            {
                _cboTipo.SelectedIndex = 3; // Ajuste positivo (ajuste saldo)
            }
        }
    }

    private async Task CargarCuentas()
    {
        try
        {
            _cuentas = await App.Api.GetCuentasTesoreria();
            
            _cboCuenta.DisplayMemberPath = "nombre";
            _cboCuenta.ItemsSource = _cuentas;
            _cboCuenta.IsSynchronizedWithCurrentItem = false;
            if (_cuentas?.Count > 0) _cboCuenta.SelectedIndex = 0;

            _cboCuentaDestino.DisplayMemberPath = "nombre";
            _cboCuentaDestino.ItemsSource = _cuentas?.ToList();
            _cboCuentaDestino.IsSynchronizedWithCurrentItem = false;
            if (_cuentas?.Count > 0) _cboCuentaDestino.SelectedIndex = 0;
        }
        catch { }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtConcepto.Text) || !decimal.TryParse(_txtMonto.Text, out var monto) || monto <= 0)
        { MessageBox.Show("Complete concepto y monto válido."); return; }

        if (_cboCuenta.SelectedItem is null) { MessageBox.Show("Seleccione una cuenta."); return; }
        dynamic cuentaSel = _cboCuenta.SelectedItem!;
        int idCuenta = Convert.ToInt32(cuentaSel.id);

        int? idCuentaDestino = null;
        if (_cboTipo.SelectedIndex == 2)
        {
            if (_cboCuentaDestino.SelectedItem is null)
            {
                MessageBox.Show("Seleccione una cuenta de destino.");
                return;
            }
            dynamic cuentaDestSel = _cboCuentaDestino.SelectedItem!;
            int idDest = Convert.ToInt32(cuentaDestSel.id);
            if (idDest == idCuenta)
            {
                MessageBox.Show("La cuenta de destino debe ser diferente a la cuenta de origen.");
                return;
            }
            idCuentaDestino = idDest;
        }

        try
        {
            await App.Api.RegistrarMovimientoTesoreria(new
            {
                IdCuenta = idCuenta,
                IdCuentaDestino = idCuentaDestino,
                Tipo = _cboTipo.SelectedIndex,
                Concepto = _txtConcepto.Text.Trim(),
                Monto = monto,
                NroDocumento = _txtDocumento.Text.Trim(),
                Beneficiario = _txtBeneficiario.Text.Trim(),
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual
            });
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }
}
