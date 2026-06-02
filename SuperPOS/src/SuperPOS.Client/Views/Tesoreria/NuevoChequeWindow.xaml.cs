using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SuperPOS.Client.Services;

namespace SuperPOS.Client.Views.Tesoreria;

public partial class NuevoChequeWindow : Window
{
    private ComboBox _cboTipo = null!, _cboCuentaBanco = null!, _cboChequera = null!, _cboBanco = null!;
    private TextBox _txtNro = null!, _txtMonto = null!, _txtLibrador = null!, _txtDias = null!;
    private TextBlock _lblBanco = null!, _lblLibrador = null!, _lblCuentaBanco = null!, _lblChequera = null!;
    private DatePicker _dtpEmision = null!, _dtpPago = null!;
    
    private bool _calculando = false;
    private bool _cargandoCuentas = false;
    private List<dynamic>? _cuentasBancos;
    private List<dynamic>? _chequeras;
    private List<dynamic>? _bancos;

    public NuevoChequeWindow()
    {
        BuildUI();
        Loaded += async (_, _) =>
        {
            await CargarBancos();
            await CargarCuentasBancarias();
        };
    }

    private void BuildUI()
    {
        Title = "Registrar Cheque";
        Width = 500; Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        ResizeMode = ResizeMode.NoResize;

        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(Lbl("TIPO DE CHEQUE"));
        _cboTipo = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 10),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)) };
        _cboTipo.Items.Add(new ComboBoxItem { Content = "Recibido (de cliente)" });
        _cboTipo.Items.Add(new ComboBoxItem { Content = "Emitido (a proveedor)" });
        _cboTipo.SelectedIndex = 0;
        _cboTipo.SelectionChanged += Tipo_SelectionChanged;
        sp.Children.Add(_cboTipo);

        // Controles Propios (Bancos y Chequeras)
        _lblCuentaBanco = Lbl("CUENTA BANCARIA DE ORIGEN *");
        _lblCuentaBanco.Visibility = Visibility.Collapsed;
        sp.Children.Add(_lblCuentaBanco);

        _cboCuentaBanco = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 10),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
            Visibility = Visibility.Collapsed };
        _cboCuentaBanco.SelectionChanged += CuentaBanco_SelectionChanged;
        sp.Children.Add(_cboCuentaBanco);

        _lblChequera = Lbl("CHEQUERA A UTILIZAR *");
        _lblChequera.Visibility = Visibility.Collapsed;
        sp.Children.Add(_lblChequera);

        _cboChequera = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 10),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
            Visibility = Visibility.Collapsed };
        _cboChequera.SelectionChanged += Chequera_SelectionChanged;
        sp.Children.Add(_cboChequera);

        var gr = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        gr.ColumnDefinitions.Add(new ColumnDefinition());
        gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        gr.ColumnDefinitions.Add(new ColumnDefinition());

        var sp1 = new StackPanel();
        sp1.Children.Add(Lbl("N° CHEQUE *"));
        _txtNro = Txt("0000-12345678"); _txtNro.Margin = new Thickness(0, 4, 0, 0);
        sp1.Children.Add(_txtNro);

        var sp2 = new StackPanel();
        _lblBanco = Lbl("BANCO *");
        sp2.Children.Add(_lblBanco);
        _cboBanco = new ComboBox { Height = 34, Margin = new Thickness(0, 4, 0, 0),
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)) };
        sp2.Children.Add(_cboBanco);

        Grid.SetColumn(sp2, 2);
        gr.Children.Add(sp1); gr.Children.Add(sp2);
        sp.Children.Add(gr);

        var gr2 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        gr2.ColumnDefinitions.Add(new ColumnDefinition()); gr2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); gr2.ColumnDefinitions.Add(new ColumnDefinition());
        var sp3 = new StackPanel(); sp3.Children.Add(Lbl("MONTO *")); _txtMonto = Txt("0.00"); _txtMonto.Margin = new Thickness(0, 4, 0, 0); sp3.Children.Add(_txtMonto);
        var sp4 = new StackPanel(); _lblLibrador = Lbl("LIBRADOR / BENEFICIARIO"); sp4.Children.Add(_lblLibrador); _txtLibrador = Txt("Quien firma o a quién se emite"); _txtLibrador.Margin = new Thickness(0, 4, 0, 0); sp4.Children.Add(_txtLibrador);
        Grid.SetColumn(sp4, 2);
        gr2.Children.Add(sp3); gr2.Children.Add(sp4);
        sp.Children.Add(gr2);

        var gr3 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        gr3.ColumnDefinitions.Add(new ColumnDefinition());
        gr3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        gr3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        gr3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        gr3.ColumnDefinitions.Add(new ColumnDefinition());

        var sp5 = new StackPanel();
        sp5.Children.Add(Lbl("FECHA EMISIÓN"));
        _dtpEmision = new DatePicker { Height = 34, SelectedDate = DateTime.Today, Margin = new Thickness(0, 4, 0, 0) };
        sp5.Children.Add(_dtpEmision);

        var spDias = new StackPanel();
        spDias.Children.Add(Lbl("DÍAS"));
        _txtDias = Txt("30");
        _txtDias.Margin = new Thickness(0, 4, 0, 0);
        _txtDias.Text = "30";
        _txtDias.TextChanged += Dias_TextChanged;
        spDias.Children.Add(_txtDias);

        var sp6 = new StackPanel();
        sp6.Children.Add(Lbl("FECHA PAGO/COBRO"));
        _dtpPago = new DatePicker { Height = 34, SelectedDate = DateTime.Today.AddDays(30), Margin = new Thickness(0, 4, 0, 0) };
        _dtpPago.SelectedDateChanged += Pago_SelectedDateChanged;
        sp6.Children.Add(_dtpPago);

        Grid.SetColumn(sp5, 0);
        Grid.SetColumn(spDias, 2);
        Grid.SetColumn(sp6, 4);

        gr3.Children.Add(sp5);
        gr3.Children.Add(spDias);
        gr3.Children.Add(sp6);
        sp.Children.Add(gr3);

        var btnGr = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        btnGr.ColumnDefinitions.Add(new ColumnDefinition());
        btnGr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var btnC = new Button { Content = "Cancelar", Height = 36 };
        btnC.Click += (_, _) => Close();
        var btnO = new Button { Content = "✓  Registrar Cheque", Height = 36, Width = 160,
            Background = new SolidColorBrush(Color.FromRgb(20, 40, 20)),
            Foreground = new SolidColorBrush(Color.FromRgb(64, 192, 96)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(36, 70, 36)),
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

    private void Tipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_lblCuentaBanco == null) return;

        if (_cboTipo.SelectedIndex == 1) // Emitido
        {
            _lblCuentaBanco.Visibility = Visibility.Visible;
            _cboCuentaBanco.Visibility = Visibility.Visible;
            _lblChequera.Visibility = Visibility.Visible;
            _cboChequera.Visibility = Visibility.Visible;
            
            // Ocultar campo de selección Banco, ya que se pre-asigna
            _cboBanco.Visibility = Visibility.Collapsed;
            _lblBanco.Visibility = Visibility.Collapsed;
            
            Height = 560;
        }
        else // Recibido
        {
            _lblCuentaBanco.Visibility = Visibility.Collapsed;
            _cboCuentaBanco.Visibility = Visibility.Collapsed;
            _lblChequera.Visibility = Visibility.Collapsed;
            _cboChequera.Visibility = Visibility.Collapsed;
            
            _cboBanco.Visibility = Visibility.Visible;
            _lblBanco.Visibility = Visibility.Visible;
            
            Height = 480;
        }
    }

    private async Task CargarCuentasBancarias()
    {
        _cargandoCuentas = true;
        try
        {
            var cuentasAll = await App.Api.GetCuentasTesoreria();
            if (cuentasAll == null) return;

            // Filtrar solo cuentas bancarias (Tipo 1 o 2)
            _cuentasBancos = cuentasAll.Where(c => {
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

            _cboCuentaBanco.DisplayMemberPath = "nombre";
            _cboCuentaBanco.ItemsSource = _cuentasBancos;
            if (_cuentasBancos.Count > 0) _cboCuentaBanco.SelectedIndex = 0;
        }
        catch { }
        finally
        {
            _cargandoCuentas = false;
        }
    }

    private async void CuentaBanco_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargandoCuentas || _cboCuentaBanco.SelectedItem == null) return;

        dynamic cuentaSel = _cboCuentaBanco.SelectedItem;
        int idCuenta;
        string bancoNombre;
        try
        {
            var el = (System.Text.Json.JsonElement)cuentaSel;
            idCuenta = el.GetProperty("id").GetInt32();
            bancoNombre = el.GetProperty("banco").GetString() ?? "";
        }
        catch
        {
            idCuenta = Convert.ToInt32(cuentaSel.id);
            bancoNombre = cuentaSel.banco?.ToString() ?? "";
        }

        if (_bancos != null && !string.IsNullOrEmpty(bancoNombre))
        {
            var match = _bancos.FirstOrDefault(b => {
                string name;
                try { name = ((System.Text.Json.JsonElement)b).GetProperty("nombre").GetString() ?? ""; }
                catch { name = b.nombre?.ToString() ?? ""; }
                return string.Equals(name, bancoNombre, StringComparison.OrdinalIgnoreCase);
            });
            if (match != null)
            {
                _cboBanco.SelectedItem = match;
            }
        }

        try
        {
            // Cargar chequeras activas para esa cuenta bancaria
            var chequerasRaw = await App.Api.GetChequerasPorCuenta(idCuenta);
            _chequeras = chequerasRaw;
            
            _cboChequera.ItemsSource = _chequeras;
            _cboChequera.DisplayMemberPath = "nombre";
            
            if (_chequeras != null && _chequeras.Count > 0)
            {
                _cboChequera.SelectedIndex = 0;
            }
            else
            {
                _cboChequera.ItemsSource = null;
                _txtNro.Text = "";
            }
        }
        catch { }
    }

    private async void Chequera_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cboChequera.SelectedItem == null) return;

        dynamic chequeraSel = _cboChequera.SelectedItem;
        int idChequera;
        try { idChequera = ((System.Text.Json.JsonElement)chequeraSel).GetProperty("id").GetInt32(); }
        catch { idChequera = Convert.ToInt32(chequeraSel.id); }

        try
        {
            var nrosDisponibles = await App.Api.GetNumerosDisponiblesChequera(idChequera);
            if (nrosDisponibles != null && nrosDisponibles.Count > 0)
            {
                _txtNro.Text = nrosDisponibles.First();
            }
            else
            {
                _txtNro.Text = "";
            }
        }
        catch { }
    }

    private async Task CargarBancos()
    {
        try
        {
            var list = await App.Api.GetBancos();
            _bancos = list;
            _cboBanco.ItemsSource = _bancos;
            _cboBanco.DisplayMemberPath = "nombre";
            if (_bancos != null && _bancos.Count > 0)
            {
                _cboBanco.SelectedIndex = 0;
            }
        }
        catch { }
    }

    private void Dias_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_calculando) return;
        _calculando = true;
        try
        {
            if (int.TryParse(_txtDias.Text, out var dias))
            {
                _dtpPago.SelectedDate = DateTime.Today.AddDays(dias);
            }
        }
        finally
        {
            _calculando = false;
        }
    }

    private void Pago_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_calculando) return;
        _calculando = true;
        try
        {
            if (_dtpPago.SelectedDate.HasValue)
            {
                var diff = (_dtpPago.SelectedDate.Value.Date - DateTime.Today).Days;
                _txtDias.Text = diff.ToString();
            }
        }
        finally
        {
            _calculando = false;
        }
    }

    private async void Guardar_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtNro.Text) || !decimal.TryParse(_txtMonto.Text, out var monto) || monto <= 0)
        {
            MessageBox.Show("Complete el número de cheque y un monto válido.");
            return;
        }

        int? idCuenta = null;
        int? idChequera = null;

        if (_cboTipo.SelectedIndex == 1) // Emitido
        {
            if (_cboCuentaBanco.SelectedItem == null)
            {
                MessageBox.Show("Seleccione la cuenta bancaria de origen.");
                return;
            }
            if (_cboChequera.SelectedItem == null)
            {
                MessageBox.Show("Seleccione la chequera de la cual emitir el cheque.");
                return;
            }

            dynamic cuentaSel = _cboCuentaBanco.SelectedItem;
            try { idCuenta = ((System.Text.Json.JsonElement)cuentaSel).GetProperty("id").GetInt32(); }
            catch { idCuenta = Convert.ToInt32(cuentaSel.id); }

            dynamic chequeraSel = _cboChequera.SelectedItem;
            try { idChequera = ((System.Text.Json.JsonElement)chequeraSel).GetProperty("id").GetInt32(); }
            catch { idChequera = Convert.ToInt32(chequeraSel.id); }
        }

        if (_cboTipo.SelectedIndex == 0 && _cboBanco.SelectedItem == null)
        {
            MessageBox.Show("Seleccione el banco emisor.");
            return;
        }

        string bankName = "";
        if (_cboBanco.SelectedItem != null)
        {
            dynamic bancoSel = _cboBanco.SelectedItem;
            try { bankName = ((System.Text.Json.JsonElement)bancoSel).GetProperty("nombre").GetString() ?? ""; }
            catch { bankName = bancoSel.nombre?.ToString() ?? ""; }
        }
        else if (_cboTipo.SelectedIndex == 1 && _cboCuentaBanco.SelectedItem != null)
        {
            dynamic cuentaSel = _cboCuentaBanco.SelectedItem;
            try { bankName = ((System.Text.Json.JsonElement)cuentaSel).GetProperty("banco").GetString() ?? ""; }
            catch { bankName = cuentaSel.banco?.ToString() ?? ""; }
        }

        try
        {
            await App.Api.RegistrarCheque(new
            {
                Tipo = _cboTipo.SelectedIndex,
                NroCheque = _txtNro.Text.Trim(),
                Banco = bankName.Trim(),
                Monto = monto,
                Librador = _txtLibrador.Text.Trim(),
                FechaEmision = _dtpEmision.SelectedDate?.ToUniversalTime() ?? DateTime.UtcNow,
                FechaPago = _dtpPago.SelectedDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(30),
                IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
                IdCuenta = idCuenta,
                IdChequera = idChequera,
                Estado = _cboTipo.SelectedIndex == 1 ? 3 : 0 // Emitido -> Entregado (3), Recibido -> Cartera (0)
            });

            MessageBox.Show("Cheque registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar cheque: {ex.Message}", "Error");
        }
    }
}
