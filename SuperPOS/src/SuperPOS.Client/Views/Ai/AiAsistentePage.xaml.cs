using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using SkiaSharp;
using SuperPOS.Client.Models;
using SuperPOS.Client.Services;
using SuperPOS.Client.Views.OrdenesCompra;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Views.Ai;

public partial class AiAsistentePage : Page
{
    /// <summary>Memoria de chat por usuario (persiste aunque cierres sesión en el POS; mismo usuario = mismo historial).</summary>
    private static string RutaMemoriaChat()
    {
        var id = App.UsuarioSession?.Id ?? App.IdUsuarioActual;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SuperPOS", $"ia_chat_user_{id}.json");
    }

    /// <summary>Bitácora unificada: análisis (compra, venc, ventas) + resúmenes de consulta; persiste al cerrar la app o sesión.</summary>
    private static string RutaBitacoraArchivo()
    {
        var id = App.UsuarioSession?.Id ?? App.IdUsuarioActual;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SuperPOS", $"ia_bitacora_user_{id}.json");
    }

    /// <summary>Consulta libre: un archivo con varias conversaciones (cada hilo aislado).</summary>
    private static string RutaChatsLibre()
    {
        var id = App.UsuarioSession?.Id ?? App.IdUsuarioActual;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SuperPOS", $"ia_chats_libre_user_{id}.json");
    }

    /// <summary>Último análisis estructurado (grillas + botones OC) para no perderlo al navegar a otro módulo: el Page se recrea.</summary>
    private static string RutaCacheUltimoAnalisis()
    {
        var id = App.UsuarioSession?.Id ?? App.IdUsuarioActual;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SuperPOS", $"ia_ultimo_analisis_user_{id}.json");
    }

    private readonly ApiService _api = App.Api;
    private string _modoActual = "compra";
    private bool _cargando = false;
    private List<AiSugerenciaCompraDto>? _ultSug;
    private List<AiAlertaVencimientoDto>? _ultVenc;
    private List<AiTopProductoDto>? _ultVent;

    /// <summary>Turnos del chat de consulta <b>activo</b> (user/assistant) para reenviar a la API y guardar en disco.</summary>
    private readonly List<AiChatMensajeDto> _memoriaApi = new();

    private readonly IaChatsRoot _chatsRoot = new();
    private readonly ObservableCollection<IaConversacionArchivo> _listaChats = new();
    private bool _suprimirSelChat;
    private bool _chatsLibreCargado;

    private readonly ObservableCollection<ChatBurbuja> _burbujas = new();
    private readonly ObservableCollection<IaBitacoraItem> _bitacora = new();
    /// <summary>Solo el modo activo: compra / vencimiento / ventas (no mezclado).</summary>
    private readonly ObservableCollection<IaBitacoraItem> _bitacoraVista = new();

    /// <summary>Copia en memoria del JSON de disco; se sincroniza al guardar tras cada análisis.</summary>
    private IaUltimoAnalisisCache _cacheAnalisis = new();

    private const int MaxBitacoraEntradas = 500;

    private static readonly JsonSerializerOptions JsonFile = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private static readonly SKColor[] PaletaIa = [
        SKColor.Parse("#60B4FF"), SKColor.Parse("#40D080"), SKColor.Parse("#FFB040"), SKColor.Parse("#FF8070")
    ];

    private static readonly Dictionary<string, string> Placeholders = new()
    {
        ["compra"]      = "Ej: dame un análisis para los próximos 7 días tomando en cuenta lo que se vendió el último mes...",
        ["vencimiento"] = "Ej: priorizá los productos lácteos y decime si conviene hacer una promoción antes de que venzan...",
        ["ventas"]      = "Ej: analizá qué días de la semana vendo más y qué productos debería tener en stock los fines de semana...",
        ["consulta"]    = "Escribí tu pregunta sobre el negocio..."
    };

    public AiAsistentePage()
    {
        InitializeComponent();
        LstConversacion.ItemsSource = _burbujas;
        ListaBitacora.ItemsSource = _bitacoraVista;
        CargarUltimoAnalisisDesdeDisco();
        CargarBitacoraDesdeDisco();
        TratarDeImportarChataHaciaBitacoraSiHaceFalta();
        var modo = NormalizarModoIa(_cacheAnalisis.UltimoModo);
        MostrarModo(modo);
        InicializarChatsConsultaLibre();
        RestaurarComboDiasDesdeCache();
        if (!string.IsNullOrEmpty(_cacheAnalisis.TituloDatos) && _modoActual == _cacheAnalisis.UltimoModo)
            TxtTituloDatos.Text = _cacheAnalisis.TituloDatos;
    }

    private static string NormalizarModoIa(string? modo)
    {
        if (string.IsNullOrEmpty(modo) || !Placeholders.ContainsKey(modo)) return "compra";
        return modo;
    }

    // ─── Caché último análisis (grillas + OC) al recrear el Page al navegar ─

    private void CargarUltimoAnalisisDesdeDisco()
    {
        try
        {
            var p = RutaCacheUltimoAnalisis();
            if (!File.Exists(p)) { _cacheAnalisis = new IaUltimoAnalisisCache(); return; }
            _cacheAnalisis = JsonSerializer.Deserialize<IaUltimoAnalisisCache>(File.ReadAllText(p, Encoding.UTF8), JsonFile) ?? new();
            _ultSug = _cacheAnalisis.Sugerencias;
            _ultVenc = _cacheAnalisis.Vencimientos;
            _ultVent = _cacheAnalisis.TopVentas;
        }
        catch
        {
            _cacheAnalisis = new IaUltimoAnalisisCache();
        }
    }

    private void SincronizarYGuardarUltimoAnalisis()
    {
        if (_modoActual == "consulta") return;
        try
        {
            _cacheAnalisis.UltimoModo = _modoActual;
            _cacheAnalisis.Sugerencias = _ultSug;
            _cacheAnalisis.Vencimientos = _ultVenc;
            _cacheAnalisis.TopVentas = _ultVent;
            _cacheAnalisis.TituloDatos = TxtTituloDatos.Text;
            _cacheAnalisis.DiasAnalisis = DiasSeleccionados();
            var p = RutaCacheUltimoAnalisis();
            var dir = Path.GetDirectoryName(p);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.WriteAllText(p, JsonSerializer.Serialize(_cacheAnalisis, JsonFile), Encoding.UTF8);
        }
        catch { }
    }

