using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SuperPOS.Client.Views.Tesoreria;

public partial class NuevaCuentaWindow : Window
{
    private ComboBox _cboTipo = null!, _cboBanco = null!;
    private System.Windows.Controls.TextBox _txtNombre = null!, _txtNroCuenta = null!, _txtCBU = null!, _txtSaldoInicial = null!;

    public NuevaCuentaWindow()
    {
        BuildUI();
        Loaded += async (_, _) => await CargarBancos();
    }

    private void BuildUI()
    {
        Title = "Nueva Cuenta de Tesorería";
        Width = 480; Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20));
        ResizeMode = ResizeMode.NoResize;

        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(Lbl("TIPO DE CUENTA"));
        _cboTipo = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 10),
            Foreground = System.Windows.Media.Brushes.WhiteSmoke,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)) };
        var tipos = new[] { "💵 Caja Efectivo", "🏦 Cuenta Corriente Bancaria", "🏦 Caja de Ahorro", "💳 Tarjeta de Crédito", "📋 Otro" };
        foreach (var t in tipos) _cboTipo.Items.Add(new ComboBoxItem { Content = t });
        _cboTipo.SelectedIndex = 0;
        sp.Children.Add(_cboTipo);

        sp.Children.Add(Lbl("NOMBRE *"));
        _txtNombre = Txt("Ej: Caja Principal, Banco Nación Cta. Cte.");
        sp.Children.Add(_txtNombre);

        var gr = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        gr.ColumnDefinitions.Add(new ColumnDefinition()); gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); gr.ColumnDefinitions.Add(new ColumnDefinition());
        var sp1 = new StackPanel(); sp1.Children.Add(Lbl("BANCO"));
        _cboBanco = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 0),
            Foreground = System.Windows.Media.Brushes.WhiteSmoke,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)) };
        sp1.Children.Add(_cboBanco);
        var sp2 = new StackPanel(); sp2.Children.Add(Lbl("N° CUENTA")); _txtNroCuenta = Txt("000-000000/0"); _txtNroCuenta.Margin = new Thickness(0, 4, 0, 0); sp2.Children.Add(_txtNroCuenta);
        Grid.SetColumn(sp2, 2);
        gr.Children.Add(sp1); gr.Children.Add(sp2);
        sp.Children.Add(gr);

        sp.Children.Add(Lbl("CBU"));
        _txtCBU = Txt("22 dígitos CBU");
        sp.Children.Add(_txtCBU);

        sp.Children.Add(Lbl("SALDO INICIAL"));
        _txtSaldoInicial = Txt("0.00");
        sp.Children.Add(_txtSaldoInicial);

        var btnGr = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        btnGr.ColumnDefinitions.Add(new ColumnDefinition());
        btnGr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var btnC = new Button { Content = "Cancelar", Height = 36 }; btnC.Click += (_, _) => Close();
        var btnO = new Button { Content = "✓  Crear Cuenta", Height = 36, Width = 140,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 40, 60)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 144, 208)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 70, 96)),
            BorderThickness = new Thickness(1) };
        btnO.Click += Guardar_Click;
        Grid.SetColumn(btnO, 1);
        btnGr.Children.Add(btnC); btnGr.Children.Add(btnO);
        sp.Children.Add(btnGr);

        Content = sp;
    }

    private async Task CargarBancos()
    {
        try
        {
            var list = await App.Api.GetBancos();
            if (list != null)
            {
                _cboBanco.ItemsSource = list;
                _cboBanco.DisplayMemberPath = "nombre";
                if (list.Count > 0) _cboBanco.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar bancos: {ex.Message}", "Error");
        }
    }

    private static System.Windows.Controls.TextBlock Lbl(string t) => new()
    { Text = t, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 112, 128)) };

    private static System.Windows.Controls.TextBox Txt(string ph) => new()
    { Height = 34, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)),
      Foreground = System.Windows.Media.Brushes.WhiteSmoke,
      BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58)),
      Margin = new Thickness(0, 4, 0, 10), Padding = new Thickness(8, 6, 8, 6), BorderThickness = new Thickness(1) };

    private static int[] _tipoValues = { 0, 1, 2, 3, 9 };

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtNombre.Text)) { MessageBox.Show("Ingrese un nombre para la cuenta."); return; }

        int tipoSeleccionado = _cboTipo.SelectedIndex;
        if (tipoSeleccionado == 1 || tipoSeleccionado == 2)
        {
            if (_cboBanco.SelectedItem == null)
            {
                MessageBox.Show("Seleccione el banco para la cuenta bancaria.");
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtNroCuenta.Text))
            {
                MessageBox.Show("Ingrese el número de cuenta bancaria.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(_txtCBU.Text))
            {
                string cbu = _txtCBU.Text.Trim();
                bool holdsOnlyDigits = true;
                foreach (char c in cbu)
                {
                    if (c < '0' || c > '9')
                    {
                        holdsOnlyDigits = false;
                        break;
                    }
                }
                if (cbu.Length != 22 || !holdsOnlyDigits)
                {
                    MessageBox.Show("El CBU debe contener exactamente 22 dígitos numéricos.");
                    return;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_txtSaldoInicial.Text) && !decimal.TryParse(_txtSaldoInicial.Text, out _))
        {
            MessageBox.Show("Ingrese un saldo inicial numérico válido (ej: 1000.00).");
            return;
        }

        decimal.TryParse(_txtSaldoInicial.Text, out var saldo);

        try
        {
            string bankName = "";
            if (_cboBanco.SelectedItem != null)
            {
                dynamic bancoSel = _cboBanco.SelectedItem;
                try { bankName = ((System.Text.Json.JsonElement)bancoSel).GetProperty("nombre").GetString() ?? ""; }
                catch { bankName = bancoSel.nombre?.ToString() ?? ""; }
            }

            await App.Api.CrearCuentaTesoreria(new
            {
                Nombre = _txtNombre.Text,
                Tipo = _tipoValues[_cboTipo.SelectedIndex],
                Banco = bankName,
                NroCuenta = _txtNroCuenta.Text,
                CBU = _txtCBU.Text,
                SaldoInicial = saldo,
                Activa = true
            });
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
    }
}
