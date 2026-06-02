using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuperPOS.Client.Views.Tesoreria;

public class NuevaChequeraWindow : Window
{
    private ComboBox _cboCuenta = null!, _cboTipo = null!;
    private TextBox _txtNombre = null!, _txtDesde = null!, _txtHasta = null!;
    private List<dynamic>? _cuentas;

    public NuevaChequeraWindow()
    {
        BuildUI();
        Loaded += async (_, _) => await CargarCuentas();
    }

    private void BuildUI()
    {
        Title = "Registrar Nueva Chequera";
        Width = 480; Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        ResizeMode = ResizeMode.NoResize;

        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(Lbl("CUENTA BANCARIA DE DESTINO *"));
        _cboCuenta = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 10),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)) };
        sp.Children.Add(_cboCuenta);

        sp.Children.Add(Lbl("NOMBRE / DESCRIPCIÓN *"));
        _txtNombre = Txt("Ej: Chequera Banco Nación - Principal");
        sp.Children.Add(_txtNombre);

        var gr = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        gr.ColumnDefinitions.Add(new ColumnDefinition());
        gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        gr.ColumnDefinitions.Add(new ColumnDefinition());

        var sp1 = new StackPanel();
        sp1.Children.Add(Lbl("DESDE N° *"));
        _txtDesde = Txt("00000100"); _txtDesde.Margin = new Thickness(0, 4, 0, 0);
        sp1.Children.Add(_txtDesde);

        var sp2 = new StackPanel();
        sp2.Children.Add(Lbl("HASTA N° *"));
        _txtHasta = Txt("00000150"); _txtHasta.Margin = new Thickness(0, 4, 0, 0);
        sp2.Children.Add(_txtHasta);

        Grid.SetColumn(sp2, 2);
        gr.Children.Add(sp1); gr.Children.Add(sp2);
        sp.Children.Add(gr);

        sp.Children.Add(Lbl("TIPO DE CHEQUERA"));
        _cboTipo = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 15),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)) };
        _cboTipo.Items.Add(new ComboBoxItem { Content = "Tradicional (Cheque común)" });
        _cboTipo.Items.Add(new ComboBoxItem { Content = "Pago Diferido" });
        _cboTipo.SelectedIndex = 0;
        sp.Children.Add(_cboTipo);

        var btnGr = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        btnGr.ColumnDefinitions.Add(new ColumnDefinition());
        btnGr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var btnC = new Button { Content = "Cancelar", Height = 36 }; btnC.Click += (_, _) => Close();
        var btnO = new Button { Content = "✓  Registrar", Height = 36, Width = 140,
            Background = new SolidColorBrush(Color.FromRgb(20, 60, 40)),
            Foreground = new SolidColorBrush(Color.FromRgb(64, 208, 128)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(36, 96, 70)),
            BorderThickness = new Thickness(1) };
        btnO.Click += Guardar_Click;
        Grid.SetColumn(btnO, 1);
        btnGr.Children.Add(btnC); btnGr.Children.Add(btnO);
        sp.Children.Add(btnGr);

        Content = sp;
    }

    private static TextBlock Lbl(string t) => new()
    { Text = t, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(96, 112, 128)) };

    private static TextBox Txt(string ph) => new()
    { Height = 34, Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
      Foreground = Brushes.WhiteSmoke,
      BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
      Margin = new Thickness(0, 4, 0, 10), Padding = new Thickness(8, 6, 8, 6), BorderThickness = new Thickness(1) };

    private async Task CargarCuentas()
    {
        try
        {
            var cuentasAll = await App.Api.GetCuentasTesoreria();
            if (cuentasAll == null) return;

            // Filtrar solo cuentas bancarias (Tipo 1 o 2)
            _cuentas = cuentasAll.Where(c => {
                try
                {
                    int t = ((System.Text.Json.JsonElement)c).GetProperty("tipo").GetInt32();
                    return t == 1 || t == 2;
                }
                catch
                {
                    int t = Convert.ToInt32(c.tipo);
                    return t == 1 || t == 2;
                }
            }).ToList();

            _cboCuenta.DisplayMemberPath = "nombre";
            _cboCuenta.ItemsSource = _cuentas;
            if (_cuentas.Count > 0) _cboCuenta.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar cuentas bancarias: {ex.Message}", "Error");
        }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (_cboCuenta.SelectedItem == null) { MessageBox.Show("Seleccione una cuenta bancaria."); return; }
        if (string.IsNullOrWhiteSpace(_txtNombre.Text)) { MessageBox.Show("Ingrese un nombre descriptivo para la chequera."); return; }
        if (string.IsNullOrWhiteSpace(_txtDesde.Text) || string.IsNullOrWhiteSpace(_txtHasta.Text)) { MessageBox.Show("Complete el rango inicial y final."); return; }

        if (!int.TryParse(_txtDesde.Text, out int start) || !int.TryParse(_txtHasta.Text, out int end) || start < 0 || end < start)
        {
            MessageBox.Show("El rango de números de cheque debe ser numérico y el valor HASTA debe ser mayor o igual a DESDE.");
            return;
        }

        dynamic cuentaSel = _cboCuenta.SelectedItem;
        int idCuenta;
        try { idCuenta = ((System.Text.Json.JsonElement)cuentaSel).GetProperty("id").GetInt32(); }
        catch { idCuenta = Convert.ToInt32(cuentaSel.id); }

        try
        {
            await App.Api.RegistrarChequera(new
            {
                IdCuentaTesoreria = idCuenta,
                Nombre = _txtNombre.Text.Trim(),
                Desde = _txtDesde.Text.Trim(),
                Hasta = _txtHasta.Text.Trim(),
                Tipo = _cboTipo.SelectedIndex,
                Activa = true
            });

            MessageBox.Show("Chequera registrada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error");
        }
    }
}