    private void RestaurarComboDiasDesdeCache()
    {
        var d = _cacheAnalisis.DiasAnalisis;
        if (d is null or 0) return;
        foreach (var o in CmbDias.Items)
        {
            if (o is ComboBoxItem item && item.Tag is string t && int.TryParse(t, out var n) && n == d.Value)
            {
                CmbDias.SelectedItem = item;
                break;
            }
        }
    }

    /// <summary>Tras <see cref="MostrarModo"/>, volver a enlazar listas y botones OC desde memoria (misma sesión: cambio de pestaña; carga: datos desde JSON).</summary>
    private void RefrescarPanelDatosSegunModoYMemoria()
    {
        if (_modoActual == "consulta") return;
        switch (_modoActual)
        {
            case "compra":
                OcultarGraficoIa();
                if (_ultSug is { Count: > 0 })
                {
                    GridSugerencias.ItemsSource = _ultSug;
                    ConstruirAccionesCompra();
                }
                else
                {
                    GridSugerencias.ItemsSource = null;
                    PanelAccionesIa.Children.Clear();
                    PanelAccionesIa.Visibility = Visibility.Collapsed;
                }
                break;
            case "vencimiento":
                if (_ultVenc is { Count: > 0 })
                {
                    GridVencimientos.ItemsSource = _ultVenc;
                    ConstruirAccionesVencimientos();
                    ActualizarGraficoVencimientosIa();
                }
                else
                {
                    GridVencimientos.ItemsSource = null;
                    PanelAccionesIa.Children.Clear();
                    PanelAccionesIa.Visibility = Visibility.Collapsed;
                    OcultarGraficoIa();
                }
                break;
            case "ventas":
                GridVentas.ItemsSource = _ultVent is { Count: > 0 } v ? v : null;
                PanelAccionesIa.Children.Clear();
                PanelAccionesIa.Visibility = Visibility.Collapsed;
                if (_ultVent is { Count: > 0 })
                    ActualizarGraficoVentasIa();
                else
                    OcultarGraficoIa();
                break;
        }
    }

    private void LimpiarVistaYArchivoUltimoAnalisis()
    {
        _ultSug = null;
        _ultVenc = null;
        _ultVent = null;
        _cacheAnalisis = new IaUltimoAnalisisCache();
        try
        {
            var p = RutaCacheUltimoAnalisis();
            if (File.Exists(p)) File.Delete(p);
        }
        catch { }
        GridSugerencias.ItemsSource = null;
        GridVencimientos.ItemsSource = null;
        GridVentas.ItemsSource = null;
        PanelAccionesIa.Children.Clear();
        PanelAccionesIa.Visibility = Visibility.Collapsed;
        OcultarGraficoIa();
        switch (_modoActual)
        {
            case "compra":
                TxtTituloDatos.Text = "🛒 Artículos a reponer (último análisis; la grilla corresponde al último ▶ Analizar)";
                break;
            case "vencimiento":
                TxtTituloDatos.Text = "⏰ Lotes próximos a vencer (último análisis)";
                break;
            case "ventas":
                TxtTituloDatos.Text = "📈 Top productos (último análisis)";
                break;
        }
    }

    private void OcultarGraficoIa()
    {
        PanelChartIa.Visibility = Visibility.Collapsed;
        BtnDescargarGrafico.Visibility = Visibility.Collapsed;
        ChartIa.Series = Array.Empty<ISeries>();
    }

    private void ActualizarGraficoVentasIa()
    {
        if (_ultVent is not { Count: > 0 } || _modoActual != "ventas")
        {
            OcultarGraficoIa();
            return;
        }
        var toma = _ultVent.OrderByDescending(p => p.TotalFacturado).Take(16).ToList();
        var labels = toma.Select(p =>
        {
            var d = p.Descripcion ?? "";
            return d.Length > 18 ? d[..15] + "…" : d;
        }).ToArray();
        var vals = toma.Select(p => (double)p.TotalFacturado).ToArray();
        ChartIa.Series = [new ColumnSeries<double>
        {
            Name   = "Facturado",
            Values = vals,
            Fill   = new LinearGradientPaint(
                [SKColor.Parse("#1040A0"), SKColor.Parse("#60B4FF")],
                new SKPoint(0, 1), new SKPoint(0, 0)),
            Stroke   = null,
            MaxBarWidth = 20,
        }];
        ChartIa.XAxes = [new Axis
        {
            Labels = labels, TextSize = 9, LabelsRotation = -20,
        }];
        ChartIa.YAxes = [new Axis { Name = "$", TextSize = 10 }];
        ChartIa.LegendTextPaint  = new SolidColorPaint(SKColors.LightGray);
        ChartIa.LegendPosition  = LegendPosition.Hidden;
        PanelChartIa.Visibility  = Visibility.Visible;
        BtnDescargarGrafico.Visibility = Visibility.Visible;
    }

    private void ActualizarGraficoVencimientosIa()
    {
        if (_ultVenc is not { Count: > 0 } || _modoActual != "vencimiento")
        {
            OcultarGraficoIa();
            return;
        }
        var toma = _ultVenc
            .OrderBy(x => x.FechaVencimiento)
            .ThenBy(x => x.Descripcion)
            .Take(16)
            .ToList();
        var labels = toma.Select(x =>
        {
            var d = x.Descripcion ?? "";
            d = d.Length > 16 ? d[..13] + "…" : d;
            return d + " (" + x.DiasRestantes + "d)";
        }).ToArray();
        var vals = toma.Select(x => (double)x.Cantidad).ToArray();
        ChartIa.Series = [new ColumnSeries<double>
        {
            Name   = "Cantidad en lote",
            Values = vals,
            Fill   = new LinearGradientPaint(
                [SKColor.Parse("#804020"), SKColor.Parse("#FFB080")],
                new SKPoint(0, 1), new SKPoint(0, 0)),
            Stroke = null,
            MaxBarWidth = 18,
        }];
        ChartIa.XAxes = [new Axis
        {
            Labels = labels, TextSize = 8, LabelsRotation = -18,
        }];
        ChartIa.YAxes = [new Axis { Name = "Uds", TextSize = 10 }];
        ChartIa.LegendTextPaint = new SolidColorPaint(SKColors.LightGray);
        ChartIa.LegendPosition  = LegendPosition.Hidden;
        PanelChartIa.Visibility  = Visibility.Visible;
        BtnDescargarGrafico.Visibility = Visibility.Visible;
    }

