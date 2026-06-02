using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuperPOS.Client.Views.Tesoreria;

public partial class TesoreriaPage : Page
{
    public TesoreriaPage()
    {
        InitializeComponent();
        DtpDesde.SelectedDate = DateTime.Today.AddDays(-30);
        DtpHasta.SelectedDate = DateTime.Today;
        DtpGastoDesde.SelectedDate = DateTime.Today.AddDays(-30);
        DtpGastoHasta.SelectedDate = DateTime.Today;
        DtpConciliaDesde.SelectedDate = DateTime.Today.AddDays(-30);
        DtpConciliaHasta.SelectedDate = DateTime.Today;
        DtpDepositoFecha.SelectedDate = DateTime.Today;

        // Inicializar nuevas fechas
        DtpRepChequeDesde.SelectedDate = DateTime.Today.AddDays(-30);
        DtpRepChequeHasta.SelectedDate = DateTime.Today;
        DtpRepDepDesde.SelectedDate = DateTime.Today.AddDays(-30);
        DtpRepDepHasta.SelectedDate = DateTime.Today;
        DtpProyFecha.SelectedDate = DateTime.Today.AddDays(30);

        Loaded += async (_, _) => await CargarTodo();
    }

    private async Task CargarTodo()
    {
        await Task.WhenAll(CargarSaldos(), CargarCuentas(), CargarMovimientos(), CargarCheques(), CargarGastos(), CargarChequesCartera(), CargarChequeras(), CargarBancosFiltro());
    }

    private async Task CargarSaldos()
    {
        try
        {
            var saldos = await App.Api.GetTesoreriaSaldos();
            if (saldos is null) return;
            var s = saldos.Value;
            TxtSaldoEfectivo.Text = s.GetProperty("totalEfectivo").GetDecimal().ToString("$ #,##0.00");
            TxtSaldoBancos.Text   = s.GetProperty("totalBancos").GetDecimal().ToString("$ #,##0.00");
            TxtSaldoTotal.Text    = s.GetProperty("totalGeneral").GetDecimal().ToString("$ #,##0.00");
        }
        catch { }
    }

    private async Task CargarCuentas()
    {
        try
        {
            var cuentas = await App.Api.GetCuentasTesoreria();
            DgCuentas.ItemsSource = cuentas;

            // Llenar filtro de cuentas en movimientos
            var lista = new List<dynamic?> { null };
            if (cuentas != null) lista.AddRange(cuentas.Cast<dynamic?>());
            CboFiltrosCuenta.ItemsSource = lista;
            CboFiltrosCuenta.DisplayMemberPath = "nombre";
            CboFiltrosCuenta.SelectedIndex = 0;

            // Llenar filtro de cuentas en conciliación (solo bancos, tipo 1 o 2)
            if (cuentas != null)
            {
                var bancos = cuentas.Where(c => {
                    try
                    {
                        var el = (System.Text.Json.JsonElement)c;
                        int t = el.GetProperty("tipo").GetInt32();
                        return t == 1 || t == 2;
                    }
                    catch
                    {
                        int t = Convert.ToInt32(c.tipo);
                        return t == 1 || t == 2;
                    }
                }).ToList();
                CboConciliaCuenta.ItemsSource = bancos;
                CboConciliaCuenta.DisplayMemberPath = "nombre";
                if (bancos.Count > 0) CboConciliaCuenta.SelectedIndex = 0;

                // Llenar filtros de depósito (Banco y Caja)
                CboDepositoBanco.ItemsSource = bancos;
                CboDepositoBanco.DisplayMemberPath = "nombre";
                if (bancos.Count > 0) CboDepositoBanco.SelectedIndex = 0;

                // Llenar filtros de depósito en reportes
                var bancosConTodos = new List<dynamic?> { null };
                bancosConTodos.AddRange(bancos.Cast<dynamic?>());
                CboRepDepCuenta.ItemsSource = bancosConTodos;
                CboRepDepCuenta.DisplayMemberPath = "nombre";
                CboRepDepCuenta.SelectedIndex = 0;

                // Llenar filtro de cuenta en proyección
                CboProyCuenta.ItemsSource = bancos;
                CboProyCuenta.DisplayMemberPath = "nombre";
                if (bancos.Count > 0) CboProyCuenta.SelectedIndex = 0;

                var cajas = cuentas.Where(c => {
                    try
                    {
                        var el = (System.Text.Json.JsonElement)c;
                        int t = el.GetProperty("tipo").GetInt32();
                        return t == 0;
                    }
                    catch
                    {
                        int t = Convert.ToInt32(c.tipo);
                        return t == 0;
                    }
                }).ToList();
                CboDepositoCaja.ItemsSource = cajas;
                CboDepositoCaja.DisplayMemberPath = "nombre";
                if (cajas.Count > 0) CboDepositoCaja.SelectedIndex = 0;
            }
        }
        catch { }
    }

