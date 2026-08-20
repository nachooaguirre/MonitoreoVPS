using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SuperPOS.Shared.Entities.Ventas;
using SuperPOS.Shared.Entities.Ventas.Legacy;

namespace SuperPOS.Client.Views.Caja;

public partial class CajaPage : Page
{
    private readonly ObservableCollection<ItemVenta> _items = [];
    private Cliente? _clienteActual;
    private List<MedioPago> _mediosPago = [];
    private List<TipoComprobante> _tiposComprobante = [];

    private POS_Config? _cajaConfig;
    private int _currentPanelId = 1;
    private readonly Stack<int> _panelHistory = new();

    public CajaPage() 
    { 
        InitializeComponent(); 
        Loaded += OnLoaded; 
        Unloaded += OnUnloaded;
        PreviewKeyDown += Page_PreviewKeyDown;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        DgItems.ItemsSource = _items;
        _items.CollectionChanged += OnItemsCollectionChanged;
        await CargarCombos();
        await InicializarTeclado();
        TxtBuscarArticulo.Focus();
    }

    private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ItemVenta item in e.NewItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (ItemVenta item in e.OldItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }
        ActualizarTotales();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemVenta.Cantidad) || 
            e.PropertyName == nameof(ItemVenta.PrecioUnitario) || 
            e.PropertyName == nameof(ItemVenta.PorcentajeDescuento))
        {
            ActualizarTotales();
        }
    }

    private async Task CargarCombos()
    {
        try
        {
            // Cargar medios de pago desde API (hardcoded por ahora hasta tener el endpoint)
            _mediosPago = new List<MedioPago>
            {
                new() { Id = 1, Nombre = "Efectivo", Tipo = TipoMedioPago.Efectivo },
                new() { Id = 2, Nombre = "Débito", Tipo = TipoMedioPago.TarjetaDebito },
                new() { Id = 3, Nombre = "Crédito", Tipo = TipoMedioPago.TarjetaCredito },
                new() { Id = 4, Nombre = "MercadoPago", Tipo = TipoMedioPago.MercadoPago },
                new() { Id = 5, Nombre = "Transferencia", Tipo = TipoMedioPago.Transferencia },
                new() { Id = 6, Nombre = "Cta. Corriente", Tipo = TipoMedioPago.CtaCte }
            };
            CmbMedioPago.ItemsSource = _mediosPago;
            CmbMedioPago.SelectedIndex = 0;

            _tiposComprobante = new List<TipoComprobante>
            {
                new() { Id = 2, Nombre = "Factura B", Abreviatura = "Fac. B" },
                new() { Id = 3, Nombre = "Factura C", Abreviatura = "Fac. C" },
                new() { Id = 7, Nombre = "Ticket", Abreviatura = "Ticket" }
            };
            CmbTipoComprobante.ItemsSource = _tiposComprobante;
            CmbTipoComprobante.SelectedIndex = 2; // Ticket por defecto

            // Cliente por defecto: Consumidor Final
            var cliente = await App.Api.GetCliente(1);
            if (cliente is not null) SetCliente(cliente);
        }
        catch { }
    }

    private void SetCliente(Cliente c)
    {
        _clienteActual = c;
        TxtClienteActual.Text = c.RazonSocial;
        // Si el cliente tiene lista especial, mostrar indicador
        if (c.IdListaPrecio > 1)
            TxtClienteActual.Text += $"  [Lista {c.IdListaPrecio}]";
    }

    private async Task<decimal> ObtenerPrecioConLista(Articulo art, decimal cant)
    {
        if (_clienteActual is null || _clienteActual.IdListaPrecio <= 1)
            return art.PrecioVenta;
        try
        {
            var result = await App.Api.GetPrecioConLista(_clienteActual.IdListaPrecio, art.Id, cant);
            return result ?? art.PrecioVenta;
        }
        catch { return art.PrecioVenta; }
    }

    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            e.Handled = true;
            ToggleFullScreen();
            return;
        }

        // Ignorar teclas de control, especiales o de navegación
        if (e.Key == Key.System || e.Key == Key.Tab || 
            (e.Key >= Key.F1 && e.Key <= Key.F24) || 
            e.Key == Key.Left || e.Key == Key.Right || 
            e.Key == Key.Up || e.Key == Key.Down || 
            e.Key == Key.Escape || e.Key == Key.LWin || e.Key == Key.RWin)
        {
            return;
        }

        var focused = Keyboard.FocusedElement as DependencyObject;
        if (focused != null && IsInputControl(focused))
        {
            if (IsDescendantOf(focused, TxtBuscarArticulo))
            {
                return;
            }
            return;
        }

        if (Keyboard.FocusedElement != TxtBuscarArticulo)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift || 
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || 
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                return;
            }

            TxtBuscarArticulo.Focus();
        }
    }

    private async void TxtBuscarArticulo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            e.Handled = true;
            await BuscarYAgregarArticulo();
        }
        else if (e.Key == Key.F12) BtnCobrar_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.F10) BtnDividirPago_Click(sender, new RoutedEventArgs());
        else if (e.Key == Key.F9) LimpiarVenta();
    }

    private async Task BuscarYAgregarArticulo()
    {
        var codigo = TxtBuscarArticulo.Text.Trim();
        if (string.IsNullOrEmpty(codigo)) return;

        decimal cant = 1;
        if (!decimal.TryParse(TxtCantidad.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out cant))
            cant = 1;
        if (cant <= 0) cant = 1;

        // Verificar si ya está en la lista
        var existing = _items.FirstOrDefault(i => i.CodigoBarras == codigo);
        if (existing is not null)
        {
            existing.Cantidad += cant;
            DgItems.Items.Refresh();
            ActualizarTotales();
            TxtBuscarArticulo.Clear();
            TxtCantidad.Text = "1";
            return;
        }

        var art = await App.Api.BuscarArticuloPorCodigo(codigo) ?? await App.Cache.BuscarPorCodigoAsync(codigo);
        if (art is null)
        {
            // Si es número y no encontró por código, abrir búsqueda
            await AbrirBuscadorArticulos(codigo, cant);
        }
        else
        {
            AgregarArticuloALista(art, cant);
        }

        TxtBuscarArticulo.Clear();
        TxtCantidad.Text = "1";
    }

    private async Task Page_PreviewKeyDown_Override(object sender, KeyEventArgs e)
    {
        // En caso de que se implementen atajos a nivel página
    }

    private async void AgregarArticuloALista(Articulo art, decimal cant)
    {
        decimal precio = await ObtenerPrecioConLista(art, cant);
        var item = new ItemVenta
        {
            IdArticulo = art.Id,
            CodigoBarras = art.CodigoBarras,
            Descripcion = art.Descripcion,
            Cantidad = cant,
            PrecioUnitario = precio,
            AlicuotaIva = art.AlicuotaIva
        };
        _items.Add(item);
        ActualizarTotales();
        DgItems.ScrollIntoView(item);
    }

    private async Task AbrirBuscadorArticulos(string buscar, decimal cant)
    {
        var dlg = new BuscadorArticulosWindow(buscar) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.ArticuloSeleccionado is not null)
            AgregarArticuloALista(dlg.ArticuloSeleccionado, cant);
    }

    private async void BtnAgregarManual_Click(object sender, RoutedEventArgs e)
        => await BuscarYAgregarArticulo();

    private async void BtnCambiarCliente_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new BuscadorClientesWindow { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.ClienteSeleccionado is not null)
            SetCliente(dlg.ClienteSeleccionado);
    }

    private void BtnQuitarItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ItemVenta item)
        {
            _items.Remove(item);
            ActualizarTotales();
        }
    }

    private void BtnNuevaVenta_Click(object sender, RoutedEventArgs e) => LimpiarVenta();

    private void BtnCierreZ_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count > 0)
        {
            var confirm = System.Windows.MessageBox.Show("Hay una venta en curso. ¿Desea descartarla y proceder al cierre?", "Cierre Z", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;
        }

        if (App.PerfilActual?.PuedeCerrarCaja != true && !App.PerfilActual!.EsAdministrador)
        {
            System.Windows.MessageBox.Show("No tiene permiso para realizar el cierre de caja.", "Sin permiso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dlg = new CierreCajaWindow();
        dlg.ShowDialog();
    }

    private void LimpiarVenta()
    {
        _items.Clear();
        ActualizarTotales();
        TxtMontoRecibido.Text = "";
        TxtBuscarArticulo.Focus();
    }

    private void ActualizarTotales()
    {
        decimal subtotalSinIva = 0, iva21 = 0, iva105 = 0;
        foreach (var item in _items)
        {
            item.Calcular();
            var sinIva = item.Cantidad * item.PrecioUnitario / (1 + item.AlicuotaIva / 100);
            var ivaItem = item.SubTotal - sinIva;
            subtotalSinIva += sinIva;
            if (item.AlicuotaIva == 21m) iva21 += ivaItem;
            else if (item.AlicuotaIva == 10.5m) iva105 += ivaItem;
        }
        var total = _items.Sum(i => i.SubTotal);
        TxtSubtotalSinIva.Text = $"$ {subtotalSinIva:N2}";
        TxtIva21.Text = $"$ {iva21:N2}";
        TxtIva105.Text = $"$ {iva105:N2}";
        TxtTotal.Text = $"$ {total:N2}";
        ActualizarVuelto();
    }

    private void TxtMontoRecibido_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => ActualizarVuelto();

    private void ActualizarVuelto()
    {
        var total = _items.Sum(i => i.SubTotal);
        decimal.TryParse(TxtMontoRecibido.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var recibido);
        var vuelto = recibido - total;
        TxtVuelto.Text = $"$ {Math.Max(0, vuelto):N2}";
    }

    private async void BtnDividirPago_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0) { MessageBox.Show("No hay artículos en la venta.", "Aviso"); return; }
        if (_clienteActual is null) { MessageBox.Show("Seleccione un cliente.", "Aviso"); return; }

        var total = _items.Sum(i => i.SubTotal);
        var dlg = new DividirPagoWindow(total, _mediosPago) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Pagos.Count > 0)
        {
            decimal totalRecibido = dlg.Pagos.Sum(p => p.Importe + p.Vuelto);
            await ProcesarCobro(dlg.Pagos, totalRecibido, dlg.TotalRecargo);
        }
    }

    private async void BtnCobrar_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0) { MessageBox.Show("No hay artículos en la venta.", "Aviso"); return; }
        if (_clienteActual is null) { MessageBox.Show("Seleccione un cliente.", "Aviso"); return; }

        var total = _items.Sum(i => i.SubTotal);
        decimal.TryParse(TxtMontoRecibido.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var recibido);
        var idMedio = CmbMedioPago.SelectedValue is int m ? m : 1;

        string? referenciaPago = null;
        decimal recargo = 0;
        try
        {
            var globalCfg = await App.Api.GetConfiguracion();

            // 1. Integración con Postnet (Tarjeta de Débito = 2 o Tarjeta de Crédito = 3)
            if ((idMedio == 2 || idMedio == 3) && globalCfg?.PosnetHabilitado == true)
            {
                var dlg = new ProcesandoPagoWindow(total, esCredito: idMedio == 3) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() != true)
                {
                    return; // Transacción cancelada o fallida por el cajero
                }
                referenciaPago = $"{dlg.TarjetaMarca} (*{dlg.TarjetaUltimosDigitos}) Aut:{dlg.CodigoAutorizacion} Cup:{dlg.NumeroCupon}";
                recargo = dlg.Recargo;
                if (recargo != 0) referenciaPago += $" | Recargo: $ {recargo:N2}";
                recibido = total;
            }
            // 2. Integración con Mercado Pago QR (idMedio == 4)
            else if (idMedio == 4 && globalCfg?.MpQrHabilitado == true)
            {
                var dlg = new MercadoPagoQrWindow(total) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() != true)
                {
                    return; // Transacción cancelada o fallida por el cajero
                }
                referenciaPago = dlg.ReferenciaPago;
                recibido = total;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al inicializar el pago integrado:\n{ex.Message}", "Error de Conexión");
            return;
        }

        if (idMedio == 1 && recibido < total)
        { MessageBox.Show("El monto recibido no cubre el total.", "Aviso"); return; }

        var pagos = new List<ComprobantePago>
        {
            new() { IdMedioPago = idMedio, Importe = Math.Min(recibido, total), Vuelto = Math.Max(0, recibido - total), Referencia = referenciaPago }
        };

        await ProcesarCobro(pagos, recibido, recargo);
    }

    private async Task ProcesarCobro(List<ComprobantePago> pagos, decimal totalRecibido, decimal recargo = 0)
    {
        var total = _items.Sum(i => i.SubTotal) + recargo;
        var tipoId = CmbTipoComprobante.SelectedValue is int tc ? tc : 7;
        var iva21 = _items.Where(i => i.AlicuotaIva == 21).Sum(i => i.SubTotal - i.Cantidad * i.PrecioUnitario / 1.21m);
        var iva105 = _items.Where(i => i.AlicuotaIva == 10.5m).Sum(i => i.SubTotal - i.Cantidad * i.PrecioUnitario / 1.105m);
        var subtotal = _items.Sum(i => i.Cantidad * i.PrecioUnitario / (1 + i.AlicuotaIva / 100));

        var cbte = new Comprobante
        {
            IdTipoComprobante = tipoId,
            Letra = 'B',
            PuntoVenta = 1,
            IdCliente = _clienteActual!.Id,
            IdCaja = 1,
            IdSucursal = 1,
            IdUsuario = App.IdUsuarioActual,
            SubTotal = subtotal,
            TotalIva21 = iva21,
            TotalIva105 = iva105,
            Total = total,
            Comision = recargo,
            Estado = EstadoComprobante.Emitido,
            Detalles = _items.Select(i => new ComprobanteDetalle
            {
                IdArticulo = i.IdArticulo,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                PrecioUnitarioSinIva = i.PrecioUnitario / (1 + i.AlicuotaIva / 100),
                AlicuotaIva = i.AlicuotaIva,
                MontoIva = i.SubTotal - i.Cantidad * i.PrecioUnitario / (1 + i.AlicuotaIva / 100),
                SubTotal = i.SubTotal
            }).ToList(),
            Pagos = pagos
        };

        var itemsSnapshot = _items.ToList();
        Comprobante? resultado;
        try
        {
            resultado = await App.Api.RegistrarVenta(cbte);
        }
        catch (Exception ex) when (EsErrorDeConexion(ex))
        {
            // Sin conexión con el servidor: guardamos la venta local y la sincronizamos sola cuando vuelva internet.
            await App.Cache.EncolarVentaAsync(cbte);
            foreach (var det in cbte.Detalles)
                await App.Cache.AjustarStockAsync(det.IdArticulo, -det.Cantidad);

            MessageBox.Show($"Sin conexión con el servidor: la venta se guardó localmente y se sincronizará sola.\n\nTotal: $ {total:N2}\nVuelto: $ {Math.Max(0, totalRecibido - total):N2}",
                "Venta guardada offline", MessageBoxButton.OK, MessageBoxImage.Warning);
            LimpiarVenta();
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al registrar la venta:\n{ex.Message}", "Error");
            return;
        }

        try
        {
            // Ejecutar la impresión en segundo plano para no congelar la interfaz de cobro
            _ = Task.Run(async () =>
            {
                try
                {
                    var globalCfg = await App.Api.GetConfiguracion();
                    var printerName = globalCfg?.ImpresoraTicketNombre;
                    var piePagina = globalCfg?.MensajePiePagina;

                    if (resultado != null)
                    {
                        cbte.Numero = resultado.Numero;
                        cbte.Letra = resultado.Letra;
                        cbte.PuntoVenta = resultado.PuntoVenta;
                        cbte.Fecha = resultado.Fecha;
                        cbte.CAE = resultado.CAE;
                        cbte.CAEVencimiento = resultado.CAEVencimiento;
                        cbte.QrAfip = resultado.QrAfip;
                    }

                    await SuperPOS.Client.Services.TicketPrinter.ImprimirVenta(cbte, itemsSnapshot, _clienteActual!, printerName, piePagina,
                        globalCfg?.NombreEmpresa, globalCfg?.Cuit, globalCfg?.Direccion);
                }
                catch (Exception printEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al imprimir ticket: {printEx.Message}");
                }
            });

            MessageBox.Show($"Venta registrada!\n\nComprobante Nº {resultado?.Numero:000000}\nTotal: $ {total:N2}\nVuelto: $ {Math.Max(0, totalRecibido - total):N2}",
                "Venta OK", MessageBoxButton.OK, MessageBoxImage.Information);
            LimpiarVenta();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al registrar la venta:\n{ex.Message}", "Error");
        }
    }

    /// <summary>True si la excepción es por no poder alcanzar el servidor (no un rechazo de negocio).</summary>
    private static bool EsErrorDeConexion(Exception ex) =>
        ex is System.Net.Http.HttpRequestException { StatusCode: null } or TaskCanceledException;

    private async Task InicializarTeclado()
    {
        try
        {
            _cajaConfig = await App.Api.GetCajaConfig(1); // Caja 1 por defecto
            var panelId = _cajaConfig?.PanelPrincipal ?? 1;
            await CargarKeypad(panelId);
        }
        catch (Exception ex)
        {
            TxtPanelTitulo.Text = $"Error teclado: {ex.Message}";
        }
    }

    private async Task CargarKeypad(int panelId)
    {
        _currentPanelId = panelId;
        BtnVolverPanel.Visibility = _panelHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        try
        {
            var funciones = await App.Api.GetPOSFuncionesPorPanel(panelId);
            CanvasKeypad.Children.Clear();

            // Calcular dinámicamente los límites del teclado para que ocupe todo el ancho
            double maxX = 400;
            double maxY = 400;
            if (funciones.Count > 0)
            {
                var computedMaxX = funciones.Max(f => (f.PosX ?? 0) + (f.Ancho ?? 90));
                var computedMaxY = funciones.Max(f => (f.PosY ?? 0) + (f.Alto ?? 50));
                if (computedMaxX > 0) maxX = computedMaxX;
                if (computedMaxY > 0) maxY = computedMaxY;
            }

            // Añadir un pequeño margen de seguridad
            maxX += 2;
            maxY += 2;

            CanvasKeypad.Width = maxX;
            CanvasKeypad.Height = maxY;

            foreach (var func in funciones)
            {
                var textBlock = new TextBlock
                {
                    Text = func.Descripcion ?? func.Funcion ?? $"F{func.NroFuncion}",
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeights.Bold
                };

                var btn = new Button
                {
                    Content = textBlock,
                    Width = func.Ancho ?? 90,
                    Height = func.Alto ?? 50,
                    FontSize = func.FontSize ?? 15,
                    FontWeight = FontWeights.Bold,
                    Tag = func
                };

                // Estilizado dinámico (Premium - Coherente con resto de la app)
                btn.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)); // #2A2A2A
                btn.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58)); // #3A3A3A
                btn.BorderThickness = new Thickness(1);

                // Si es un producto / artículo, destacar con tono verdoso
                if (func.Funcion == "Articulo" || func.Codigo > 0 || !string.IsNullOrEmpty(func.Busqueda))
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(26, 58, 42)); // #1A3A2A
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 90, 58)); // #2A5A3A
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 128)); // #64C880
                }
                // Si es un medio de pago o acción clave (Cobrar)
                else if (func.Funcion == "Cobrar" || func.LlamarFuncion == 12 || func.Funcion == "MedioPago")
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(42, 26, 26)); // #2A1A1A
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 42, 42)); // #5A2A2A
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(255, 112, 112)); // #FF7070
                }
                // Si cambia de panel (navegación)
                else if (func.MoverPanel > 0)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(26, 42, 64)); // #1A2A40
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 58, 90)); // #2A3A5A
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(64, 144, 192)); // #4090C0
                }

                btn.Click += BtnDynamicKeypad_Click;

                Canvas.SetLeft(btn, func.PosX ?? 0);
                Canvas.SetTop(btn, func.PosY ?? 0);
                CanvasKeypad.Children.Add(btn);
            }

            // Cambiar título del panel
            if (panelId == (_cajaConfig?.PanelPrincipal ?? 1))
                TxtPanelTitulo.Text = "Menú Principal";
            else
                TxtPanelTitulo.Text = $"Panel {panelId}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar botones del teclado: {ex.Message}");
        }
    }

    private async void BtnDynamicKeypad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is POS_Funcion func)
        {
            // 1. Mover panel (navegación)
            if (func.MoverPanel.HasValue && func.MoverPanel.Value > 0)
            {
                _panelHistory.Push(_currentPanelId);
                await CargarKeypad(func.MoverPanel.Value);
                return;
            }

            // 2. Venta de artículo
            string? barcode = func.Busqueda?.Trim();
            if (string.IsNullOrEmpty(barcode) && func.Codigo.HasValue && func.Codigo.Value > 0)
            {
                barcode = func.Codigo.Value.ToString();
            }

            if (!string.IsNullOrEmpty(barcode))
            {
                TxtBuscarArticulo.Text = barcode;
                await BuscarYAgregarArticulo();
                return;
            }

            // 3. Ejecutar comandos del POS
            if (!string.IsNullOrEmpty(func.Funcion))
            {
                switch (func.Funcion.ToLower())
                {
                    case "cobrar":
                        BtnCobrar_Click(sender, e);
                        break;
                    case "nueva":
                    case "nuevaventa":
                        LimpiarVenta();
                        break;
                    case "anular":
                        if (DgItems.SelectedItem is ItemVenta item)
                        {
                            _items.Remove(item);
                            ActualizarTotales();
                        }
                        else
                        {
                            MessageBox.Show("Seleccione un ítem de la grilla para quitar.");
                        }
                        break;
                    case "cantidad":
                        TxtCantidad.Focus();
                        TxtCantidad.SelectAll();
                        break;
                    case "cliente":
                        BtnCambiarCliente_Click(sender, e);
                        break;
                    case "cierez":
                    case "cierre":
                        BtnCierreZ_Click(sender, e);
                        break;
                }
            }
        }
    }

    private async void BtnVolverPanel_Click(object sender, RoutedEventArgs e)
    {
        if (_panelHistory.Count > 0)
        {
            var prev = _panelHistory.Pop();
            await CargarKeypad(prev);
        }
    }

    private bool IsInputControl(DependencyObject? element)
    {
        var current = element;
        while (current != null)
        {
            if (current is TextBox or ComboBox or DatePicker or Button)
                return true;

            var typeName = current.GetType().Name;
            if (typeName.Contains("TextBox") || typeName.Contains("ComboBox") || typeName.Contains("DatePicker") || typeName.Contains("Button"))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private bool IsDescendantOf(DependencyObject? element, DependencyObject parent)
    {
        var current = element;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }
    private bool _isFullScreen = false;

    private void ToggleFullScreen()
    {
        var win = Window.GetWindow(this) as MainWindow;
        if (win != null)
        {
            _isFullScreen = !_isFullScreen;
            win.TogglePantallaCompleta(_isFullScreen);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Asegurar que si salimos de la página, restauramos la ventana
        if (_isFullScreen)
        {
            var win = Window.GetWindow(this) as MainWindow;
            win?.TogglePantallaCompleta(false);
            _isFullScreen = false;
        }
    }
}

public class ItemVenta : INotifyPropertyChanged
{
    private decimal _cantidad;
    private decimal _precioUnitario;
    private decimal _subTotal;

    public int IdArticulo { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal AlicuotaIva { get; set; } = 21;
    public decimal PorcentajeDescuento { get; set; }

    public decimal Cantidad
    {
        get => _cantidad;
        set { _cantidad = value; OnPropertyChanged(); Calcular(); }
    }

    public decimal PrecioUnitario
    {
        get => _precioUnitario;
        set { _precioUnitario = value; OnPropertyChanged(); Calcular(); }
    }

    public decimal SubTotal
    {
        get => _subTotal;
        private set 
        { 
            if (_subTotal != value)
            {
                _subTotal = value; 
                OnPropertyChanged(); 
            }
        }
    }

    public void Calcular()
    {
        var bruto = _cantidad * _precioUnitario;
        var dto = bruto * (PorcentajeDescuento / 100);
        SubTotal = Math.Round(bruto - dto, 2);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