    private async Task CargarBorradoresIaAsync()
    {
        try
        {
            TxtSinBorradores.Text = "Ninguna aún. Generá sugerencias y «Orden de compra → proveedor»; se guardan como borrador.";
            var list = await _api.GetOrdenesCompra((int)EstadoOrdenCompra.Borrador) ?? [];
            ListaBorradoresOc.ItemsSource = list.Count > 0
                ? list.OrderByDescending(o => o.Fecha).ToList()
                : null;
            TxtSinBorradores.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListaBorradoresOc.Visibility = list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            TxtSinBorradores.Visibility = Visibility.Visible;
            var m = (ex.Message ?? "Error").Trim();
            if (m.Length > 220) m = m[..217] + "…";
            TxtSinBorradores.Text = "No se pudo cargar borradores: " + m;
        }
    }

    private void BtnRefrescarBorradores_Click(object sender, RoutedEventArgs e)
    {
        _ = CargarBorradoresIaAsync();
    }

    private void BtnAbrirBorradorOc_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is null) return;
        if (!int.TryParse(b.Tag.ToString(), out var id)) return;
        new DetalleOCWindow(id) { Owner = Window.GetWindow(this) }.ShowDialog();
        _ = CargarBorradoresIaAsync();
    }

    private void BtnDescargarGrafico_Click(object sender, RoutedEventArgs e)
    {
        if (PanelChartIa.Visibility != Visibility.Visible || !ChartIa.Series.Any())
        {
            MessageBox.Show("No hay gráfico para exportar. Ejecutá un análisis con datos primero.", "Asistente IA", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new SaveFileDialog
        {
            Filter   = "PNG|*.png",
            FileName = _modoActual == "ventas" ? "ia_ventas.png" : "ia_vencimientos.png"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var w = Math.Max(480, (int)Math.Ceiling(PanelChartIa.ActualWidth));
            var h = Math.Max(200, (int)Math.Ceiling(PanelChartIa.ActualHeight));
            w = w > 0 ? w : 800;
            h = h > 0 ? h : 260;
            PanelChartIa.Measure(new Size(w, h));
            PanelChartIa.Arrange(new Rect(0, 0, w, h));
            var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(PanelChartIa);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var s = File.Create(dlg.FileName))
                enc.Save(s);
            MessageBox.Show("Gráfico guardado en:\n" + dlg.FileName, "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Exportar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ─── Bitácora unificada (todos los modos) ─────────────────────────────

    private void CargarBitacoraDesdeDisco()
    {
        _bitacora.Clear();
        try
        {
            var p = RutaBitacoraArchivo();
            if (!File.Exists(p)) { AplicarFiltroBitacora(); return; }
            var json = File.ReadAllText(p);
            var d = JsonSerializer.Deserialize<BitacoraWrapper>(json, JsonFile);
            if (d?.Entradas is { Count: > 0 } e)
            {
                foreach (var b in e.OrderBy(x => x.Utc))
                    _bitacora.Add(b);
            }
        }
        catch { }
        AplicarFiltroBitacora();
    }

    private void AplicarFiltroBitacora()
    {
        _bitacoraVista.Clear();
        if (_modoActual is not ("compra" or "vencimiento" or "ventas")) { RefrescarPlaceholderBitacora(); return; }
        foreach (var b in _bitacora.Where(b => b.Tipo == _modoActual).OrderBy(x => x.Utc))
            _bitacoraVista.Add(b);
        RefrescarPlaceholderBitacora();
    }

    private void GuardarBitacoraDisco()
    {
        try
        {
            while (_bitacora.Count > MaxBitacoraEntradas)
                _bitacora.RemoveAt(0);
            AplicarFiltroBitacora();
            var p = RutaBitacoraArchivo();
            var dir = Path.GetDirectoryName(p);
            if (dir is not null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new BitacoraWrapper { Entradas = _bitacora.ToList() }, JsonFile);
            File.WriteAllText(p, json);
        }
        catch { }
    }

    private void AgregarEntradaBitacora(string tipo, int? dias, string? instruccionOUsuario, string textoIa, string? idConversacion = null)
    {
        if (string.IsNullOrWhiteSpace(textoIa) && string.IsNullOrEmpty(instruccionOUsuario)) return;
        _bitacora.Add(new IaBitacoraItem
        {
            Tipo = tipo,
            Utc = DateTime.UtcNow,
            DiasAnalisis = dias,
            InstruccionOUsuario = string.IsNullOrWhiteSpace(instruccionOUsuario) ? null : instruccionOUsuario!.Trim(),
            TextoIa = textoIa?.Trim() ?? "",
            IdConversacion = idConversacion
        });
        GuardarBitacoraDisco();
        DesplazarAnalisisAlFinal();
    }

    /// <summary>Si aún no hay bitácora, importa un solo turno a la vez el chat guardado (misma ruta de usuario antigua).</summary>
    private void TratarDeImportarChataHaciaBitacoraSiHaceFalta()
    {
        if (_bitacora.Count > 0) return;
        var path = RutaMemoriaChat();
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var d = JsonSerializer.Deserialize<MemoriaArchivo>(json, JsonFile);
            if (d?.Mensajes is not { } lista) return;
            var t = 0u;
            var k = 0;
            while (k + 1 < lista.Count)
            {
                var a = lista[k];
                var b = lista[k + 1];
                if (a.Rol.Equals("user", StringComparison.OrdinalIgnoreCase)
                    && b.Rol.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(b.Contenido))
                {
                    _bitacora.Add(new IaBitacoraItem
                    {
                        Tipo = "consulta",
                        Utc = DateTime.UtcNow.AddSeconds(-(lista.Count - t)),
                        InstruccionOUsuario = a.Contenido,
                        TextoIa = b.Contenido
                    });
                    t++;
                    k += 2;
                }
                else
                    k++;
            }
            if (_bitacora.Count > 0) GuardarBitacoraDisco();
        }
        catch { }
        AplicarFiltroBitacora();
    }

    private void RefrescarPlaceholderBitacora()
    {
        TxtPlaceholderAnalisis.Visibility = _bitacoraVista.Count == 0 && _modoActual != "consulta" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DesplazarAnalisisAlFinal()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            try
            {
                var e = Math.Max(0, ScrollAnalisis.ExtentHeight - ScrollAnalisis.ViewportHeight);
                ScrollAnalisis.ScrollToVerticalOffset(e);
            }
            catch { }
        });
    }

    // ─── Consulta libre: varias conversaciones, historial aislado por hilo ──

    private void InicializarChatsConsultaLibre()
    {
        if (_chatsLibreCargado) return;
        _chatsLibreCargado = true;
        try
        {
            if (File.Exists(RutaChatsLibre()))
            {
                var root = JsonSerializer.Deserialize<IaChatsRoot>(File.ReadAllText(RutaChatsLibre(), Encoding.UTF8), JsonFile);
                if (root?.Conversaciones is { Count: > 0 } list)
                {
                    foreach (var c in list)
                        _listaChats.Add(c);
                    _chatsRoot.Version = root.Version;
                    _chatsRoot.ChatActivoId = root.ChatActivoId;
                }
            }
            if (_listaChats.Count == 0)
                TryMigrarChatV1UnicoArchivo();
            if (_listaChats.Count == 0)
                _listaChats.Add(new IaConversacionArchivo());

            LstChats.ItemsSource = _listaChats;
            var prefer = _chatsRoot.ChatActivoId;
            var sel = !string.IsNullOrEmpty(prefer)
                ? _listaChats.FirstOrDefault(c => c.Id == prefer) ?? _listaChats[0]
                : _listaChats[0];
            _suprimirSelChat = true;
            LstChats.SelectedItem = sel;
            _suprimirSelChat = false;
            CargarMensajesDeConversacion(sel);
            SincronizarParesChataHaciaBitacora();
        }
        catch
        {
            if (_listaChats.Count == 0) _listaChats.Add(new IaConversacionArchivo());
        }
    }

    private void TryMigrarChatV1UnicoArchivo()
    {
        try
        {
            var p = RutaMemoriaChat();
            if (!File.Exists(p)) return;
            var d = JsonSerializer.Deserialize<MemoriaArchivo>(File.ReadAllText(p, Encoding.UTF8), JsonFile);
            if (d?.Mensajes is not { Count: > 0 } m) return;
            _listaChats.Add(new IaConversacionArchivo
            {
                Titulo = "Conversación importada",
                Mensajes = m,
                UpdatedUtc = DateTime.UtcNow
            });
            _chatsRoot.ChatActivoId = _listaChats[0].Id;
        }
        catch { }
    }

    private void CargarMensajesDeConversacion(IaConversacionArchivo c)
    {
        c.Mensajes ??= [];
        _memoriaApi.Clear();
        foreach (var m in c.Mensajes)
        {
            if (string.IsNullOrWhiteSpace(m.Contenido)) continue;
            m.Rol = m.Rol.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            _memoriaApi.Add(m);
        }
        _chatsRoot.ChatActivoId = c.Id;
        RefrescarVistaConversacion();
    }

    private void PersistirMensajesEnConversacion(IaConversacionArchivo c)
    {
        c.Mensajes = new List<AiChatMensajeDto>(_memoriaApi.Select(m => new AiChatMensajeDto
        {
            Rol = m.Rol.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
            Contenido = m.Contenido
        }));
        c.UpdatedUtc = DateTime.UtcNow;
        if (c.Titulo == "Nueva conversación")
        {
            var first = c.Mensajes.Find(x => x.Rol.Equals("user", StringComparison.OrdinalIgnoreCase))?.Contenido;
            if (!string.IsNullOrWhiteSpace(first))
                c.Titulo = first!.Length > 52 ? first[..49] + "…" : first;
        }
    }

    private void GuardarChatsConsultaLibre()
    {
        try
        {
            if (LstChats.SelectedItem is IaConversacionArchivo activa)
                PersistirMensajesEnConversacion(activa);
            _chatsRoot.Conversaciones = [.. _listaChats];
            _chatsRoot.ChatActivoId = (LstChats.SelectedItem as IaConversacionArchivo)?.Id ?? _chatsRoot.ChatActivoId;
            var p = RutaChatsLibre();
            var dir = Path.GetDirectoryName(p);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.WriteAllText(p, JsonSerializer.Serialize(_chatsRoot, JsonFile), Encoding.UTF8);
        }
        catch { }
    }

    private void LstChats_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suprimirSelChat) return;
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is IaConversacionArchivo ant)
            PersistirMensajesEnConversacion(ant);
        if (LstChats.SelectedItem is IaConversacionArchivo nue)
        {
            CargarMensajesDeConversacion(nue);
            GuardarChatsConsultaLibre();
        }
    }

    private void BtnNuevaConversacion_Click(object sender, RoutedEventArgs e)
    {
        if (LstChats.SelectedItem is IaConversacionArchivo a)
            PersistirMensajesEnConversacion(a);
        var n = new IaConversacionArchivo();
        _listaChats.Insert(0, n);
        _suprimirSelChat = true;
        LstChats.SelectedItem = n;
        _suprimirSelChat = false;
        CargarMensajesDeConversacion(n);
        _chatsRoot.ChatActivoId = n.Id;
        GuardarChatsConsultaLibre();
    }

    private void GuardarMemoriaConLista() => GuardarChatsConsultaLibre();

    /// <summary>Integra a la bitácora los turnos (pregunta+respuesta) de consulta que estén en memoria y aún no figuren (p. ej. viejos archivos de solo chat).</summary>
    private void SincronizarParesChataHaciaBitacora()
    {
        var añadí = false;
        for (var k = 0; k + 1 < _memoriaApi.Count; k += 2)
        {
            var u = _memoriaApi[k];
            var a = _memoriaApi[k + 1];
            if (!u.Rol.Equals("user", StringComparison.OrdinalIgnoreCase)
                || !a.Rol.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(a.Contenido)) continue;
            if (_bitacora.Any(b => b.Tipo == "consulta"
                               && b.InstruccionOUsuario == u.Contenido
                               && b.TextoIa == a.Contenido))
                continue;
            _bitacora.Add(new IaBitacoraItem
            {
                Tipo = "consulta",
                Utc = DateTime.UtcNow,
                InstruccionOUsuario = u.Contenido,
                TextoIa = a.Contenido,
                IdConversacion = _chatsRoot.ChatActivoId
            });
            añadí = true;
        }
        if (añadí) GuardarBitacoraDisco();
    }

    private void RefrescarVistaConversacion()
    {
        _burbujas.Clear();
        foreach (var m in _memoriaApi)
        {
            _burbujas.Add(new ChatBurbuja
            {
                EsUsuario = m.Rol == "user",
                Texto = m.Contenido
            });
        }
        if (_burbujas.Count == 0)
        {
            _burbujas.Add(new ChatBurbuja
            {
                EsUsuario = false,
                Texto = "Soy tu asistente. Preguntame lo que quieras: stock, ventas, proveedores… La conversación se guarda por usuario en tu PC, incluso si cerrás sesión en el POS y volvés a entrar con el mismo usuario."
            });
        }
        DesplazarChatAlFinal();
    }

    private void DesplazarChatAlFinal()
    {
        if (LstConversacion.Visibility != Visibility.Visible) return;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (LstConversacion.Visibility != Visibility.Visible) return;
            if (_burbujas.Count == 0) return;
            LstConversacion.ScrollIntoView(_burbujas[^1]);
        });
    }

    private void BtnBorrarChat_Click(object sender, RoutedEventArgs e)
    {
        if (_modoActual == "consulta")
        {
            if (MessageBox.Show(
                    "Se borra solo esta conversación (este hilo) de consulta libre, en esta PC. Las demás conversaciones y los análisis (compra, vencimientos, ventas) se mantienen. ¿Seguir?",
                    "Borrar conversación", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            if (LstChats.SelectedItem is not IaConversacionArchivo act) return;
            _listaChats.Remove(act);
            if (_listaChats.Count == 0)
                _listaChats.Add(new IaConversacionArchivo());
            _suprimirSelChat = true;
            LstChats.SelectedItem = _listaChats[0];
            _suprimirSelChat = false;
            CargarMensajesDeConversacion((IaConversacionArchivo)LstChats.SelectedItem!);
            GuardarChatsConsultaLibre();
            return;
        }

        if (MessageBox.Show(
                "Se borra el historial de análisis (compra, vencimientos, ventas) en el panel de la izquierda. No borra las conversaciones de consulta libre. ¿Seguir?",
                "Borrar análisis", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _bitacora.Clear();
        try
        {
            var p = RutaBitacoraArchivo();
            if (File.Exists(p)) File.Delete(p);
        }
        catch { }
        LimpiarVistaYArchivoUltimoAnalisis();
        AplicarFiltroBitacora();
    }

    // ─── Cambio de modo ─────────────────────────────────────────────────────

    private void BtnModo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            MostrarModo(btn.Tag?.ToString() ?? "compra");
    }

    private void MostrarModo(string modo)
    {
        modo = NormalizarModoIa(modo);
        _modoActual = modo;

        var activo   = TryFindResource("AiBtnActive") as Style;
        var inactivo = TryFindResource("AiBtn") as Style;
        BtnSugerencias.Style  = inactivo;
        BtnVencimientos.Style = inactivo;
        BtnVentas.Style       = inactivo;
        BtnConsulta.Style     = inactivo;

        GridSugerencias.Visibility = Visibility.Collapsed;
        GridVencimientos.Visibility = Visibility.Collapsed;
        GridVentas.Visibility = Visibility.Collapsed;
        PanelConsulta.Visibility = Visibility.Collapsed;
        PanelAccionesIa.Children.Clear();
        PanelAccionesIa.Visibility = Visibility.Collapsed;
        BtnBorrarChat.Visibility = Visibility.Visible;

        var esChat = modo == "consulta";
        ChkBuscarWeb.Visibility = esChat ? Visibility.Visible : Visibility.Collapsed;
        ChkWebAnalisis.Visibility = !esChat ? Visibility.Visible : Visibility.Collapsed;
        ScrollAnalisis.Visibility = esChat ? Visibility.Collapsed : Visibility.Visible;
        LstConversacion.Visibility = esChat ? Visibility.Visible : Visibility.Collapsed;

        TxtPregunta.Clear();
        TxtPregunta.Tag = Placeholders[modo];
        BarraChats.Visibility = esChat ? Visibility.Visible : Visibility.Collapsed;
        if (esChat)
            TxtTituloPanelIa.Text = "💬 Cada conversación tiene su memoria. Cambiá de chat a la izquierda o creá \"＋ Nueva\".";
        else
            TxtTituloPanelIa.Text = modo switch
            {
                "compra" => "🛒 Historial de análisis de compra (solo este modo; consulta libre no aparece acá).",
                "vencimiento" => "⏰ Historial de vencimientos (solo este modo).",
                "ventas" => "📈 Historial de ventas (solo este modo).",
                _ => "💡 Análisis IA"
            };
        BtnBorrarChat.Content = esChat ? "🗑  Borrar esta conversación" : "🗑  Borrar historial de análisis";

        switch (modo)
        {
            case "compra":
                BtnSugerencias.Style = activo;
                GridSugerencias.Visibility = Visibility.Visible;
                TxtTituloDatos.Text = "🛒 Artículos a reponer (último análisis; la grilla corresponde al último ▶ Analizar)";
                TxtLabelInput.Text = "💬 Instrucción extra:";
                TxtBtnEnviar.Text = "Analizar";
                CmbDias.Visibility = Visibility.Visible;
                break;

            case "vencimiento":
                BtnVencimientos.Style = activo;
                GridVencimientos.Visibility = Visibility.Visible;
                TxtTituloDatos.Text = "⏰ Lotes próximos a vencer (último análisis)";
                TxtLabelInput.Text = "💬 Instrucción extra:";
                TxtBtnEnviar.Text = "Analizar";
                CmbDias.Visibility = Visibility.Visible;
                break;

            case "ventas":
                BtnVentas.Style = activo;
                GridVentas.Visibility = Visibility.Visible;
                TxtTituloDatos.Text = "📈 Top productos (último análisis)";
                TxtLabelInput.Text = "💬 Instrucción extra:";
                TxtBtnEnviar.Text = "Analizar";
                CmbDias.Visibility = Visibility.Visible;
                break;

            case "consulta":
                BtnConsulta.Style = activo;
                PanelConsulta.Visibility = Visibility.Visible;
                BtnBorrarChat.Visibility = Visibility.Visible;
                TxtTituloDatos.Text = "💬 Contexto: resumen del negocio se envía en cada mensaje (y mucho historial de este hilo, según el servidor).";
                TxtLabelInput.Text = "💬 Tu mensaje:";
                TxtBtnEnviar.Text = "Preguntar";
                CmbDias.Visibility = Visibility.Collapsed;
                InicializarChatsConsultaLibre();
                if (_burbujas.Count == 0) RefrescarVistaConversacion();
                break;
        }

        var visTope = modo == "compra" ? Visibility.Visible : Visibility.Collapsed;
        TxtLabelTope.Visibility = visTope;
        CmbTopeSugerencias.Visibility = visTope;
        TxtSufijoTope.Visibility = visTope;
        AplicarFiltroBitacora();

        if (esChat)
        {
            OcultarGraficoIa();
            PanelBorradoresOc.Visibility = Visibility.Collapsed;
        }
        else if (modo == "compra")
        {
            PanelBorradoresOc.Visibility = Visibility.Visible;
            _ = CargarBorradoresIaAsync();
        }
        else
            PanelBorradoresOc.Visibility = Visibility.Collapsed;

        BarCompartir.Visibility = Visibility.Visible;
        BtnExportarCsv.Visibility = esChat ? Visibility.Collapsed : Visibility.Visible;
        RefrescarPanelDatosSegunModoYMemoria();
    }

    /// <summary>Si todavía no hay archivo por usuario, copia el historial suelto anterior (misma PC).</summary>
    private static void MigrarMemoriaViejaSiHaceFalta()
    {
        try
        {
            var actual = RutaMemoriaChat();
            if (File.Exists(actual)) return;
            var vieja = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SuperPOS", "ia_chat_consulta.json");
            if (File.Exists(vieja)) File.Copy(vieja, actual);
        }
        catch { }
    }

    // ─── Ejecutar ───────────────────────────────────────────────────────────

    private async void BtnEjecutar_Click(object sender, RoutedEventArgs e)
    {
        if (_cargando) return;
        await EjecutarAnalisis();
    }

    private async void TxtPregunta_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_cargando)
            await EjecutarAnalisis();
    }

    private void CmbDias_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private int DiasSeleccionados()
    {
        if (CmbDias.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int dias))
            return dias;
        return 30;
    }

    private string? InstruccionExtra()
    {
        var txt = TxtPregunta.Text.Trim();
        return string.IsNullOrEmpty(txt) ? null : txt;
    }

    private async Task EjecutarAnalisis()
    {
        if (_modoActual == "consulta" && string.IsNullOrWhiteSpace(TxtPregunta.Text))
            return;

        SetCargando(true);

        try
        {
            switch (_modoActual)
            {
                case "compra":      await AnalizarCompras();      break;
                case "vencimiento": await AnalizarVencimientos(); break;
                case "ventas":      await AnalizarVentas();       break;
                case "consulta":    await EjecutarConsulta();     break;
            }
        }
        catch (Exception ex)
        {
            if (_modoActual == "consulta")
            {
                _burbujas.Add(new ChatBurbuja { EsUsuario = false, Texto = "❌ Error: " + ex.Message });
                DesplazarChatAlFinal();
            }
            else
                AgregarEntradaBitacora(_modoActual, DiasSeleccionados(), InstruccionExtra(), "❌ Error: " + ex.Message);
        }
        finally
        {
            SetCargando(false);
        }
    }

    private int? TopeSugerenciasCompra()
    {
        if (CmbTopeSugerencias.SelectedItem is not ComboBoxItem i || i.Tag is not string t || !int.TryParse(t, out var n) || n < 1) return 100;
        return n;
    }

    private async Task AnalizarCompras()
    {
        TxtCargando.Text = "Analizando stock...";
        var dias = DiasSeleccionados();
        var instr = InstruccionExtra();
        var tope = TopeSugerenciasCompra();
        var resultado = await _api.AiSugerenciasCompra(dias, instr, ChkWebAnalisis.IsChecked == true, tope);
        if (resultado is null)
        {
            AgregarEntradaBitacora("compra", dias, instr, "❌ No se pudo conectar con la API.");
            return;
        }

        var texto = (resultado.Texto ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(texto) && !string.IsNullOrEmpty(resultado.Error)) texto = resultado.Error;
        if (string.IsNullOrEmpty(texto)) texto = "Sin texto de la IA.";

        _ultSug = resultado.SugerenciasCompra ?? [];
        AgregarEntradaBitacora("compra", dias, instr, texto);
        GridSugerencias.ItemsSource = _ultSug;
        var tot = resultado.SugerenciasTotalBajoMinimo;
        var inc = resultado.SugerenciasIncluidas;
        if (tot is { } t && inc is { } i && t > i)
            TxtTituloDatos.Text = $"🛒 Grilla: {i} de {t} artículos bajo mín. góndola (más críticos). Importá a OC por proveedor.";
        else
            TxtTituloDatos.Text = $"🛒 Artículos a reponer ({_ultSug.Count})";
        ConstruirAccionesCompra();
        SincronizarYGuardarUltimoAnalisis();
    }

    private async Task AnalizarVencimientos()
    {
        TxtCargando.Text = "Revisando vencimientos...";
        var dias = DiasSeleccionados();
        var instr = InstruccionExtra();
        var resultado = await _api.AiAlertasVencimientos(dias, instr, ChkWebAnalisis.IsChecked == true);
        if (resultado is null)
        {
            AgregarEntradaBitacora("vencimiento", dias, instr, "❌ No se pudo conectar con la API.");
            return;
        }

        var texto = (resultado.Texto ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(texto) && !string.IsNullOrEmpty(resultado.Error)) texto = resultado.Error;
        if (string.IsNullOrEmpty(texto)) texto = "Sin texto de la IA.";

        _ultVenc = resultado.AlertasVencimiento ?? [];
        AgregarEntradaBitacora("vencimiento", dias, instr, texto);
        GridVencimientos.ItemsSource = _ultVenc;
        TxtTituloDatos.Text = $"⏰ Lotes próximos a vencer ({_ultVenc.Count})";
        ConstruirAccionesVencimientos();
        ActualizarGraficoVencimientosIa();
        SincronizarYGuardarUltimoAnalisis();
    }

    private async Task AnalizarVentas()
    {
        TxtCargando.Text = "Analizando ventas...";
        var dias = DiasSeleccionados();
        var instr = InstruccionExtra();
        var resultado = await _api.AiAnalisisVentas(dias, instr, ChkWebAnalisis.IsChecked == true);
        if (resultado is null)
        {
            AgregarEntradaBitacora("ventas", dias, instr, "❌ No se pudo conectar con la API.");
            return;
        }

        var texto = (resultado.Texto ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(texto) && !string.IsNullOrEmpty(resultado.Error)) texto = resultado.Error;
        if (string.IsNullOrEmpty(texto)) texto = "Sin texto de la IA.";

        _ultVent = resultado.AnalisisVentas?.TopProductos ?? [];
        AgregarEntradaBitacora("ventas", dias, instr, texto);
        GridVentas.ItemsSource = _ultVent;
        var a = resultado.AnalisisVentas;
        if (a is not null) TxtTituloDatos.Text = $"📈 Top productos — {a.DiasAnalizados}d — ${a.TotalFacturado:N0}";
        PanelAccionesIa.Children.Clear();
        PanelAccionesIa.Visibility = Visibility.Collapsed;
        ActualizarGraficoVentasIa();
        SincronizarYGuardarUltimoAnalisis();
    }

    private void ConstruirAccionesCompra()
    {
        PanelAccionesIa.Children.Clear();
        if (_ultSug is null || _ultSug.Count == 0) { PanelAccionesIa.Visibility = Visibility.Collapsed; return; }

        foreach (var grp in _ultSug.GroupBy(s => s.IdProveedor))
        {
            var prov = grp.First().Proveedor;
            var idProv = grp.Key;
            var lineas = grp.Select(s => new NuevaOCLineaInicial(
                s.IdArticulo, s.Descripcion, s.CodigoBarras, s.CantidadSugerida, s.PrecioCosto,
                s.AlicuotaIva > 0 ? s.AlicuotaIva : 21m, idProv, prov ?? "", "")).ToList();
            var b = new Button
            {
                Content = $"📄 Orden de compra → {prov} ({lineas.Count} ítems)",
                Margin = new Thickness(0, 0, 8, 6),
                Padding = new Thickness(10, 6, 10, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Abre la pantalla de nueva OC con estas líneas cargadas; podés editar todo antes de guardar."
            };
            b.Click += async (_, _) =>
            {
                var w = new NuevaOCWindow(idProv, lineas);
                w.Owner = Window.GetWindow(this);
                if (w.ShowDialog() == true) await CargarBorradoresIaAsync();
            };
            PanelAccionesIa.Children.Add(b);
        }
        PanelAccionesIa.Visibility = Visibility.Visible;
    }

    private void ConstruirAccionesVencimientos()
    {
        PanelAccionesIa.Children.Clear();
        if (_ultVenc is null || _ultVenc.Count == 0) { PanelAccionesIa.Visibility = Visibility.Collapsed; return; }
        var b = new Button
        {
            Content = "🏷  Armar ofertas (precio oferta editable por artículo)",
            Margin = new Thickness(0, 0, 8, 6),
            Padding = new Thickness(10, 6, 10, 6),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Asigna Precio de oferta en la ficha del artículo para liquidar antes del vencimiento del lote."
        };
        b.Click += (_, _) =>
        {
            var w = new OfertasLotesIaWindow(_ultVenc);
            w.Owner = Window.GetWindow(this);
            w.ShowDialog();
        };
        PanelAccionesIa.Children.Add(b);
        PanelAccionesIa.Visibility = Visibility.Visible;
    }

    private void BtnCopiarAnalisis_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_modoActual == "consulta")
            {
                var sbc = new StringBuilder();
                foreach (var x in _burbujas)
                    sbc.AppendLine(x.EsUsuario ? "Tú: " : "IA: ").AppendLine(x.Texto).AppendLine();
                Clipboard.SetText(sbc.ToString());
            }
            else if (_bitacoraVista.Count == 0)
                Clipboard.SetText("");
            else
            {
                var sb = new StringBuilder();
                foreach (var b in _bitacoraVista)
                {
                    sb.AppendLine(b.LineaEncabezado);
                    if (!string.IsNullOrEmpty(b.InstruccionOUsuario))
                        sb.AppendLine("💬 " + b.InstruccionOUsuario);
                    sb.AppendLine(b.TextoIa);
                    sb.AppendLine();
                }
                Clipboard.SetText(sb.ToString());
            }
            TxtEstado.Text = "● Copiado al portapapeles";
        }
        catch { }
    }

    private void BtnExportarCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = _modoActual switch
            {
                "compra" => "ia_sugerencias_compra.csv",
                "vencimiento" => "ia_alertas_vencimiento.csv",
                "ventas" => "ia_top_ventas.csv",
                _ => "export.csv"
            } };
            if (dlg.ShowDialog() != true) return;
            string csv = _modoActual switch
            {
                "compra" => ExportarCsvSugerencias(),
                "vencimiento" => ExportarCsvVenc(),
                "ventas" => ExportarCsvVentas(),
                _ => ""
            };
            File.WriteAllText(dlg.FileName, csv, Encoding.UTF8);
            MessageBox.Show("Archivo guardado.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Exportar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string ExportarCsvSugerencias()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Origen;Prioridad;Descripcion;Codigo;VelocidadDia;CobDias;StockActual;StockMinimo;CantidadSugerida;VendidoPeriodo;Proveedor;PrecioCosto;TotalEstimado;AlicuotaIva");
        if (_ultSug is null) return sb.ToString();
        foreach (var s in _ultSug)
            sb.AppendLine($"{Csv(s.OrigenSugerencia)};{s.Prioridad};{Csv(s.Descripcion)};{Csv(s.CodigoBarras)};{s.VelocidadVentaDiaria.ToString(System.Globalization.CultureInfo.InvariantCulture)};{s.CoberturaDiasAproximada?.ToString() ?? ""};{s.StockActual};{s.StockMinimo};{s.CantidadSugerida};{s.CantidadVendida30Dias};{Csv(s.Proveedor)};{s.PrecioCosto.ToString(System.Globalization.CultureInfo.InvariantCulture)};{s.TotalEstimado.ToString(System.Globalization.CultureInfo.InvariantCulture)};{s.AlicuotaIva.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        return sb.ToString();
    }

    private string ExportarCsvVenc()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Urgencia;Descripcion;Lote;FechaVencimiento;DiasRestantes;Cantidad");
        if (_ultVenc is null) return sb.ToString();
        foreach (var v in _ultVenc)
            sb.AppendLine($"{v.Urgencia};{Csv(v.Descripcion)};{Csv(v.LoteNro)};{v.FechaVencimiento:yyyy-MM-dd};{v.DiasRestantes};{v.Cantidad.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        return sb.ToString();
    }

    private string ExportarCsvVentas()
    {
        var sb = new StringBuilder();
        sb.AppendLine("IdArticulo;Descripcion;CantidadVendida;TotalFacturado");
        if (_ultVent is null) return sb.ToString();
        foreach (var t in _ultVent)
            sb.AppendLine($"{t.IdArticulo};{Csv(t.Descripcion)};{t.CantidadVendida.ToString(System.Globalization.CultureInfo.InvariantCulture)};{t.TotalFacturado.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        return sb.ToString();
    }

    private static string Csv(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        var t = s.Replace("\"", "\"\"");
        return $"\"{t}\"";
    }

    private async Task EjecutarConsulta()
    {
        var pregunta = TxtPregunta.Text.Trim();
        var conWeb = ChkBuscarWeb.IsChecked == true;
        TxtCargando.Text = conWeb ? "Buscando en la web e IA…" : "Consultando IA…";

        // Quitar el mensaje de bienvenida (no está en memoria en disco) al primer envío
        if (_memoriaApi.Count == 0 && _burbujas.Count == 1 && !_burbujas[0].EsUsuario)
            _burbujas.Clear();

        _burbujas.Add(new ChatBurbuja { EsUsuario = true, Texto = pregunta });
        TxtPregunta.Clear();
        DesplazarChatAlFinal();

        var historial = _memoriaApi.Count == 0 ? null : (IReadOnlyList<AiChatMensajeDto>?)_memoriaApi.ToList();

        var resultado = await _api.AiConsultaLibre(pregunta, historial, conWeb);
        if (resultado is null)
        {
            _burbujas.Add(new ChatBurbuja { EsUsuario = false, Texto = "❌ No se pudo conectar con la API." });
            DesplazarChatAlFinal();
            return;
        }

        var resTxt = (resultado.Texto ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(resTxt) && !string.IsNullOrEmpty(resultado.Error))
            resTxt = resultado.Error;
        if (string.IsNullOrEmpty(resTxt))
            resTxt = "No hubo texto de la IA. Revisá el saldo y la clave de DeepSeek, que la API esté en ejecución, o reintentá.";

        _memoriaApi.Add(new AiChatMensajeDto { Rol = "user", Contenido = pregunta });
        _memoriaApi.Add(new AiChatMensajeDto { Rol = "assistant", Contenido = resTxt });
        var idChat = (LstChats.SelectedItem as IaConversacionArchivo)?.Id;
        GuardarMemoriaConLista();
        AgregarEntradaBitacora("consulta", null, pregunta, resTxt, idChat);

        _burbujas.Add(new ChatBurbuja { EsUsuario = false, Texto = resTxt });
        DesplazarChatAlFinal();
    }

    private void SetCargando(bool cargando)
    {
        _cargando = cargando;
        PanelCargando.Visibility = cargando ? Visibility.Visible : Visibility.Collapsed;
        TxtEstado.Text = cargando ? "● Procesando…" : "● Listo";
        TxtEstado.Foreground = cargando
            ? System.Windows.Media.Brushes.Orange
            : System.Windows.Media.Brushes.LimeGreen;
        BtnEjecutar.IsEnabled = !cargando;
        BtnEnviarChat.IsEnabled = !cargando;
    }

    private class MemoriaArchivo
    {
        public List<AiChatMensajeDto> Mensajes { get; set; } = new();
    }

    private sealed class BitacoraWrapper
    {
        public List<IaBitacoraItem> Entradas { get; set; } = new();
    }

    private sealed class IaUltimoAnalisisCache
    {
        public string? UltimoModo { get; set; }
        public int? DiasAnalisis { get; set; }
        public string? TituloDatos { get; set; }
        public List<AiSugerenciaCompraDto>? Sugerencias { get; set; }
        public List<AiAlertaVencimientoDto>? Vencimientos { get; set; }
        public List<AiTopProductoDto>? TopVentas { get; set; }
    }
}

public class ChatBurbuja
{
    public bool EsUsuario { get; set; }
    public string Texto { get; set; } = string.Empty;
}