    private async Task CargarMovimientos()
    {
        try
        {
            var desde = DtpDesde.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
            var hasta = DtpHasta.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var movs = await App.Api.GetMovimientosTesoreria(desde, hasta);
            DgMovimientos.ItemsSource = movs;
        }
        catch { }
    }

    private async Task CargarCheques()
    {
        try
        {
            var cheques = await App.Api.GetCheques();
            DgCheques.ItemsSource = cheques;
            var total = cheques?.Sum(c =>
            {
                try { return Convert.ToDecimal(((System.Text.Json.JsonElement)c).GetProperty("monto").GetDecimal()); }
                catch { return 0m; }
            }) ?? 0;
            TxtTotalCheques.Text = $"Total en cartera: {total:$ #,##0.00}";
        }
        catch { }
    }

    private async Task CargarGastos()
    {
        try
        {
            var gastos = await App.Api.GetGastosCaja();
            DgGastos.ItemsSource = gastos;
        }
        catch { }
    }

    private async void FiltroMovimientosChanged(object sender, SelectionChangedEventArgs e) => await CargarMovimientos();
    private async void FiltroGastosChanged(object sender, SelectionChangedEventArgs e) => await CargarGastos();
    private async void FiltroChequeChanged(object sender, SelectionChangedEventArgs e) => await CargarCheques();
    private async void BtnRefreshMov_Click(object sender, RoutedEventArgs e) => await CargarMovimientos();

