using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuperPOS.Client.Views.Tesoreria;

public class ActualizarEstadoChequeWindow : Window
{
    private readonly dynamic _cheque;
    private ComboBox _cboEstado = null!;
    private ComboBox _cboCuentaDestino = null!;
    private System.Windows.Controls.TextBlock _lblCuentaDestino = null!;
    private System.Windows.Controls.TextBox _txtObservaciones = null!;
    private List<dynamic>? _cuentasBancarias;

    public int SelectedEstado { get; private set; }
    public int? SelectedCuentaDestinoId { get; private set; }
    public string? Observaciones { get; private set; }

    public ActualizarEstadoChequeWindow(dynamic cheque)
    {
        _cheque = cheque;
        BuildUI();
        Loaded += async (_, _) => await CargarCuentasBancarias();
    }

    private void BuildUI()
    {
        Title = "Actualizar Estado de Cheque";
        Width = 480;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        ResizeMode = ResizeMode.NoResize;

        var sp = new StackPanel { Margin = new Thickness(20) };

        // Datos del Cheque (Solo lectura)
        string nro = "";
        string banco = "";
        decimal monto = 0;
        try
        {
            var el = (System.Text.Json.JsonElement)_cheque;
            nro = el.GetProperty("nroCheque").GetString() ?? "";
            banco = el.GetProperty("banco").GetString() ?? "";
            monto = el.GetProperty("monto").GetDecimal();
        }
        catch
        {
            nro = _cheque.nroCheque?.ToString() ?? "";
            banco = _cheque.banco?.ToString() ?? "";
            decimal.TryParse(_cheque.monto?.ToString(), out monto);
        }

        var borderInfo = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var gridInfo = new Grid();
        gridInfo.ColumnDefinitions.Add(new ColumnDefinition());
        gridInfo.ColumnDefinitions.Add(new ColumnDefinition());
        gridInfo.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        gridInfo.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var txtNroTitle = Lbl("N° CHEQUE"); gridInfo.Children.Add(txtNroTitle); Grid.SetRow(txtNroTitle, 0); Grid.SetColumn(txtNroTitle, 0);
        var txtNroVal = Val(nro); gridInfo.Children.Add(txtNroVal); Grid.SetRow(txtNroVal, 1); Grid.SetColumn(txtNroVal, 0);

        var txtMontoTitle = Lbl("MONTO"); gridInfo.Children.Add(txtMontoTitle); Grid.SetRow(txtMontoTitle, 0); Grid.SetColumn(txtMontoTitle, 1);
        var txtMontoVal = Val(monto.ToString("$ #,##0.00")); txtMontoVal.FontWeight = FontWeights.Bold; txtMontoVal.Foreground = new SolidColorBrush(Color.FromRgb(64, 192, 128));
        gridInfo.Children.Add(txtMontoVal); Grid.SetRow(txtMontoVal, 1); Grid.SetColumn(txtMontoVal, 1);

        borderInfo.Child = gridInfo;
        sp.Children.Add(borderInfo);

        // Selector de Nuevo Estado
        sp.Children.Add(Lbl("NUEVO ESTADO"));
        _cboEstado = new ComboBox
        {
            Height = 34,
            Margin = new Thickness(0, 4, 0, 12),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42))
        };
        _cboEstado.Items.Add(new ComboBoxItem { Content = "🏦  Depositado en Banco", Tag = 1 });
        _cboEstado.Items.Add(new ComboBoxItem { Content = "💵  Cobrado por Caja", Tag = 2 });
        _cboEstado.Items.Add(new ComboBoxItem { Content = "📦  Entregado a Proveedor", Tag = 3 });
        _cboEstado.Items.Add(new ComboBoxItem { Content = "❌  Rechazado", Tag = 4 });
        _cboEstado.Items.Add(new ComboBoxItem { Content = "🚫  Anulado", Tag = 5 });
        _cboEstado.SelectedIndex = 0;
        _cboEstado.SelectionChanged += Estado_SelectionChanged;
        sp.Children.Add(_cboEstado);

        // Selector de Cuenta de Destino (Solo visible para "Depositado")
        _lblCuentaDestino = Lbl("CUENTA BANCARIA DE DESTINO *");
        sp.Children.Add(_lblCuentaDestino);

        _cboCuentaDestino = new ComboBox
        {
            Height = 34,
            Margin = new Thickness(0, 4, 0, 12),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42))
        };
        sp.Children.Add(_cboCuentaDestino);

        // Observaciones
        sp.Children.Add(Lbl("OBSERVACIONES / MOTIVO"));
        _txtObservaciones = Txt("Ingrese detalles sobre el cambio de estado...");
        sp.Children.Add(_txtObservaciones);

        // Botones de acción
        var btnGr = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        btnGr.ColumnDefinitions.Add(new ColumnDefinition());
        btnGr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var btnC = new Button { Content = "Cancelar", Height = 36 };
        btnC.Click += (_, _) => Close();
        
        var btnO = new Button
        {
            Content = "✓  Confirmar",
            Height = 36,
            Width = 140,
            Background = new SolidColorBrush(Color.FromRgb(20, 50, 30)),
            Foreground = new SolidColorBrush(Color.FromRgb(64, 208, 128)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(36, 80, 48)),
            BorderThickness = new Thickness(1)
        };
        btnO.Click += Confirmar_Click;
        
        Grid.SetColumn(btnO, 1);
        btnGr.Children.Add(btnC);
        btnGr.Children.Add(btnO);
        sp.Children.Add(btnGr);

        Content = sp;
    }

    private static TextBlock Lbl(string t) => new()
    {
        Text = t,
        FontSize = 10,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(96, 112, 128)),
        Margin = new Thickness(0, 0, 0, 2)
    };

    private static TextBlock Val(string t) => new()
    {
        Text = t,
        FontSize = 14,
        Foreground = Brushes.WhiteSmoke,
        Margin = new Thickness(0, 0, 0, 8)
    };

    private static TextBox Txt(string ph) => new()
    {
        Height = 34,
        Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
        Foreground = Brushes.WhiteSmoke,
        BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58)),
        Margin = new Thickness(0, 4, 0, 10),
        Padding = new Thickness(8, 6, 8, 6),
        BorderThickness = new Thickness(1)
    };

    private void Estado_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_lblCuentaDestino == null || _cboCuentaDestino == null) return;
        
        var item = _cboEstado.SelectedItem as ComboBoxItem;
        int tagVal = Convert.ToInt32(item?.Tag);

        if (tagVal == 1) // Depositado
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

    private async Task CargarCuentasBancarias()
    {
        try
        {
            var cuentas = await App.Api.GetCuentasTesoreria();
            if (cuentas != null)
            {
                // Filtrar solo cuentas bancarias (Cta Cte = 1, Caja Ahorro = 2)
                _cuentasBancarias = cuentas.Where(c => {
                    try
                    {
                        var el = (System.Text.Json.JsonElement)c;
                        int tipoVal = el.GetProperty("tipo").GetInt32();
                        return tipoVal == 1 || tipoVal == 2;
                    }
                    catch
                    {
                        int tipoVal = Convert.ToInt32(c.tipo);
                        return tipoVal == 1 || tipoVal == 2;
                    }
                }).ToList();

                _cboCuentaDestino.DisplayMemberPath = "nombre";
                _cboCuentaDestino.ItemsSource = _cuentasBancarias;
                if (_cuentasBancarias.Count > 0)
                {
                    _cboCuentaDestino.SelectedIndex = 0;
                }
            }
        }
        catch { }
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        var item = _cboEstado.SelectedItem as ComboBoxItem;
        SelectedEstado = Convert.ToInt32(item?.Tag);

        if (SelectedEstado == 1) // Depositado
        {
            if (_cboCuentaDestino.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una cuenta bancaria de destino para el depósito.");
                return;
            }
            dynamic cuentaSel = _cboCuentaDestino.SelectedItem;
            try
            {
                var el = (System.Text.Json.JsonElement)cuentaSel;
                SelectedCuentaDestinoId = el.GetProperty("id").GetInt32();
            }
            catch
            {
                SelectedCuentaDestinoId = Convert.ToInt32(cuentaSel.id);
            }
        }
        else
        {
            SelectedCuentaDestinoId = null;
        }

        Observaciones = _txtObservaciones.Text;
        DialogResult = true;
        Close();
    }
}
