using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SuperPOS.Client.Models;
using SuperPOS.Shared.Entities.Ventas;
using SuperPOS.Shared.Entities.Ventas.Legacy;

namespace SuperPOS.Client.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public ApiService(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // === ARTÍCULOS ===
    public async Task<(int total, List<Articulo> items)> GetArticulos(
        string? buscar = null,
        int? idDepto = null,
        int? idProveedor = null,
        int page = 1,
        int pageSize = 100,
        int? idFamilia = null,
        bool incluirInactivos = false,
        bool aplicarOfertas = false)
    {
        var url = $"api/articulos?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(buscar)) url += $"&buscar={Uri.EscapeDataString(buscar)}";
        if (idDepto.HasValue) url += $"&idDepartamento={idDepto}";
        if (idProveedor.HasValue) url += $"&idProveedor={idProveedor.Value}";
        if (idFamilia is > 0) url += $"&idFamilia={idFamilia.Value}";
        if (incluirInactivos) url += "&incluirInactivos=true";
        if (aplicarOfertas) url += "&aplicarOfertas=true";
        var resp = await _http.GetFromJsonAsync<PagedResult<Articulo>>(url, _json);
        return (resp?.Total ?? 0, resp?.Items ?? []);
    }

    /// <summary>Artículos activos del proveedor (órdenes de compra, búsqueda filtrada).</summary>
    public async Task<List<Articulo>> ListarArticulosProveedor(int idProveedor, string? buscar = null, int pageSize = 500)
    {
        var (_, items) = await GetArticulos(buscar, idDepto: null, idProveedor: idProveedor, page: 1, pageSize: pageSize);
        return items;
    }

    public async Task<List<dynamic>?> GetArticulos(string buscar)
    {
        var url = $"api/articulos?buscar={Uri.EscapeDataString(buscar)}&pageSize=10";
        var resp = await _http.GetFromJsonAsync<PagedResult<System.Text.Json.JsonElement>>(url, _json);
        if (resp is null) return null;
        return resp.Items.Select(e =>
        {
            dynamic d = new System.Dynamic.ExpandoObject();
            var dict = (IDictionary<string, object?>)(d as System.Dynamic.ExpandoObject)!;
            foreach (var prop in e.EnumerateObject())
            {
                object? val = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number when prop.Value.TryGetInt32(out var i) => (object)i,
                    System.Text.Json.JsonValueKind.Number => prop.Value.GetDecimal(),
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    _ => null
                };
                dict[prop.Name] = val;
            }
            return d;
        }).Cast<dynamic>().ToList();
    }

    public async Task<List<ProveedorSimple>?> GetProveedoresLista()
    {
        var url = "api/proveedores?pageSize=500";
        var resp = await _http.GetFromJsonAsync<PagedResult<System.Text.Json.JsonElement>>(url, _json);
        if (resp is null) return null;
        return resp.Items.Select(e => new ProveedorSimple
        {
            Id          = e.TryGetProperty("id",          out var pid) ? pid.GetInt32() : e.TryGetProperty("Id", out var pId) ? pId.GetInt32() : 0,
            RazonSocial = e.TryGetProperty("razonSocial", out var prs) ? prs.GetString() ?? "" : e.TryGetProperty("RazonSocial", out var pRs) ? pRs.GetString() ?? "" : "",
            Cuit        = e.TryGetProperty("cuit",        out var pc)  ? pc.GetString()  ?? "" : ""
        }).ToList();
    }

    public async Task<Articulo?> BuscarArticuloPorCodigo(string codigo)
    {
        try { return await _http.GetFromJsonAsync<Articulo>($"api/articulos/buscarPorCodigoBarras/{Uri.EscapeDataString(codigo)}", _json); }
        catch { return null; }
    }

    public async Task<Articulo?> GetArticulo(int id)
    {
        try { return await _http.GetFromJsonAsync<Articulo>($"api/articulos/{id}", _json); }
        catch { return null; }
    }

    public async Task<Articulo?> CrearArticulo(Articulo art)
    {
        var r = await _http.PostAsJsonAsync($"api/articulos?idUsuario={App.IdUsuarioActual}&idSucursal={App.SucursalId}", art);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Articulo>(_json);
    }

    public async Task ActualizarArticulo(Articulo art)
    {
        var r = await _http.PutAsJsonAsync($"api/articulos/{art.Id}?idUsuario={App.IdUsuarioActual}&idSucursal={App.SucursalId}", art);
        r.EnsureSuccessStatusCode();
    }

    public async Task EliminarArticulo(int id)
    {
        var r = await _http.DeleteAsync($"api/articulos/{id}");
        r.EnsureSuccessStatusCode();
    }

    // === OFERTAS ===
    public async Task<List<Oferta>?> GetOfertas()
    {
        try { return await _http.GetFromJsonAsync<List<Oferta>>("api/ofertas", _json); }
        catch { return null; }
    }

    public async Task<Oferta?> CrearOferta(Oferta oferta)
    {
        var r = await _http.PostAsJsonAsync("api/ofertas", oferta, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Oferta>(_json);
    }

    public async Task<Oferta?> ActualizarOferta(Oferta oferta)
    {
        var r = await _http.PutAsJsonAsync($"api/ofertas/{oferta.Id}", oferta, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Oferta>(_json);
    }

    public async Task EliminarOferta(int id)
    {
        var r = await _http.DeleteAsync($"api/ofertas/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<OfertaGraficaPunto>?> GetGraficaVentasOferta(int id)
    {
        try { return await _http.GetFromJsonAsync<List<OfertaGraficaPunto>>($"api/ofertas/grafica/{id}", _json); }
        catch { return null; }
    }

    public async Task<List<HistorialPrecio>?> GetHistorialPrecios(int idArticulo)
    {
        try { return await _http.GetFromJsonAsync<List<HistorialPrecio>>($"api/articulos/{idArticulo}/historial-precios", _json); }
        catch { return null; }
    }

    public async Task<List<BonificacionFecha>?> GetBonificacionesFechas(int idArticulo)
    {
        try { return await _http.GetFromJsonAsync<List<BonificacionFecha>>($"api/listasprecios/bonificaciones-fechas/{idArticulo}", _json); }
        catch { return null; }
    }

    public async Task<BonificacionFecha?> CrearBonificacionFecha(BonificacionFecha bonif)
    {
        var r = await _http.PostAsJsonAsync("api/listasprecios/bonificaciones-fechas", bonif);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<BonificacionFecha>(_json);
    }

    public async Task EliminarBonificacionFecha(int id)
    {
        var r = await _http.DeleteAsync($"api/listasprecios/bonificaciones-fechas/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<Departamento>> GetDepartamentos() =>
        await _http.GetFromJsonAsync<List<Departamento>>("api/articulos/departamentos", _json) ?? [];

    public async Task<List<Familia>> GetFamilias(int? idDepto = null)
    {
        var url = "api/articulos/familias";
        if (idDepto.HasValue) url += $"?idDepartamento={idDepto}";
        return await _http.GetFromJsonAsync<List<Familia>>(url, _json) ?? [];
    }

    public async Task<List<Marca>> GetMarcas() =>
        await _http.GetFromJsonAsync<List<Marca>>("api/articulos/marcas", _json) ?? [];

    // === CLIENTES ===
    public async Task<(int total, List<Cliente> items)> GetClientes(string? buscar = null, int page = 1, int pageSize = 100)
    {
        var url = $"api/clientes?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(buscar)) url += $"&buscar={Uri.EscapeDataString(buscar)}";
        var resp = await _http.GetFromJsonAsync<PagedResult<Cliente>>(url, _json);
        return (resp?.Total ?? 0, resp?.Items ?? []);
    }

    public async Task<Cliente?> GetCliente(int id)
    {
        try { return await _http.GetFromJsonAsync<Cliente>($"api/clientes/{id}", _json); }
        catch { return null; }
    }

    public async Task<Cliente?> CrearCliente(Cliente c)
    {
        var r = await _http.PostAsJsonAsync("api/clientes", c);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Cliente>(_json);
    }

    public async Task ActualizarCliente(Cliente c)
    {
        var r = await _http.PutAsJsonAsync($"api/clientes/{c.Id}", c);
        r.EnsureSuccessStatusCode();
    }

    public async Task EliminarCliente(int id)
    {
        var r = await _http.DeleteAsync($"api/clientes/{id}");
        r.EnsureSuccessStatusCode();
    }

    // === PROVEEDORES ===
    public async Task<(int total, List<Proveedor> items)> GetProveedores(string? buscar = null, int page = 1, int pageSize = 100)
    {
        var url = $"api/proveedores?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(buscar)) url += $"&buscar={Uri.EscapeDataString(buscar)}";
        var resp = await _http.GetFromJsonAsync<PagedResult<Proveedor>>(url, _json);
        return (resp?.Total ?? 0, resp?.Items ?? []);
    }

    public async Task<Proveedor?> GetProveedor(int id)
    {
        try { return await _http.GetFromJsonAsync<Proveedor>($"api/proveedores/{id}", _json); }
        catch { return null; }
    }

    public async Task<Proveedor?> CrearProveedor(Proveedor p)
    {
        var r = await _http.PostAsJsonAsync("api/proveedores", p);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Proveedor>(_json);
    }

    public async Task ActualizarProveedor(Proveedor p)
    {
        var r = await _http.PutAsJsonAsync($"api/proveedores/{p.Id}", p);
        r.EnsureSuccessStatusCode();
    }

    // === SUCURSALES / PUNTOS DE VENTA ===
    public async Task<List<SucursalAdminDto>> GetSucursales(bool incluirInactivas = true) =>
        await _http.GetFromJsonAsync<List<SucursalAdminDto>>($"api/sucursales?incluirInactivas={incluirInactivas}", _json) ?? [];

    public async Task<Sucursal?> CrearSucursal(Sucursal s)
    {
        var r = await _http.PostAsJsonAsync("api/sucursales", s);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Sucursal>(_json);
    }

    public async Task ActualizarSucursal(Sucursal s)
    {
        var r = await _http.PutAsJsonAsync($"api/sucursales/{s.Id}", s);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<Caja>> GetCajas(int idSucursal) =>
        await _http.GetFromJsonAsync<List<Caja>>($"api/cajas?idSucursal={idSucursal}", _json) ?? [];

    public async Task<Caja?> CrearCaja(Caja c)
    {
        var r = await _http.PostAsJsonAsync("api/cajas", c);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Caja>(_json);
    }

    public async Task ActualizarCaja(Caja c)
    {
        var r = await _http.PutAsJsonAsync($"api/cajas/{c.Id}", c);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<CajaDisponibleDto>> GetCajasDisponibles() =>
        await _http.GetFromJsonAsync<List<CajaDisponibleDto>>("api/cajas/disponibles", _json) ?? [];

    public async Task<List<CajaEstadoDto>> GetEstadoTerminales() =>
        await _http.GetFromJsonAsync<List<CajaEstadoDto>>("api/cajas/estado", _json) ?? [];

    // === VENTAS ===
    public async Task<Comprobante?> RegistrarVenta(Comprobante cbte)
    {
        var r = await _http.PostAsJsonAsync("api/ventas", cbte);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Comprobante>(_json);
    }

    public async Task<(int total, List<Comprobante> items)> GetVentas(DateTime? desde = null, DateTime? hasta = null, int page = 1, int pageSize = 50)
    {
        var url = $"api/ventas?page={page}&pageSize={pageSize}";
        if (desde.HasValue) url += $"&desde={desde:yyyy-MM-dd}";
        if (hasta.HasValue) url += $"&hasta={hasta:yyyy-MM-dd}";
        var resp = await _http.GetFromJsonAsync<PagedResult<Comprobante>>(url, _json);
        return (resp?.Total ?? 0, resp?.Items ?? []);
    }

    // === USUARIOS / PERFILES ===
    public async Task<Usuario?> Login(string usuario, string password)
    {
        var req = new { NombreUsuario = usuario, Password = password };
        var r = await _http.PostAsJsonAsync("api/usuarios/login", req);
        if (!r.IsSuccessStatusCode) return null;

        var resultado = await r.Content.ReadFromJsonAsync<LoginResponse>(_json);
        if (resultado?.Token is not null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resultado.Token);

        return resultado?.Usuario;
    }

    private class LoginResponse
    {
        public Usuario? Usuario { get; set; }
        public string? Token { get; set; }
    }

    public async Task<List<Usuario>> GetUsuarios() =>
        await _http.GetFromJsonAsync<List<Usuario>>("api/usuarios?soloActivos=true", _json) ?? [];

    // === AUDITORÍA (solo admin) ===
    public async Task<(int total, List<AuditLog> items)> GetAuditoria(
        int? idUsuario = null, string? entidad = null, string? buscar = null,
        DateTime? desde = null, DateTime? hasta = null, int page = 1, int pageSize = 100)
    {
        var url = $"api/auditoria?page={page}&pageSize={pageSize}";
        if (idUsuario.HasValue) url += $"&idUsuario={idUsuario}";
        if (!string.IsNullOrWhiteSpace(entidad)) url += $"&entidad={Uri.EscapeDataString(entidad)}";
        if (!string.IsNullOrWhiteSpace(buscar)) url += $"&buscar={Uri.EscapeDataString(buscar)}";
        if (desde.HasValue) url += $"&desde={desde:yyyy-MM-dd}";
        if (hasta.HasValue) url += $"&hasta={hasta:yyyy-MM-dd}";
        var resp = await _http.GetFromJsonAsync<PagedResult<AuditLog>>(url, _json);
        return (resp?.Total ?? 0, resp?.Items ?? []);
    }

    public async Task<List<string>> GetAuditoriaEntidades() =>
        await _http.GetFromJsonAsync<List<string>>("api/auditoria/entidades", _json) ?? [];

    public async Task<Usuario?> CrearUsuario(string nombreUsuario, string nombreCompleto, string password, int idPerfil, string? email, string? tel, bool accesoZebra)
    {
        var req = new { NombreUsuario = nombreUsuario, NombreCompleto = nombreCompleto, Password = password, IdPerfil = idPerfil, Email = email, Telefono = tel, AccesoZebra = accesoZebra };
        var r = await _http.PostAsJsonAsync("api/usuarios", req);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Usuario>(_json);
    }

    public async Task ActualizarUsuario(int id, string nombreCompleto, int idPerfil, bool activo, string? nuevaPassword, string? email, string? tel, bool accesoZebra)
    {
        var req = new { NombreCompleto = nombreCompleto, IdPerfil = idPerfil, Activo = activo, NuevaPassword = nuevaPassword, Email = email, Telefono = tel, AccesoZebra = accesoZebra };
        var r = await _http.PutAsJsonAsync($"api/usuarios/{id}", req);
        r.EnsureSuccessStatusCode();
    }

    public async Task EliminarUsuario(int id)
    {
        var r = await _http.DeleteAsync($"api/usuarios/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<Perfil>> GetPerfiles() =>
        await _http.GetFromJsonAsync<List<Perfil>>("api/perfiles", _json) ?? [];

    public async Task<Perfil?> CrearPerfil(Perfil p)
    {
        var r = await _http.PostAsJsonAsync("api/perfiles", p);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Perfil>(_json);
    }

    public async Task ActualizarPerfil(Perfil p)
    {
        var r = await _http.PutAsJsonAsync($"api/perfiles/{p.Id}", p);
        r.EnsureSuccessStatusCode();
    }

    // === PAGOS INTEGRADOS ===
    public async Task<List<TarjetaInfoDto>> GetTarjetasSoportadas() =>
        await _http.GetFromJsonAsync<List<TarjetaInfoDto>>("api/pagos-integrados/tarjetas", _json) ?? [];

    // === CONFIGURACIÓN ===
    public async Task<ConfiguracionEmpresa?> GetConfiguracion()
    {
        try { return await _http.GetFromJsonAsync<ConfiguracionEmpresa>("api/configuracion", _json); }
        catch { return null; }
    }

    public async Task GuardarConfiguracion(ConfiguracionEmpresa cfg)
    {
        var r = await _http.PutAsJsonAsync("api/configuracion", cfg);
        r.EnsureSuccessStatusCode();
    }

    // === CONFIGURACIÓN LEGACY CAJA ===
    public async Task<POS_Config?> GetCajaConfig(int nroCaja)
    {
        try { return await _http.GetFromJsonAsync<POS_Config>($"api/cajaconfig/config/{nroCaja}", _json); }
        catch { return null; }
    }

    public async Task<List<POS_Panel>> GetPOSPaneles()
    {
        try { return await _http.GetFromJsonAsync<List<POS_Panel>>("api/cajaconfig/paneles", _json) ?? []; }
        catch { return []; }
    }

    public async Task<List<POS_Funcion>> GetPOSFuncionesPorPanel(int panelId)
    {
        try { return await _http.GetFromJsonAsync<List<POS_Funcion>>($"api/cajaconfig/funciones/panel/{panelId}", _json) ?? []; }
        catch { return []; }
    }

    // === CUENTA CORRIENTE ===
    public async Task<List<ClienteCtaCteDto>?> GetClientesCtaCte(bool soloDeudores = false) =>
        await _http.GetFromJsonAsync<List<ClienteCtaCteDto>>($"api/ctacte/clientes?soloDeudores={soloDeudores}", _json);

    public async Task<MovimientosResult?> GetMovimientosCtaCte(int idCliente, DateTime desde, DateTime hasta) =>
        await _http.GetFromJsonAsync<MovimientosResult>($"api/ctacte/movimientos/{idCliente}?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}", _json);

    public async Task RegistrarPagoCtaCte(int idCliente, decimal monto, string? concepto, int idUsuario)
    {
        var r = await _http.PostAsJsonAsync("api/ctacte/pago", new { idCliente, monto, concepto, idUsuario });
        r.EnsureSuccessStatusCode();
    }

    public async Task AjusteManualCtaCte(int idCliente, decimal monto, bool esDebito, string? concepto, int idUsuario)
    {
        var r = await _http.PostAsJsonAsync("api/ctacte/ajuste", new { idCliente, monto, esDebito, concepto, idUsuario });
        r.EnsureSuccessStatusCode();
    }

    // === REPORTES ===
    public async Task<VentasDiaDto?> GetVentasDia(DateTime fecha) =>
        await _http.GetFromJsonAsync<VentasDiaDto>($"api/reportes/ventas-dia?fecha={fecha:yyyy-MM-dd}", _json);

    public async Task<VentasPeriodoResult?> GetVentasPeriodo(DateTime desde, DateTime hasta, string agrupar = "dia") =>
        await _http.GetFromJsonAsync<VentasPeriodoResult>($"api/reportes/ventas-periodo?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&agrupar={agrupar}", _json);

    public async Task<List<RankingProductoDto>?> GetRankingProductos(DateTime desde, DateTime hasta, int top = 20) =>
        await _http.GetFromJsonAsync<List<RankingProductoDto>>($"api/reportes/ranking-productos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&top={top}", _json);

    public async Task<StockBajoMinimoResult?> GetStockBajoMinimo() =>
        await _http.GetFromJsonAsync<StockBajoMinimoResult>("api/reportes/stock-bajo-minimo", _json);

    public async Task<RentabilidadProveedoresResult?> GetRentabilidadProveedores(DateTime desde, DateTime hasta, int? idProveedor = null)
    {
        var url = $"api/reportes/rentabilidad-proveedor?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (idProveedor.HasValue) url += $"&idProveedor={idProveedor.Value}";
        return await _http.GetFromJsonAsync<RentabilidadProveedoresResult>(url, _json);
    }

    // === CALENDARIO DE PAGOS A PROVEEDORES ===
    public async Task<List<CalendarioPagoDto>?> GetCalendarioPagos(int? idProveedor = null)
    {
        var url = "api/compras/calendario-pagos" + (idProveedor.HasValue ? $"?idProveedor={idProveedor.Value}" : "");
        return await _http.GetFromJsonAsync<List<CalendarioPagoDto>>(url, _json);
    }

    public async Task RegistrarPagoCompra(int idProveedor, decimal monto, string? concepto, int idUsuario, long idCompra)
    {
        var r = await _http.PostAsJsonAsync("api/ctacte-proveedores/pago", new { idProveedor, monto, concepto, idUsuario, idCompra });
        r.EnsureSuccessStatusCode();
    }



    public async Task<Comprobante?> GetVentaById(long id) =>
        await _http.GetFromJsonAsync<Comprobante>($"api/ventas/{id}", _json);

    public async Task AnularVenta(long id, int idUsuario)
    {
        var r = await _http.PostAsync($"api/ventas/{id}/anular?idUsuario={idUsuario}", null);
        r.EnsureSuccessStatusCode();
    }

    // === ZETAS (CIERRE DE CAJA) ===
    public async Task<ArqueoDto?> GetArqueoCaja(int idCaja) =>
        await _http.GetFromJsonAsync<ArqueoDto>($"api/zetas/arqueo/{idCaja}", _json);

    public async Task<ZetaDto?> CerrarCaja(object request)
    {
        var r = await _http.PostAsJsonAsync("api/zetas/cerrar", request);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ZetaDto>(_json);
    }

    public async Task<List<ZetaDto>?> GetZetas(int? idCaja = null) =>
        await _http.GetFromJsonAsync<List<ZetaDto>>($"api/zetas{(idCaja.HasValue ? $"?idCaja={idCaja}" : "")}", _json);

    /// <summary>Precio de venta según lista del cliente (Minorista/Mayorista/…). La administración de esas listas es en BD; caja usa este endpoint.</summary>
    public async Task<decimal?> GetPrecioConLista(int idLista, int idArticulo, decimal cantidad = 1)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<System.Text.Json.JsonElement>($"api/listasprecios/precio/{idLista}/{idArticulo}?cantidad={cantidad}", _json);
            if (result.TryGetProperty("PrecioFinal", out var pf))
                return pf.GetDecimal();
            return null;
        }
        catch { return null; }
    }

    // === LISTAS DE PRECIO DE PROVEEDOR (COMPRA / TARIFAS) ===
    public async Task<List<ListaPrecioProveedorResumenDto>?> GetListasPrecioProveedor(int? idProveedor = null)
    {
        var u = "api/listas-precio-proveedor";
        if (idProveedor.HasValue) u += $"?idProveedor={idProveedor.Value}";
        return await _http.GetFromJsonAsync<List<ListaPrecioProveedorResumenDto>>(u, _json);
    }

    public async Task<ListaPrecioProveedor?> GetListaPrecioProveedor(int id) =>
        await _http.GetFromJsonAsync<ListaPrecioProveedor>($"api/listas-precio-proveedor/{id}", _json);

    public async Task<ImportarListaProveedorResult> ImportarListaPrecioProveedor(int idProveedor, string nombre, string? rutaArchivo, string? textoPegado = null)
    {
        // Cargar el archivo en memoria aquí: si usamos StreamContent(FileStream) y el `using` del stream
        // cierra el archivo antes de que PostAsync termine de leer el multipart, falla con
        // "Error while copying content to a stream."
        byte[]? archivoBytes = null;
        string? archivoNombre = null;
        if (string.IsNullOrWhiteSpace(textoPegado) && !string.IsNullOrEmpty(rutaArchivo))
        {
            archivoBytes = await File.ReadAllBytesAsync(rutaArchivo);
            archivoNombre = Path.GetFileName(rutaArchivo);
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(idProveedor.ToString(CultureInfo.InvariantCulture)), "idProveedor");
        form.Add(new StringContent(nombre), "nombre");
        if (!string.IsNullOrWhiteSpace(textoPegado))
            form.Add(new StringContent(textoPegado), "textoPegado");
        else if (archivoBytes != null)
        {
            var fileContent = new ByteArrayContent(archivoBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", archivoNombre ?? "archivo");
        }
        else
            return new ImportarListaProveedorResult(false, "Elegí un archivo o pegá el texto de la lista (WhatsApp, Excel…).", null, null, null);
        var r = await _http.PostAsync("api/listas-precio-proveedor/importar", form);
        var body = await r.Content.ReadAsStringAsync();
        if (!r.IsSuccessStatusCode)
        {
            try
            {
                var je = JsonSerializer.Deserialize<JsonElement>(body);
                if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty("error", out var er))
                    return new ImportarListaProveedorResult(false, er.GetString() ?? r.ReasonPhrase, null, null, body);
            }
            catch { /* */ }

            return new ImportarListaProveedorResult(false, body, null, null, body);
        }

        var ok = JsonSerializer.Deserialize<JsonElement>(body, _json);
        if (ok.ValueKind != JsonValueKind.Object) return new ImportarListaProveedorResult(true, null, null, null, null);
        var idL = ok.TryGetProperty("id", out var idP) ? idP.GetInt32() : (int?)null;
        var nL = ok.TryGetProperty("lineas", out var nP) ? nP.GetInt32() : (int?)null;
        return new ImportarListaProveedorResult(true, null, idL, nL, null);
    }

    public async Task<JsonElement?> MatchearListaProveedor(int idLista)
    {
        var r = await _http.PostAsync($"api/listas-precio-proveedor/{idLista}/matchear-articulos", new StringContent("{}", Encoding.UTF8, "application/json"));
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    public async Task<bool> UpdateListaProveedorLinea(int idLinea, ListaLineaUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/listas-precio-proveedor/linea/{idLinea}", dto, _json);
        return r.IsSuccessStatusCode;
    }

    public async Task EliminarListaPrecioProveedor(int id)
    {
        var r = await _http.DeleteAsync($"api/listas-precio-proveedor/{id}");
        r.EnsureSuccessStatusCode();
    }

    // === ÓRDENES DE COMPRA ===
    public async Task<List<OrdenCompraResumenDto>?> GetOrdenesCompra(int? estado = null)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<OrdenCompraResumenDto>>(
                $"api/ordenescompra{(estado.HasValue ? $"?estado={estado}" : "")}", _json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<SugerenciaOCDto?> GetSugerenciaOC(int idProveedor) =>
        await _http.GetFromJsonAsync<SugerenciaOCDto>($"api/ordenescompra/sugerida/{idProveedor}", _json);

    public async Task CrearOrdenCompra(object oc)
    {
        var r = await _http.PostAsJsonAsync("api/ordenescompra", oc);
        r.EnsureSuccessStatusCode();
    }

    public async Task ActualizarOrdenCompra(int id, object oc)
    {
        var r = await _http.PutAsJsonAsync($"api/ordenescompra/{id}", oc);
        r.EnsureSuccessStatusCode();
    }

    public async Task AnularOrdenCompra(int id)
    {
        var r = await _http.PutAsJsonAsync($"api/ordenescompra/{id}/anular", new { });
        r.EnsureSuccessStatusCode();
    }

    public async Task EnviarOrdenCompra(int id)
    {
        var r = await _http.PutAsJsonAsync($"api/ordenescompra/{id}/enviar", new { });
        r.EnsureSuccessStatusCode();
    }

    public async Task DevolverOrdenCompra(int id)
    {
        var r = await _http.PutAsJsonAsync($"api/ordenescompra/{id}/devolver", new { });
        r.EnsureSuccessStatusCode();
    }

    public async Task CrearOrdenCompraDesde(SugerenciaOCDto sug, int idProveedor)
    {
        var oc = new
        {
            IdProveedor = idProveedor,
            IdUsuario = App.UsuarioSession?.Id ?? App.IdUsuarioActual,
            Detalles = sug.Items.Select(i => new
            {
                IdArticulo = i.Id,
                CantidadPedida = i.CantidadSugerida,
                PrecioCosto = i.PrecioCosto,
                AlicuotaIva = 21m,
                Subtotal = i.SubtotalEstimado
            }).ToList()
        };
        var r = await _http.PostAsJsonAsync("api/ordenescompra", oc);
        r.EnsureSuccessStatusCode();
    }

    // === INVENTARIO ===
    public async Task<List<InventarioResumenDto>?> GetInventarios() =>
        await _http.GetFromJsonAsync<List<InventarioResumenDto>>("api/inventarios", _json);

    public async Task<Inventario?> GetInventarioById(int id)
    {
        try { return await _http.GetFromJsonAsync<Inventario>($"api/inventarios/{id}", _json); }
        catch { return null; }
    }

    public async Task<InventarioDiferenciasResultDto?> GetInventarioDiferencias(int id)
    {
        try { return await _http.GetFromJsonAsync<InventarioDiferenciasResultDto>($"api/inventarios/{id}/diferencias", _json); }
        catch { return null; }
    }

    public async Task CrearInventario(object req)
    {
        var r = await _http.PostAsJsonAsync("api/inventarios", req);
        r.EnsureSuccessStatusCode();
    }

    public async Task ContarInventario(int idInventario, int idArticulo, decimal stockContado, bool acumulativo = false, string? observaciones = null)
    {
        var r = await _http.PutAsJsonAsync($"api/inventarios/{idInventario}/contar",
            new { IdArticulo = idArticulo, StockContado = stockContado, Observaciones = observaciones, Acumulativo = acumulativo }, _json);
        r.EnsureSuccessStatusCode();
    }

    public async Task CerrarInventario(int idInventario, bool aplicar)
    {
        var r = await _http.PutAsJsonAsync($"api/inventarios/{idInventario}/cerrar", new { AplicarAlStock = aplicar });
        r.EnsureSuccessStatusCode();
    }

    // === TESORERÍA ===
    public async Task<System.Text.Json.JsonElement?> GetTesoreriaSaldos() =>
        await _http.GetFromJsonAsync<System.Text.Json.JsonElement>("api/tesoreria/saldos", _json);

    public async Task<List<dynamic>?> GetCuentasTesoreria()
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>("api/tesoreria/cuentas", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task CrearCuentaTesoreria(object cuenta)
    {
        var r = await _http.PostAsJsonAsync("api/tesoreria/cuentas", cuenta);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<dynamic>?> GetMovimientosTesoreria(string? desde = null, string? hasta = null)
    {
        var url = "api/tesoreria/movimientos?pageSize=300";
        if (desde != null) url += $"&desde={desde}";
        if (hasta != null) url += $"&hasta={hasta}";
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement>(url, _json);
        if (r.ValueKind == System.Text.Json.JsonValueKind.Undefined) return null;
        return r.GetProperty("items").EnumerateArray().Cast<dynamic>().ToList();
    }

    public async Task RegistrarMovimientoTesoreria(object mov)
    {
        var r = await _http.PostAsJsonAsync("api/tesoreria/movimientos", mov);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<dynamic>?> GetMovimientosDeCuenta(int idCuenta, string? desde = null, string? hasta = null)
    {
        var url = $"api/tesoreria/movimientos?idCuenta={idCuenta}&pageSize=1000";
        if (desde != null) url += $"&desde={desde}";
        if (hasta != null) url += $"&hasta={hasta}";
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement>(url, _json);
        if (r.ValueKind == System.Text.Json.JsonValueKind.Undefined) return null;
        return r.GetProperty("items").EnumerateArray().Cast<dynamic>().ToList();
    }

    public async Task ConciliarMovimientos(List<object> request)
    {
        var r = await _http.PutAsJsonAsync("api/tesoreria/movimientos/conciliar", request);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<dynamic>?> GetCheques()
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>("api/tesoreria/cheques", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task<List<dynamic>?> GetChequesEnCartera()
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>("api/tesoreria/cheques?estado=0", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task RegistrarDeposito(object req)
    {
        var r = await _http.PostAsJsonAsync("api/tesoreria/depositos", req);
        r.EnsureSuccessStatusCode();
    }

    public async Task<int> GetConteoArticulosLote(object req)
    {
        var r = await _http.PostAsJsonAsync("api/articulos/precios/conteo-lote", req);
        r.EnsureSuccessStatusCode();
        var el = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return el.GetProperty("count").GetInt32();
    }

    public async Task<int> ActualizarPreciosLote(object req)
    {
        var r = await _http.PutAsJsonAsync("api/articulos/precios/actualizar-lote", req);
        r.EnsureSuccessStatusCode();
        var el = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return el.GetProperty("count").GetInt32();
    }

    public async Task RegistrarCheque(object cheque)
    {
        var r = await _http.PostAsJsonAsync("api/tesoreria/cheques", cheque);
        r.EnsureSuccessStatusCode();
    }

    public async Task ActualizarEstadoCheque(int id, int nuevoEstado, int? idCuentaDestino = null, string? observaciones = null)
    {
        var req = new { NuevoEstado = nuevoEstado, IdCuentaDestino = idCuentaDestino, Observaciones = observaciones };
        var r = await _http.PutAsJsonAsync($"api/tesoreria/cheques/{id}/estado", req);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<dynamic>?> GetChequeras()
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>("api/tesoreria/chequeras", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task<List<dynamic>?> GetChequerasPorCuenta(int idCuenta)
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>($"api/tesoreria/chequeras/cuenta/{idCuenta}", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task RegistrarChequera(object chequera)
    {
        var r = await _http.PostAsJsonAsync("api/tesoreria/chequeras", chequera);
        r.EnsureSuccessStatusCode();
    }

    public async Task<List<string>?> GetNumerosDisponiblesChequera(int idChequera)
    {
        return await _http.GetFromJsonAsync<List<string>>($"api/tesoreria/chequeras/{idChequera}/numeros-disponibles", _json);
    }

    public async Task<List<dynamic>?> GetBancos()
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>("api/tesoreria/bancos", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task<List<dynamic>?> GetReporteCheques(int? tipo, int? estado, string? banco, string? desde, string? hasta)
    {
        var url = "api/tesoreria/reportes/cheques?";
        if (tipo.HasValue) url += $"tipo={tipo}&";
        if (estado.HasValue) url += $"estado={estado}&";
        if (!string.IsNullOrWhiteSpace(banco)) url += $"banco={Uri.EscapeDataString(banco)}&";
        if (!string.IsNullOrWhiteSpace(desde)) url += $"desde={desde}&";
        if (!string.IsNullOrWhiteSpace(hasta)) url += $"hasta={hasta}&";
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>(url, _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task<List<dynamic>?> GetReporteDepositos(int? idCuentaBanco, string? desde, string? hasta)
    {
        var url = "api/tesoreria/reportes/depositos?";
        if (idCuentaBanco.HasValue) url += $"idCuentaBanco={idCuentaBanco}&";
        if (!string.IsNullOrWhiteSpace(desde)) url += $"desde={desde}&";
        if (!string.IsNullOrWhiteSpace(hasta)) url += $"hasta={hasta}&";
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>(url, _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task<System.Text.Json.JsonElement?> GetProyeccionFinanciera(int idCuentaBanco, string fechaHasta)
    {
        var url = $"api/tesoreria/reportes/proyeccion?idCuentaBanco={idCuentaBanco}&fechaHasta={fechaHasta}";
        return await _http.GetFromJsonAsync<System.Text.Json.JsonElement>(url, _json);
    }

    public async Task<List<dynamic>?> GetGastosCaja()
    {
        var r = await _http.GetFromJsonAsync<System.Text.Json.JsonElement[]>("api/tesoreria/gastos", _json);
        return r?.Cast<dynamic>().ToList();
    }

    public async Task RegistrarGastoCaja(object gasto)
    {
        var r = await _http.PostAsJsonAsync("api/tesoreria/gastos", gasto);
        r.EnsureSuccessStatusCode();
    }

    // === REMITOS ===
    public async Task<List<dynamic>?> GetRemitos(int? tipo = null, int? estado = null, int? idProveedor = null)
    {
        var url = "api/remitos?pageSize=200";
        if (tipo.HasValue) url += $"&tipo={tipo}";
        if (estado.HasValue) url += $"&estado={estado}";
        if (idProveedor.HasValue) url += $"&idProveedor={idProveedor}";
        var resp = await _http.GetFromJsonAsync<JsonElement>(url, _json);
        if (resp.ValueKind == JsonValueKind.Undefined) return null;
        return resp.GetProperty("items").EnumerateArray()
            .Select(e => (dynamic)e).ToList();
    }

    public async Task<JsonElement?> GetRemitoDetalle(int id)
    {
        var resp = await _http.GetAsync($"api/remitos/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    public async Task<JsonElement?> CrearRemitoManual(Remito remito)
    {
        var r = await _http.PostAsJsonAsync("api/remitos", remito, _json);
        if (!r.IsSuccessStatusCode)
        {
            var err = await r.Content.ReadAsStringAsync();
            throw new InvalidOperationException(err.Length > 200 ? err[..200] : err);
        }
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    public async Task<JsonElement?> GetOrdenCompraDetalle(int id)
    {
        try
        {
            var resp = await _http.GetAsync($"api/ordenescompra/{id}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> CrearRemitoDesdeOC(int idOC, object req)
    {
        var r = await _http.PostAsJsonAsync($"api/remitos/desde-oc/{idOC}", req);
        r.EnsureSuccessStatusCode();
        var el = await r.Content.ReadFromJsonAsync<JsonElement>(_json);
        if (el.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Respuesta inválida al crear el remito.");
        if (el.TryGetProperty("id", out var idCamel) && idCamel.ValueKind == JsonValueKind.Number)
            return idCamel.GetInt32();
        if (el.TryGetProperty("Id", out var idPascal) && idPascal.ValueKind == JsonValueKind.Number)
            return idPascal.GetInt32();
        throw new InvalidOperationException("No se pudo leer el id del remito creado en la respuesta de la API.");
    }

    public async Task ConfirmarRemito(int idRemito, object req)
    {
        var r = await _http.PutAsJsonAsync($"api/remitos/{idRemito}/confirmar", req);
        r.EnsureSuccessStatusCode();
    }

    // === SUCURSALES ===
    public async Task<List<JsonElement>?> GetSucursales()
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<JsonElement[]>("api/sucursales", _json);
            return resp?.ToList();
        }
        catch { return null; }
    }

    // === TRANSFERENCIAS INTERNAS ===
    public async Task<List<JsonElement>?> GetTransferenciasInternas()
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<JsonElement[]>("api/transferenciasinternas", _json);
            return resp?.ToList();
        }
        catch { return null; }
    }

    public async Task<JsonElement?> CrearTransferenciaInterna(object req)
    {
        var r = await _http.PostAsJsonAsync("api/transferenciasinternas", req, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    public async Task<JsonElement?> ConfirmarTransferenciaInterna(int id)
    {
        var r = await _http.PutAsJsonAsync($"api/transferenciasinternas/{id}/confirmar", new { });
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    public async Task<JsonElement?> AnularTransferenciaInterna(int id)
    {
        var r = await _http.PutAsJsonAsync($"api/transferenciasinternas/{id}/anular", new { });
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    // === INTELIGENCIA ARTIFICIAL ===
    public async Task<AiRespuestaDto?> AiSugerenciasCompra(int dias = 30, string? instruccion = null, bool buscarEnWeb = false, int? maxFilasSugerencias = null)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/ai/sugerencias-compra", new { Dias = dias, Instruccion = instruccion, BuscarEnWeb = buscarEnWeb, MaxFilasSugerencias = maxFilasSugerencias }, _json);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<AiRespuestaDto>(_json);
        }
        catch { return null; }
    }

    public async Task<AiRespuestaDto?> AiAlertasVencimientos(int dias = 30, string? instruccion = null, bool buscarEnWeb = false)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/ai/alertas-vencimientos", new { Dias = dias, Instruccion = instruccion, BuscarEnWeb = buscarEnWeb }, _json);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<AiRespuestaDto>(_json);
        }
        catch { return null; }
    }

    public async Task<AiRespuestaDto?> AiAnalisisVentas(int dias = 30, string? instruccion = null, bool buscarEnWeb = false)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/ai/analisis-ventas", new { Dias = dias, Instruccion = instruccion, BuscarEnWeb = buscarEnWeb }, _json);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<AiRespuestaDto>(_json);
        }
        catch { return null; }
    }

    public async Task<AiRespuestaDto?> AiConsultaLibre(string pregunta, IReadOnlyList<AiChatMensajeDto>? historial = null, bool buscarEnWeb = false)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/ai/consulta", new
            {
                Pregunta = pregunta,
                Historial = historial,
                BuscarEnWeb = buscarEnWeb
            }, _json);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<AiRespuestaDto>(_json);
        }
        catch { return null; }
    }

    /// <summary>Recomienda cantidades según tarifa, ventas 30d y bonificaciones.</summary>
    public async Task<AiRespuestaDto?> AiRecomendarListaProveedor(int idLista, int diasProyeccion = 10, string? instruccion = null)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/ai/recomendar-lista-proveedor",
                new { IdLista = idLista, DiasProyeccion = diasProyeccion, Instruccion = instruccion }, _json);
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<AiRespuestaDto>(_json);
        }
        catch { return null; }
    }

    // === TRAZABILIDAD ===
    public async Task<List<JsonElement>?> GetTrazabilidadPorArticulo(int idArticulo, int take = 200)
    {
        var resp = await _http.GetFromJsonAsync<JsonElement[]>($"api/trazabilidad/articulos/{idArticulo}?take={take}", _json);
        return resp?.ToList();
    }

    public async Task<JsonElement?> CrearEventoTrazabilidad(object req)
    {
        var r = await _http.PostAsJsonAsync("api/trazabilidad/eventos", req, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    // === ETIQUETAS ===
    public async Task<List<JsonElement>?> GetEtiquetasCola()
    {
        var resp = await _http.GetFromJsonAsync<JsonElement[]>("api/etiquetas/cola", _json);
        return resp?.ToList();
    }

    public async Task<object?> EncolarEtiqueta(int idArticulo, string? barcode, int qty)
    {
        var req = new { IdArticulo = idArticulo, CodigoBarras = barcode, Cantidad = qty };
        var r = await _http.PostAsJsonAsync("api/etiquetas/encolar", req, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<JsonElement>(_json);
    }

    public async Task<bool> EliminarEtiquetaCola(int id)
    {
        var r = await _http.DeleteAsync($"api/etiquetas/cola/{id}");
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> LimpiarEtiquetasCola()
    {
        var r = await _http.DeleteAsync("api/etiquetas/cola/limpiar");
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> MarcarEtiquetasImpresas(List<int> ids)
    {
        var req = new { Ids = ids };
        var r = await _http.PostAsJsonAsync("api/etiquetas/cola/imprimir-marcar", req, _json);
        return r.IsSuccessStatusCode;
    }

    // === PRESUPUESTOS ===
    public async Task<(int total, List<Presupuesto> items)> GetPresupuestos(
        DateTime? desde = null,
        DateTime? hasta = null,
        int? idCliente = null,
        EstadoPresupuesto? estado = null,
        int page = 1,
        int pageSize = 50)
    {
        var url = $"api/presupuestos?page={page}&pageSize={pageSize}";
        if (desde.HasValue) url += $"&desde={desde.Value.ToString("o")}";
        if (hasta.HasValue) url += $"&hasta={hasta.Value.ToString("o")}";
        if (idCliente.HasValue) url += $"&idCliente={idCliente.Value}";
        if (estado.HasValue) url += $"&estado={(int)estado.Value}";
        try
        {
            var resp = await _http.GetFromJsonAsync<PagedResult<Presupuesto>>(url, _json);
            return (resp?.Total ?? 0, resp?.Items ?? []);
        }
        catch { return (0, []); }
    }

    public async Task<Presupuesto?> GetPresupuesto(long id)
    {
        try { return await _http.GetFromJsonAsync<Presupuesto>($"api/presupuestos/{id}", _json); }
        catch { return null; }
    }

    public async Task<Presupuesto?> CrearPresupuesto(Presupuesto p)
    {
        var r = await _http.PostAsJsonAsync("api/presupuestos", p, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Presupuesto>(_json);
    }

    public async Task ActualizarPresupuesto(Presupuesto p)
    {
        var r = await _http.PutAsJsonAsync($"api/presupuestos/{p.Id}", p, _json);
        r.EnsureSuccessStatusCode();
    }

    public async Task EliminarPresupuesto(long id)
    {
        var r = await _http.DeleteAsync($"api/presupuestos/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<bool> FacturarPresupuesto(long id, int idCaja, int idMedioPago, int idTipoComprobante, string? letra, int puntoVenta)
    {
        var req = new
        {
            IdCaja = idCaja,
            IdMedioPago = idMedioPago,
            IdTipoComprobante = idTipoComprobante,
            Letra = letra,
            PuntoVenta = puntoVenta
        };
        var r = await _http.PostAsJsonAsync($"api/presupuestos/{id}/facturar", req, _json);
        return r.IsSuccessStatusCode;
    }

    // === COTIZACIONES ===
    public async Task<(int total, List<Cotizacion> items)> GetCotizaciones(
        DateTime? desde = null,
        DateTime? hasta = null,
        int? idProveedor = null,
        int page = 1,
        int pageSize = 50)
    {
        var url = $"api/cotizaciones?page={page}&pageSize={pageSize}";
        if (desde.HasValue) url += $"&desde={desde.Value.ToString("o")}";
        if (hasta.HasValue) url += $"&hasta={hasta.Value.ToString("o")}";
        if (idProveedor.HasValue) url += $"&idProveedor={idProveedor.Value}";
        try
        {
            var resp = await _http.GetFromJsonAsync<PagedResult<Cotizacion>>(url, _json);
            return (resp?.Total ?? 0, resp?.Items ?? []);
        }
        catch { return (0, []); }
    }

    public async Task<Cotizacion?> GetCotizacion(long id)
    {
        try { return await _http.GetFromJsonAsync<Cotizacion>($"api/cotizaciones/{id}", _json); }
        catch { return null; }
    }

    public async Task<Cotizacion?> CrearCotizacion(Cotizacion c)
    {
        var r = await _http.PostAsJsonAsync("api/cotizaciones", c, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<Cotizacion>(_json);
    }

    public async Task ActualizarCotizacion(Cotizacion c)
    {
        var r = await _http.PutAsJsonAsync($"api/cotizaciones/{c.Id}", c, _json);
        r.EnsureSuccessStatusCode();
    }

    public async Task EliminarCotizacion(long id)
    {
        var r = await _http.DeleteAsync($"api/cotizaciones/{id}");
        r.EnsureSuccessStatusCode();
    }

    // === CONTABILIDAD ===
    public async Task<byte[]?> DownloadLibroIvaVentasCbte(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/libro-iva-ventas-cbte?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadLibroIvaVentasAlic(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/libro-iva-ventas-alic?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadLibroIvaComprasCbte(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/libro-iva-compras-cbte?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadLibroIvaComprasAlic(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/libro-iva-compras-alic?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadPercepcionesIvaVentas(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/percepciones-iva-ventas?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadPercepcionesIIBBCompras(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/percepciones-iibb-compras?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadResumenVentasCsv(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/resumen-ventas-csv?mes={mes}&anio={anio}");

    public async Task<byte[]?> DownloadResumenComprasCsv(int mes, int anio) =>
        await DownloadBytes($"api/contabilidad/resumen-compras-csv?mes={mes}&anio={anio}");

    private async Task<byte[]?> DownloadBytes(string url)
    {
        var resp = await _http.GetAsync(url);
        if (resp.IsSuccessStatusCode)
        {
            return await resp.Content.ReadAsByteArrayAsync();
        }
        var errorContent = await resp.Content.ReadAsStringAsync();
        throw new Exception(errorContent);
    }
}

public class ProveedorSimple
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = "";
    public string Cuit { get; set; } = "";
    public override string ToString() => RazonSocial;
}

public class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = [];
}

public class ListaPrecioProveedorResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int IdProveedor { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("proveedor")]
    public string? ProveedorNombre { get; set; }
    public DateTime FechaCargaUtc { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("archivoOrigenNombre")]
    public string? ArchivoOrigenNombre { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("lineasCount")]
    public int LineasCount { get; set; }
}

public class ListaLineaUpdateDto
{
    public string? CodigoProveedor { get; set; }
    public string? Descripcion { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public decimal? IvaPorcentaje { get; set; }
    public string? BonificacionesJson { get; set; }
    public int? IdArticulo { get; set; }
}

public record ImportarListaProveedorResult(bool Exito, string? Error, int? IdNueva, int? LineasCreadas, string? CuerpoRaw);

// ─── DTOs IA ─────────────────────────────────────────────────────────────────

public class AiChatMensajeDto
{
    public string Rol { get; set; } = "user";
    public string Contenido { get; set; } = string.Empty;
}

public class AiRespuestaDto
{
    public bool Exito { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string? Error { get; set; }
    public bool? BusquedaWebAplicada { get; set; }
    public int? SugerenciasTotalBajoMinimo { get; set; }
    public int? SugerenciasIncluidas { get; set; }
    public List<AiSugerenciaCompraDto>? SugerenciasCompra { get; set; }
    public List<AiAlertaVencimientoDto>? AlertasVencimiento { get; set; }
    public AiAnalisisVentasDto? AnalisisVentas { get; set; }
}

public class AiSugerenciaCompraDto
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal StockMaximo { get; set; }
    public decimal CantidadSugerida { get; set; }
    public decimal CantidadVendida30Dias { get; set; }
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal TotalEstimado { get; set; }
    public decimal AlicuotaIva { get; set; }
    public string Prioridad { get; set; } = "Media";
    [System.Text.Json.Serialization.JsonPropertyName("precioListaCompraReciente")]
    public decimal? PrecioListaCompraReciente { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("nombreTarifaCompra")]
    public string? NombreTarifaCompra { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("fechaTarifaCompra")]
    public System.DateTime? FechaTarifaCompra { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("bonifTarifaCompra")]
    public string? BonifTarifaCompra { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("origenSugerencia")]
    public string? OrigenSugerencia { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("velocidadVentaDiaria")]
    public decimal VelocidadVentaDiaria { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("coberturaDiasAproximada")]
    public int? CoberturaDiasAproximada { get; set; }
}

public class AiAlertaVencimientoDto
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? LoteNro { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int DiasRestantes { get; set; }
    public decimal Cantidad { get; set; }
    public string Urgencia { get; set; } = "Normal";
}

public class AiAnalisisVentasDto
{
    public int DiasAnalizados { get; set; }
    public decimal TotalFacturado { get; set; }
    public int CantidadVentas { get; set; }
    public decimal TicketPromedio { get; set; }
    public List<AiTopProductoDto> TopProductos { get; set; } = [];
}

public class AiTopProductoDto
{
    public int IdArticulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalFacturado { get; set; }
}