    private void BtnNuevoMovimiento_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevoMovimientoWindow();
        if (dlg.ShowDialog() == true) _ = CargarTodo();
    }

    private void BtnNuevoGasto_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevoGastoWindow();
        if (dlg.ShowDialog() == true) _ = CargarGastos();
    }

    private void BtnNuevoCheque_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevoChequeWindow();
        if (dlg.ShowDialog() == true) _ = CargarCheques();
    }

    private void BtnBtnNuevaChequera_Click(object sender, RoutedEventArgs e)
    {
        // We will call it BtnNuevaChequera_Click in XAML, wait, the button says Click="BtnNuevaChequera_Click"
        // Let's name it BtnNuevaChequera_Click!
    }

    private void BtnNuevaChequera_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevaChequeraWindow();
        if (dlg.ShowDialog() == true) _ = CargarTodo();
    }

    private void BtnNuevaCuenta_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NuevaCuentaWindow();
        if (dlg.ShowDialog() == true) _ = CargarTodo();
    }

    private async void DgCheques_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DgCheques.SelectedItem is null) return;
        var cheque = DgCheques.SelectedItem;
        
        var dlg = new ActualizarEstadoChequeWindow(cheque);
        if (dlg.ShowDialog() == true)
        {
            try
            {
                int id = 0;
                try
                {
                    var el = (System.Text.Json.JsonElement)cheque;
                    id = el.GetProperty("id").GetInt32();
                }
                catch
                {
                    id = Convert.ToInt32(((dynamic)cheque).id);
                }

                await App.Api.ActualizarEstadoCheque(id, dlg.SelectedEstado, dlg.SelectedCuentaDestinoId, dlg.Observaciones);
                await CargarTodo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el estado del cheque: {ex.Message}", "Error");
            }
        }
    }

    // ═══════════════════════════════════════════
    // CONCILIACIÓN BANCARIA
    // ═══════════════════════════════════════════
    private List<MovimientoConciliacionModel>? _movimientosConciliacion;
    private decimal _saldoActualCuentaConcilia = 0;
    private decimal _saldoInicialConciliado = 0;

    // Depósitos
    private List<ChequeSeleccionModel>? _chequesCartera;

    private async void BtnConciliaCargar_Click(object sender, RoutedEventArgs e)
    {
        await CargarDatosConciliacion();
    }

    private async Task CargarDatosConciliacion()
    {
        if (CboConciliaCuenta.SelectedItem == null)
        {
            MessageBox.Show("Seleccione una cuenta bancaria.", "Conciliación");
            return;
        }

        dynamic cuentaSel = CboConciliaCuenta.SelectedItem;
        int idCuenta;
        try
        {
            var el = (System.Text.Json.JsonElement)cuentaSel;
            idCuenta = el.GetProperty("id").GetInt32();
            _saldoActualCuentaConcilia = el.GetProperty("saldoActual").GetDecimal();
        }
        catch
        {
            idCuenta = Convert.ToInt32(cuentaSel.id);
            decimal.TryParse(cuentaSel.saldoActual?.ToString(), out _saldoActualCuentaConcilia);
        }

        try
        {
            var desde = DtpConciliaDesde.SelectedDate;
            var hasta = DtpConciliaHasta.SelectedDate;
            var movs = await App.Api.GetMovimientosDeCuenta(idCuenta);

            if (movs == null)
            {
                DgConciliacion.ItemsSource = null;
                return;
            }

            // Mapear movimientos
            var listAll = movs.Select(m => {
                var model = new MovimientoConciliacionModel();
                try
                {
                    var el = (System.Text.Json.JsonElement)m;
                    model.Id = el.GetProperty("id").GetInt32();
                    model.Fecha = el.GetProperty("fecha").GetDateTime().ToLocalTime();
                    model.Concepto = el.GetProperty("concepto").GetString() ?? "";
                    model.NroDocumento = el.TryGetProperty("nroDocumento", out var nd) ? nd.GetString() : null;
                    model.Beneficiario = el.TryGetProperty("beneficiario", out var bn) ? bn.GetString() : null;
                    model.Conciliado = el.GetProperty("conciliado").GetBoolean();
                    model.OriginalConciliado = model.Conciliado;
                    
                    decimal monto = el.GetProperty("monto").GetDecimal();
                    int tipo = el.GetProperty("tipo").GetInt32();
                    
                    if (tipo == 0 || tipo == 3) // Ingreso o Ajuste Positivo
                    {
                        model.Debe = monto;
                    }
                    else if (tipo == 1 || tipo == 4) // Egreso o Ajuste Negativo
                    {
                        model.Haber = monto;
                    }
                    else if (tipo == 2) // Transferencia
                    {
                        int idOrigen = el.GetProperty("idCuenta").GetInt32();
                        if (idOrigen == idCuenta)
                            model.Haber = monto;
                        else
                            model.Debe = monto;
                    }
                }
                catch
                {
                    model.Id = Convert.ToInt32(m.id);
                    model.Fecha = Convert.ToDateTime(m.fecha).ToLocalTime();
                    model.Concepto = m.concepto?.ToString() ?? "";
                    model.NroDocumento = m.nroDocumento?.ToString();
                    model.Beneficiario = m.beneficiario?.ToString();
                    model.Conciliado = Convert.ToBoolean(m.conciliado);
                    model.OriginalConciliado = model.Conciliado;
                    
                    decimal monto = Convert.ToDecimal(m.monto);
                    int tipo = Convert.ToInt32(m.tipo);
                    
                    if (tipo == 0 || tipo == 3)
                    {
                        model.Debe = monto;
                    }
                    else if (tipo == 1 || tipo == 4)
                    {
                        model.Haber = monto;
                    }
                    else if (tipo == 2)
                    {
                        int idOrigen = Convert.ToInt32(m.idCuenta);
                        if (idOrigen == idCuenta)
                            model.Haber = monto;
                        else
                            model.Debe = monto;
                    }
                }
                return model;
            }).ToList();

            // Calcular Saldo Inicial Conciliado
            decimal noConciliadoIngresos = listAll.Where(m => !m.OriginalConciliado).Sum(m => m.Debe);
            decimal noConciliadoEgresos = listAll.Where(m => !m.OriginalConciliado).Sum(m => m.Haber);
            _saldoInicialConciliado = _saldoActualCuentaConcilia - noConciliadoIngresos + noConciliadoEgresos;

            // Filtrar movimientos para mostrar en la grilla
            _movimientosConciliacion = listAll.Where(m => 
                !m.OriginalConciliado || 
                (m.OriginalConciliado && m.Fecha.Date >= desde?.Date && m.Fecha.Date <= hasta?.Date)
            ).OrderByDescending(m => m.Fecha).ToList();

            DgConciliacion.ItemsSource = _movimientosConciliacion;

            RecalcularConciliacion();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar movimientos: {ex.Message}", "Conciliación");
        }
    }

    private void ChkConciliado_Click(object sender, RoutedEventArgs e)
    {
        RecalcularConciliacion();
    }

    private void TxtConciliaExtracto_TextChanged(object sender, TextChangedEventArgs e)
    {
        RecalcularConciliacion();
    }

    private void RecalcularConciliacion()
    {
        if (_movimientosConciliacion == null) return;

        decimal ingresosTildados = _movimientosConciliacion.Where(m => m.Conciliado).Sum(m => m.Debe);
        decimal egresosTildados = _movimientosConciliacion.Where(m => m.Conciliado).Sum(m => m.Haber);

        decimal saldoCalculado = _saldoInicialConciliado + ingresosTildados - egresosTildados;

        decimal.TryParse(TxtConciliaExtracto.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var extracto);

        decimal diferencia = saldoCalculado - extracto;

        TxtConciliaSaldoInic.Text = _saldoInicialConciliado.ToString("$ #,##0.00");
        TxtConciliaIngresos.Text = ingresosTildados.ToString("$ #,##0.00");
        TxtConciliaEgresos.Text = egresosTildados.ToString("$ #,##0.00");
        TxtConciliaCalculado.Text = saldoCalculado.ToString("$ #,##0.00");
        
        TxtConciliaDiferencia.Text = diferencia.ToString("$ #,##0.00");
        if (diferencia == 0)
        {
            TxtConciliaDiferencia.Foreground = new SolidColorBrush(Color.FromRgb(64, 208, 128)); // Verde
        }
        else
        {
            TxtConciliaDiferencia.Foreground = new SolidColorBrush(Color.FromRgb(220, 80, 80)); // Rojo
        }
    }

    private async void BtnConciliaGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (_movimientosConciliacion == null || _movimientosConciliacion.Count == 0) return;

        var changed = _movimientosConciliacion.Where(m => m.IsChanged).ToList();
        if (changed.Count == 0)
        {
            MessageBox.Show("No se detectaron cambios para guardar.", "Conciliación");
            return;
        }

        decimal.TryParse(TxtConciliaExtracto.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var extracto);
        decimal ingresosTildados = _movimientosConciliacion.Where(m => m.Conciliado).Sum(m => m.Debe);
        decimal egresosTildados = _movimientosConciliacion.Where(m => m.Conciliado).Sum(m => m.Haber);
        decimal saldoCalculado = _saldoInicialConciliado + ingresosTildados - egresosTildados;
        
        if (saldoCalculado - extracto != 0)
        {
            if (MessageBox.Show("Hay una diferencia entre el saldo calculado y el saldo de extracto bancario. ¿Desea guardar la conciliación de todos modos?", "Confirmar Guardado", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            var req = changed.Select(m => new { IdMovimiento = m.Id, Conciliado = m.Conciliado }).Cast<object>().ToList();
            await App.Api.ConciliarMovimientos(req);

            MessageBox.Show("Conciliación guardada correctamente.", "Conciliación", MessageBoxButton.OK, MessageBoxImage.Information);
            
            await CargarTodo();
            await CargarDatosConciliacion();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar conciliación: {ex.Message}", "Error");
        }
    }

    // ═══════════════════════════════════════════
    // DEPÓSITOS BANCARIOS
    // ═══════════════════════════════════════════
    private async Task CargarChequesCartera()
    {
        try
        {
            var rawCheques = await App.Api.GetChequesEnCartera();
            if (rawCheques == null)
            {
                DgDepositoCheques.ItemsSource = null;
                _chequesCartera = null;
                RecalcularDeposito();
                return;
            }

            _chequesCartera = rawCheques.Select(c => {
                var model = new ChequeSeleccionModel();
                try
                {
                    var el = (System.Text.Json.JsonElement)c;
                    model.Id = el.GetProperty("id").GetInt32();
                    model.NroCheque = el.GetProperty("nroCheque").GetString() ?? "";
                    model.Banco = el.GetProperty("banco").GetString() ?? "";
                    model.FechaPago = el.GetProperty("fechaPago").GetDateTime().ToLocalTime();
                    model.Librador = el.TryGetProperty("librador", out var lib) ? (lib.GetString() ?? "") : "";
                    model.Monto = el.GetProperty("monto").GetDecimal();
                }
                catch
                {
                    model.Id = Convert.ToInt32(c.id);
                    model.NroCheque = c.nroCheque?.ToString() ?? "";
                    model.Banco = c.banco?.ToString() ?? "";
                    model.FechaPago = Convert.ToDateTime(c.fechaPago).ToLocalTime();
                    model.Librador = c.librador?.ToString() ?? "";
                    model.Monto = Convert.ToDecimal(c.monto);
                }
                model.Seleccionado = false;
                return model;
            }).ToList();

            DgDepositoCheques.ItemsSource = _chequesCartera;
            RecalcularDeposito();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar cheques en cartera: {ex.Message}", "Depósitos");
        }
    }

    private void ChkDepositoCheque_Click(object sender, RoutedEventArgs e)
    {
        RecalcularDeposito();
    }

    private void TxtDepositoEfectivo_TextChanged(object sender, TextChangedEventArgs e)
    {
        RecalcularDeposito();
    }

    private void RecalcularDeposito()
    {
        decimal totalCheques = _chequesCartera?.Where(c => c.Seleccionado).Sum(c => c.Monto) ?? 0m;
        
        string txt = TxtDepositoEfectivo?.Text?.Replace(',', '.') ?? "0";
        decimal.TryParse(txt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal efectivo);

        if (TxtDepTotalCheques != null) TxtDepTotalCheques.Text = totalCheques.ToString("$ #,##0.00");
        if (TxtDepTotalEfectivo != null) TxtDepTotalEfectivo.Text = efectivo.ToString("$ #,##0.00");
        if (TxtDepTotalDeposito != null) TxtDepTotalDeposito.Text = (totalCheques + efectivo).ToString("$ #,##0.00");
    }

    private async void BtnDepositoGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (CboDepositoBanco.SelectedItem == null)
        {
            MessageBox.Show("Seleccione el banco de destino.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtDepositoComprobante.Text))
        {
            MessageBox.Show("Ingrese el número de comprobante/boleta de depósito.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DtpDepositoFecha.SelectedDate.HasValue)
        {
            MessageBox.Show("Seleccione la fecha del depósito.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string txt = TxtDepositoEfectivo.Text.Replace(',', '.');
        decimal.TryParse(txt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal efectivo);

        int? idCuentaOrigen = null;
        if (efectivo > 0)
        {
            if (CboDepositoCaja.SelectedItem == null)
            {
                MessageBox.Show("Seleccione la caja de origen para el depósito de efectivo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic cajaSel = CboDepositoCaja.SelectedItem;
            try { idCuentaOrigen = ((System.Text.Json.JsonElement)cajaSel).GetProperty("id").GetInt32(); }
            catch { idCuentaOrigen = Convert.ToInt32(cajaSel.id); }
        }

        dynamic bancoSel = CboDepositoBanco.SelectedItem;
        int idCuentaDestino;
        try { idCuentaDestino = ((System.Text.Json.JsonElement)bancoSel).GetProperty("id").GetInt32(); }
        catch { idCuentaDestino = Convert.ToInt32(bancoSel.id); }

        var selectedChequesIds = _chequesCartera?.Where(c => c.Seleccionado).Select(c => c.Id).ToList() ?? new List<int>();

        if (efectivo == 0 && selectedChequesIds.Count == 0)
        {
            MessageBox.Show("El depósito debe contener al menos un cheque seleccionado o un importe en efectivo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var req = new
        {
            IdCuentaDestino = idCuentaDestino,
            IdCuentaOrigen = idCuentaOrigen,
            NroComprobante = TxtDepositoComprobante.Text.Trim(),
            Fecha = DtpDepositoFecha.SelectedDate.Value,
            MontoEfectivo = efectivo,
            ChequesIds = selectedChequesIds,
            Observaciones = TxtDepositoObservaciones.Text.Trim(),
            IdUsuario = 1
        };

        try
        {
            await App.Api.RegistrarDeposito(req);
            MessageBox.Show("Depósito registrado y saldos actualizados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            // Limpiar campos
            TxtDepositoComprobante.Text = "";
            TxtDepositoEfectivo.Text = "0.00";
            TxtDepositoObservaciones.Text = "";
            DtpDepositoFecha.SelectedDate = DateTime.Today;

            // Recargar datos de todas las pestañas
            await CargarTodo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar depósito: {ex.Message}", "Error");
        }
    }

    private async Task CargarChequeras()
    {
        try
        {
            var chequeras = await App.Api.GetChequeras();
            DgChequeras.ItemsSource = chequeras;
        }
        catch { }
    }

    // ═══════════════════════════════════════════
    // REPORTES Y PROYECCIÓN FINANCIERA
    // ═══════════════════════════════════════════
    private async Task CargarBancosFiltro()
    {
        try
        {
            var bancos = await App.Api.GetBancos();
            var list = new List<dynamic?> { null };
            if (bancos != null) list.AddRange(bancos.Cast<dynamic?>());
            CboRepChequeBanco.ItemsSource = list;
            CboRepChequeBanco.DisplayMemberPath = "nombre";
            CboRepChequeBanco.SelectedIndex = 0;
        }
        catch { }
    }

    private async void BtnRepChequeBuscar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int? tipo = CboRepChequeTipo.SelectedIndex > 0 ? (int?)(CboRepChequeTipo.SelectedIndex - 1) : null;
            int? estado = CboRepChequeEstado.SelectedIndex > 0 ? (int?)(CboRepChequeEstado.SelectedIndex - 1) : null;
            
            string? bancoName = null;
            if (CboRepChequeBanco.SelectedItem != null)
            {
                dynamic bancoSel = CboRepChequeBanco.SelectedItem;
                try { bancoName = ((System.Text.Json.JsonElement)bancoSel).GetProperty("nombre").GetString(); }
                catch { bancoName = bancoSel.nombre?.ToString(); }
            }

            var desde = DtpRepChequeDesde.SelectedDate?.ToString("yyyy-MM-dd");
            var hasta = DtpRepChequeHasta.SelectedDate?.ToString("yyyy-MM-dd");

            var list = await App.Api.GetReporteCheques(tipo, estado, bancoName, desde, hasta);
            DgRepCheques.ItemsSource = list;

            decimal total = list?.Sum(c =>
            {
                try { return Convert.ToDecimal(((System.Text.Json.JsonElement)c).GetProperty("monto").GetDecimal()); }
                catch { return 0m; }
            }) ?? 0;

            TxtRepChequeTotal.Text = total.ToString("$ #,##0.00");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al consultar reporte de cheques: {ex.Message}", "Error");
        }
    }

    private async void BtnRepDepBuscar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int? idCuenta = null;
            if (CboRepDepCuenta.SelectedItem != null)
            {
                dynamic cuentaSel = CboRepDepCuenta.SelectedItem;
                try { idCuenta = ((System.Text.Json.JsonElement)cuentaSel).GetProperty("id").GetInt32(); }
                catch { idCuenta = Convert.ToInt32(cuentaSel.id); }
            }

            var desde = DtpRepDepDesde.SelectedDate?.ToString("yyyy-MM-dd");
            var hasta = DtpRepDepHasta.SelectedDate?.ToString("yyyy-MM-dd");

            var list = await App.Api.GetReporteDepositos(idCuenta, desde, hasta);
            DgRepDepositos.ItemsSource = list;

            decimal total = list?.Sum(d =>
            {
                try { return Convert.ToDecimal(((System.Text.Json.JsonElement)d).GetProperty("monto").GetDecimal()); }
                catch { return 0m; }
            }) ?? 0;

            TxtRepDepTotal.Text = total.ToString("$ #,##0.00");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al consultar reporte de depósitos: {ex.Message}", "Error");
        }
    }

    private async void BtnProyCalcular_Click(object sender, RoutedEventArgs e)
    {
        if (CboProyCuenta.SelectedItem == null)
        {
            MessageBox.Show("Seleccione una cuenta bancaria.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DtpProyFecha.SelectedDate.HasValue)
        {
            MessageBox.Show("Seleccione una fecha límite para la proyección.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            dynamic cuentaSel = CboProyCuenta.SelectedItem;
            int idCuenta;
            try { idCuenta = ((System.Text.Json.JsonElement)cuentaSel).GetProperty("id").GetInt32(); }
            catch { idCuenta = Convert.ToInt32(cuentaSel.id); }

            var hasta = DtpProyFecha.SelectedDate.Value.ToString("yyyy-MM-dd");

            var result = await App.Api.GetProyeccionFinanciera(idCuenta, hasta);
            if (result == null) return;

            var r = result.Value;
            TxtProySaldoActual.Text = r.GetProperty("saldoActual").GetDecimal().ToString("$ #,##0.00");
            TxtProyIngresos.Text = r.GetProperty("totalIngresos").GetDecimal().ToString("$ #,##0.00");
            TxtProyEgresos.Text = r.GetProperty("totalEgresos").GetDecimal().ToString("$ #,##0.00");
            TxtProySaldoFinal.Text = r.GetProperty("saldoProyectadoFinal").GetDecimal().ToString("$ #,##0.00");

            var proyDiariaJson = r.GetProperty("proyeccionDiaria").EnumerateArray();
            var listEvolucion = new List<dynamic>();
            foreach (var item in proyDiariaJson)
            {
                listEvolucion.Add(new
                {
                    Fecha = item.GetProperty("fecha").GetDateTime().ToLocalTime(),
                    Ingresos = item.GetProperty("ingresos").GetDecimal(),
                    Egresos = item.GetProperty("egresos").GetDecimal(),
                    SaldoProyectado = item.GetProperty("saldoProyectado").GetDecimal(),
                    Detalles = item.GetProperty("detalles").Clone()
                });
            }

            DgProyEvolucion.ItemsSource = listEvolucion;

            var chequesPendientesJson = r.GetProperty("detalleChequesPendientes").EnumerateArray();
            var listCheques = new List<dynamic>();
            foreach (var c in chequesPendientesJson)
            {
                listCheques.Add(new
                {
                    Fecha = c.GetProperty("fecha").GetDateTime().ToLocalTime(),
                    Tipo = c.GetProperty("esIngreso").GetBoolean() ? "COBRO (+)" : "DEBITO (-)",
                    NroCheque = c.GetProperty("nroCheque").GetString(),
                    Monto = c.GetProperty("monto").GetDecimal(),
                    Detalle = c.GetProperty("detalle").GetString()
                });
            }

            DgProyChequesPendientes.ItemsSource = listCheques;
            TxtProyChequesTitulo.Text = "Todos los cheques involucrados en el período";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al calcular proyección financiera: {ex.Message}", "Error");
        }
    }

    private void DgProyEvolucion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgProyEvolucion.SelectedItem == null) return;

        try
        {
            dynamic selectedDay = DgProyEvolucion.SelectedItem;
            var detallesJson = (System.Text.Json.JsonElement)selectedDay.Detalles;
            var listCheques = new List<dynamic>();
            
            foreach (var c in detallesJson.EnumerateArray())
            {
                listCheques.Add(new
                {
                    Fecha = selectedDay.Fecha,
                    Tipo = c.GetProperty("tipo").GetString(),
                    NroCheque = c.GetProperty("nroCheque").GetString(),
                    Monto = c.GetProperty("monto").GetDecimal(),
                    Detalle = c.GetProperty("detalle").GetString()
                });
            }

            DgProyChequesPendientes.ItemsSource = listCheques;
            TxtProyChequesTitulo.Text = $"Cheques del día {selectedDay.Fecha:dd/MM/yyyy}";
        }
        catch { }
    }
}

public class MovimientoConciliacionModel
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = "";
    public string? NroDocumento { get; set; }
    public string? Beneficiario { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public bool Conciliado { get; set; }
    public bool OriginalConciliado { get; set; }
    
    public string DebeText => Debe > 0 ? Debe.ToString("$ #,##0.00") : "";
    public string HaberText => Haber > 0 ? Haber.ToString("$ #,##0.00") : "";
    
    public bool IsChanged => Conciliado != OriginalConciliado;
}

public class ChequeSeleccionModel
{
    public int Id { get; set; }
    public string NroCheque { get; set; } = string.Empty;
    public string Banco { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public string Librador { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public bool Seleccionado { get; set; }
}
