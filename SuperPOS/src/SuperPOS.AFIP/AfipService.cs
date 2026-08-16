using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace SuperPOS.AFIP;

/// <summary>
/// Servicio completo de integración con AFIP WSAA + WSFE (Factura Electrónica Argentina).
/// Implementa firma CMS real con certificado digital .p12, caché de token y todos los
/// métodos necesarios para operar en homologación y producción.
/// </summary>
public class AfipService
{
    // ══════════════════════════════════════════
    // URLs AFIP
    // ══════════════════════════════════════════
    private const string WsaaHomologacion = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
    private const string WsaaProduccion   = "https://wsaa.afip.gov.ar/ws/services/LoginCms";
    private const string WsfeHomologacion = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
    private const string WsfeProduccion   = "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

    private readonly HttpClient  _http;
    private readonly AfipConfig  _config;
    private AfipToken?           _token;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public AfipService(AfipConfig config)
    {
        _config = config;
        _http   = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Add("User-Agent", "SuperPOS/1.0");

        // Intentar cargar token cacheado de disco
        _token = CargarTokenDisco();
    }

    // ══════════════════════════════════════════════════════════════════
    // 1. Autenticación WSAA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene o renueva el token WSAA. Reutiliza mientras no expire (con margen de 5 min).
    /// </summary>
    public async Task<AfipToken> ObtenerTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            if (_token != null && _token.Expiracion > DateTime.UtcNow.AddMinutes(5))
                return _token;

            if (_config.ModoDemo)
            {
                _token = new AfipToken
                {
                    Token      = "TOKEN_DEMO",
                    Sign       = "SIGN_DEMO",
                    Expiracion = DateTime.UtcNow.AddHours(12)
                };
                return _token;
            }

            _token = await LoginWSAAAsync();
            GuardarTokenDisco(_token);
            return _token;
        }
        finally { _tokenLock.Release(); }
    }

    private async Task<AfipToken> LoginWSAAAsync()
    {
        if (string.IsNullOrEmpty(_config.RutaCertificado))
            throw new InvalidOperationException("No se configuró RutaCertificado para AFIP.");

        var tra = GenerarTRA("wsfe");
        var cms = FirmarTRAConCertificado(tra);
        var soap = BuildLoginSoap(cms);
        var url  = _config.Homologacion ? WsaaHomologacion : WsaaProduccion;

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(soap, Encoding.UTF8, "text/xml")
        };
        req.Headers.Add("SOAPAction", "");

        var resp = await _http.SendAsync(req);
        var xml  = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"WSAA error HTTP {resp.StatusCode}: {xml}");

        return ParsearRespuestaLogin(xml);
    }

    /// <summary>
    /// Genera el TRA (Ticket de Requerimiento de Acceso) para el servicio indicado.
    /// </summary>
    private static string GenerarTRA(string servicio)
    {
        var ahora = DateTime.UtcNow;
        var uniqueId = (long)(ahora - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        return $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <loginTicketRequest version="1.0">
                  <header>
                    <uniqueId>{uniqueId}</uniqueId>
                    <generationTime>{ahora.AddMinutes(-10):yyyy-MM-ddTHH:mm:ss}</generationTime>
                    <expirationTime>{ahora.AddHours(12):yyyy-MM-ddTHH:mm:ss}</expirationTime>
                  </header>
                  <service>{servicio}</service>
                </loginTicketRequest>
                """;
    }

    /// <summary>
    /// Firma el TRA con el certificado .p12 usando CMS / PKCS#7 (requerido por AFIP WSAA).
    /// </summary>
    private string FirmarTRAConCertificado(string tra)
    {
        var password   = _config.PasswordCertificado ?? "";
        var certBytes  = File.ReadAllBytes(_config.RutaCertificado!);
        var cert       = new X509Certificate2(certBytes, password,
                            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

        var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(tra));
        var signedCms   = new SignedCms(contentInfo, detached: false);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert)
        {
            IncludeOption = X509IncludeOption.EndCertOnly
        };
        // AFIP WSAA acepta SHA-256
        signer.DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1");

        signedCms.ComputeSignature(signer, silent: true);
        return Convert.ToBase64String(signedCms.Encode());
    }

    private static string BuildLoginSoap(string cms) =>
        $"""
         <?xml version="1.0" encoding="UTF-8"?>
         <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
           <soapenv:Body>
             <loginCms xmlns="https://wsaa.afip.gov.ar/ws/services/LoginCms">
               <in0>{cms}</in0>
             </loginCms>
           </soapenv:Body>
         </soapenv:Envelope>
         """;

    private static AfipToken ParsearRespuestaLogin(string xml)
    {
        try
        {
            var doc      = XDocument.Parse(xml);
            var creds    = doc.Descendants("credentials").FirstOrDefault()
                        ?? throw new InvalidOperationException("No se encontró <credentials> en respuesta WSAA.");
            var expNs    = doc.Descendants("expirationTime").FirstOrDefault();
            var expiracion = DateTime.UtcNow.AddHours(12);
            if (expNs is not null && DateTime.TryParse(expNs.Value, out var expParsed))
                expiracion = expParsed.ToUniversalTime();

            return new AfipToken
            {
                Token      = creds.Element("token")?.Value ?? "",
                Sign       = creds.Element("sign")?.Value  ?? "",
                Expiracion = expiracion
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al parsear respuesta WSAA: {ex.Message}\nXML: {xml}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. WSFE — Métodos de negocio
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Solicita CAE para un comprobante electrónico.
    /// Tipos: 1=FA, 6=FB, 11=FC, 2=NDA, 7=NDB, 12=NDC, 3=NCA, 8=NCB, 13=NCC
    /// </summary>
    public async Task<SolicitudCAEResult> SolicitarCAEAsync(SolicitudCAE solicitud)
    {
        if (_config.ModoDemo)
        {
            return new SolicitudCAEResult
            {
                Exito               = true,
                CAE                 = $"6{DateTime.Now:yyMMddHHmmss}9",
                FechaVencimientoCAE = DateTime.UtcNow.Date.AddDays(10),
                NroComprobante      = solicitud.NroComprobante,
                Observaciones       = "MODO DEMO — sin conexión a AFIP"
            };
        }

        var token = await ObtenerTokenAsync();
        var soap  = BuildFECAESolicitarSoap(solicitud, token);
        var url   = _config.Homologacion ? WsfeHomologacion : WsfeProduccion;

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(soap, Encoding.UTF8, "text/xml")
        };
        req.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECAESolicitar");

        var resp = await _http.SendAsync(req);
        var xml  = await resp.Content.ReadAsStringAsync();
        var resultado = ParsearRespuestaCAE(xml, solicitud.NroComprobante);
        resultado.RequestXml  = soap;
        resultado.ResponseXml = xml;

        // Error 10016 = "el número de comprobante no es el siguiente al último autorizado": casi siempre
        // significa que ESTE comprobante ya fue autorizado antes (ej. la venta se guardó pero la respuesta
        // se perdió por timeout) y AFIP rechaza el reintento por no ser "el próximo". En vez de dejar la
        // venta sin CAE, recuperamos el que ya existe en vez de fallar.
        if (!resultado.Exito && resultado.CodigosError.Contains("10016"))
        {
            var recuperado = await ConsultarComprobanteAsync(solicitud.PuntoVenta, solicitud.TipoComprobante, solicitud.NroComprobante);
            if (recuperado?.CAE is { Length: > 0 })
            {
                resultado.Exito               = true;
                resultado.CAE                 = recuperado.CAE;
                resultado.FechaVencimientoCAE = recuperado.FechaVencimiento ?? DateTime.UtcNow.Date.AddDays(10);
                resultado.Recuperado          = true;
                resultado.Observaciones       = $"CAE recuperado de AFIP tras error 10016 (reintento). {resultado.Error}";
                resultado.Error               = null;
            }
        }

        return resultado;
    }

    /// <summary>
    /// Devuelve el número del último comprobante autorizado para un punto de venta y tipo.
    /// </summary>
    public async Task<long> ObtenerUltimoComprobanteAsync(int puntoVenta, int tipoComprobante)
    {
        if (_config.ModoDemo) return 0;

        var token = await ObtenerTokenAsync();
        var soap  = $"""
                     <?xml version="1.0" encoding="UTF-8"?>
                     <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
                       <soapenv:Body>
                         <ar:FECompUltimoAutorizado>
                           <ar:Auth>
                             <ar:Token>{token.Token}</ar:Token>
                             <ar:Sign>{token.Sign}</ar:Sign>
                             <ar:Cuit>{_config.CUIT}</ar:Cuit>
                           </ar:Auth>
                           <ar:PtoVta>{puntoVenta}</ar:PtoVta>
                           <ar:CbteTipo>{tipoComprobante}</ar:CbteTipo>
                         </ar:FECompUltimoAutorizado>
                       </soapenv:Body>
                     </soapenv:Envelope>
                     """;

        var url = _config.Homologacion ? WsfeHomologacion : WsfeProduccion;
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(soap, Encoding.UTF8, "text/xml")
        };
        req.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado");

        var resp = await _http.SendAsync(req);
        var xml  = await resp.Content.ReadAsStringAsync();

        try
        {
            var doc = XDocument.Parse(xml);
            var ns  = XNamespace.Get("http://ar.gov.afip.dif.FEV1/");
            var nro = doc.Descendants(ns + "CbteNro").FirstOrDefault()?.Value;
            return long.TryParse(nro, out var n) ? n : 0;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Verifica el estado de los servidores AFIP (AppServer, AuthServer, DbServer).
    /// </summary>
    public async Task<AfipServerStatus> FEDummyAsync()
    {
        var soap = """
                   <?xml version="1.0" encoding="UTF-8"?>
                   <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
                     <soapenv:Body>
                       <ar:FEDummy/>
                     </soapenv:Body>
                   </soapenv:Envelope>
                   """;

        try
        {
            var url = _config.Homologacion ? WsfeHomologacion : WsfeProduccion;
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(soap, Encoding.UTF8, "text/xml")
            };
            req.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FEDummy");

            var resp = await _http.SendAsync(req);
            var xml  = await resp.Content.ReadAsStringAsync();
            var doc  = XDocument.Parse(xml);
            var ns   = XNamespace.Get("http://ar.gov.afip.dif.FEV1/");
            return new AfipServerStatus
            {
                AppServer  = doc.Descendants(ns + "AppServer").FirstOrDefault()?.Value ?? "?",
                AuthServer = doc.Descendants(ns + "AuthServer").FirstOrDefault()?.Value ?? "?",
                DbServer   = doc.Descendants(ns + "DbServer").FirstOrDefault()?.Value ?? "?",
                Online     = true
            };
        }
        catch (Exception ex)
        {
            return new AfipServerStatus { Online = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Consulta un comprobante ya autorizado en AFIP para verificar su estado.
    /// </summary>
    public async Task<ComprobanteAfipInfo?> ConsultarComprobanteAsync(int puntoVenta, int tipoComprobante, long nroComprobante)
    {
        if (_config.ModoDemo) return null;

        var token = await ObtenerTokenAsync();
        var soap  = $"""
                     <?xml version="1.0" encoding="UTF-8"?>
                     <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
                       <soapenv:Body>
                         <ar:FECompConsultar>
                           <ar:Auth>
                             <ar:Token>{token.Token}</ar:Token>
                             <ar:Sign>{token.Sign}</ar:Sign>
                             <ar:Cuit>{_config.CUIT}</ar:Cuit>
                           </ar:Auth>
                           <ar:FeCompConsReq>
                             <ar:CbteTipo>{tipoComprobante}</ar:CbteTipo>
                             <ar:CbteNro>{nroComprobante}</ar:CbteNro>
                             <ar:PtoVta>{puntoVenta}</ar:PtoVta>
                           </ar:FeCompConsReq>
                         </ar:FECompConsultar>
                       </soapenv:Body>
                     </soapenv:Envelope>
                     """;

        var url = _config.Homologacion ? WsfeHomologacion : WsfeProduccion;
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(soap, Encoding.UTF8, "text/xml")
        };
        req.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FECompConsultar");

        var resp = await _http.SendAsync(req);
        var xml  = await resp.Content.ReadAsStringAsync();

        try
        {
            var doc = XDocument.Parse(xml);
            var ns  = XNamespace.Get("http://ar.gov.afip.dif.FEV1/");
            var det = doc.Descendants(ns + "FECompConsultarResult").FirstOrDefault();
            if (det is null) return null;

            var cae    = det.Descendants(ns + "CodAut").FirstOrDefault()?.Value;
            var vtoStr = det.Descendants(ns + "FchVto").FirstOrDefault()?.Value;
            return new ComprobanteAfipInfo
            {
                CAE              = cae,
                FechaVencimiento = DateTime.TryParseExact(vtoStr, "yyyyMMdd", null, DateTimeStyles.None, out var fvto) ? fvto : null,
                Resultado        = det.Descendants(ns + "Resultado").FirstOrDefault()?.Value ?? "?"
            };
        }
        catch { return null; }
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. Builders SOAP WSFE
    // ══════════════════════════════════════════════════════════════════

    private string BuildFECAESolicitarSoap(SolicitudCAE s, AfipToken token)
    {
        var fmt    = CultureInfo.InvariantCulture;
        var fecha  = s.Fecha.ToString("yyyyMMdd");
        var ivasXml = s.Ivas.Count > 0 ? BuildIvasXml(s.Ivas) : "";

        // Campos de servicio (Concepto 2 o 3)
        var servicioXml = s.Concepto is 2 or 3
            ? $"""
               <ar:FchServDesde>{s.FchServDesde ?? fecha}</ar:FchServDesde>
               <ar:FchServHasta>{s.FchServHasta ?? fecha}</ar:FchServHasta>
               <ar:FchVtoPago>{s.FchVtoPago ?? fecha}</ar:FchVtoPago>
               """
            : "";

        return $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
                  <soapenv:Body>
                    <ar:FECAESolicitar>
                      <ar:Auth>
                        <ar:Token>{token.Token}</ar:Token>
                        <ar:Sign>{token.Sign}</ar:Sign>
                        <ar:Cuit>{_config.CUIT}</ar:Cuit>
                      </ar:Auth>
                      <ar:FeCAEReq>
                        <ar:FeCabReq>
                          <ar:CantReg>1</ar:CantReg>
                          <ar:PtoVta>{s.PuntoVenta}</ar:PtoVta>
                          <ar:CbteTipo>{s.TipoComprobante}</ar:CbteTipo>
                        </ar:FeCabReq>
                        <ar:FeDetReq>
                          <ar:FECAEDetRequest>
                            <ar:Concepto>{s.Concepto}</ar:Concepto>
                            <ar:DocTipo>{s.TipoDocCliente}</ar:DocTipo>
                            <ar:DocNro>{s.NroDocCliente}</ar:DocNro>
                            <ar:CbteDesde>{s.NroComprobante}</ar:CbteDesde>
                            <ar:CbteHasta>{s.NroComprobante}</ar:CbteHasta>
                            <ar:CbteFch>{fecha}</ar:CbteFch>
                            <ar:ImpTotal>{s.ImporteTotal.ToString("F2", fmt)}</ar:ImpTotal>
                            <ar:ImpTotConc>{s.ImporteTotConc.ToString("F2", fmt)}</ar:ImpTotConc>
                            <ar:ImpNeto>{s.ImporteNeto.ToString("F2", fmt)}</ar:ImpNeto>
                            <ar:ImpOpEx>{s.ImporteOpEx.ToString("F2", fmt)}</ar:ImpOpEx>
                            <ar:ImpIVA>{s.ImporteIva.ToString("F2", fmt)}</ar:ImpIVA>
                            <ar:ImpTrib>0.00</ar:ImpTrib>
                            <ar:MonId>PES</ar:MonId>
                            <ar:MonCotiz>1</ar:MonCotiz>
                            {servicioXml}
                            {ivasXml}
                          </ar:FECAEDetRequest>
                        </ar:FeDetReq>
                      </ar:FeCAEReq>
                    </ar:FECAESolicitar>
                  </soapenv:Body>
                </soapenv:Envelope>
                """;
    }

    private static string BuildIvasXml(List<AfipIva> ivas)
    {
        var fmt = CultureInfo.InvariantCulture;
        var items = string.Join("", ivas.Select(i =>
            $"""
             <ar:AlicIva>
               <ar:Id>{i.IdIva}</ar:Id>
               <ar:BaseImp>{i.BaseImponible.ToString("F2", fmt)}</ar:BaseImp>
               <ar:Importe>{i.Importe.ToString("F2", fmt)}</ar:Importe>
             </ar:AlicIva>
             """));
        return $"<ar:Iva>{items}</ar:Iva>";
    }

    private static SolicitudCAEResult ParsearRespuestaCAE(string xml, long nroComprobante)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var ns  = XNamespace.Get("http://ar.gov.afip.dif.FEV1/");

            // Errores de negocio
            var errores = doc.Descendants(ns + "Err")
                .Select(e => $"[{e.Element(ns + "Code")?.Value}] {e.Element(ns + "Msg")?.Value}")
                .ToList();

            var result    = doc.Descendants(ns + "FECAEDetResponse").FirstOrDefault();
            var resultado = result?.Element(ns + "Resultado")?.Value ?? "R";

            if (resultado == "A")
            {
                var cae    = result?.Element(ns + "CAE")?.Value ?? "";
                var vtoStr = result?.Element(ns + "CAEFchVto")?.Value ?? "";
                var obs    = doc.Descendants(ns + "Obs")
                    .Select(o => $"[{o.Element(ns + "Code")?.Value}] {o.Element(ns + "Msg")?.Value}")
                    .ToList();

                return new SolicitudCAEResult
                {
                    Exito               = true,
                    CAE                 = cae,
                    FechaVencimientoCAE = DateTime.TryParseExact(vtoStr, "yyyyMMdd", null, DateTimeStyles.None, out var dt) ? dt : DateTime.UtcNow.Date.AddDays(10),
                    NroComprobante      = nroComprobante,
                    Observaciones       = obs.Count > 0 ? string.Join("; ", obs) : null
                };
            }

            var errorMsg = errores.Count > 0 ? string.Join("; ", errores) : "Rechazado por AFIP sin detalle.";
            var codigos  = doc.Descendants(ns + "Err").Select(e => e.Element(ns + "Code")?.Value ?? "").Where(c => c != "").ToList();
            return new SolicitudCAEResult
            {
                Exito          = false,
                Error          = errorMsg,
                NroComprobante = nroComprobante,
                CodigosError   = codigos
            };
        }
        catch (Exception ex)
        {
            return new SolicitudCAEResult { Exito = false, Error = $"Error al parsear respuesta WSFE: {ex.Message}", NroComprobante = nroComprobante };
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. Caché de token en disco
    // ══════════════════════════════════════════════════════════════════

    private static string TokenCachePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SuperPOS", "afip_token.json");

    private static AfipToken? CargarTokenDisco()
    {
        try
        {
            if (!File.Exists(TokenCachePath)) return null;
            var json = File.ReadAllText(TokenCachePath);
            return JsonSerializer.Deserialize<AfipToken>(json);
        }
        catch { return null; }
    }

    private static void GuardarTokenDisco(AfipToken token)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TokenCachePath)!);
            File.WriteAllText(TokenCachePath, JsonSerializer.Serialize(token));
        }
        catch { /* no crítico */ }
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. Helpers de tipos de comprobante
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Convierte letra de comprobante (A/B/C) y tipo (Factura/NC/ND) al código AFIP.
    /// Si <paramref name="comision"/> es mayor a 0 y es una Factura A/B, AFIP exige emitirla como
    /// Factura de Crédito Electrónica MiPyME (60=FCE A, 61=FCE B) en vez del tipo estándar (1/6).
    /// </summary>
    public static int ObtenerTipoComprobanteAfip(char letra, TipoComprobanteAfip tipo, decimal comision = 0)
    {
        var codigo = (tipo, letra) switch
        {
            (TipoComprobanteAfip.Factura,      'A') => 1,
            (TipoComprobanteAfip.Factura,      'B') => 6,
            (TipoComprobanteAfip.Factura,      'C') => 11,
            (TipoComprobanteAfip.NotaDebito,   'A') => 2,
            (TipoComprobanteAfip.NotaDebito,   'B') => 7,
            (TipoComprobanteAfip.NotaDebito,   'C') => 12,
            (TipoComprobanteAfip.NotaCredito,  'A') => 3,
            (TipoComprobanteAfip.NotaCredito,  'B') => 8,
            (TipoComprobanteAfip.NotaCredito,  'C') => 13,
            _ => 6
        };
        return AplicarFceSiCorresponde(codigo, comision);
    }

    /// <summary>
    /// Si hay comisión y el código es Factura A/B estándar (1/6), lo convierte a Factura de Crédito
    /// Electrónica MiPyME (60/61) — requerido por AFIP en ese caso. Cualquier otro código (Factura C,
    /// Notas de Débito/Crédito) queda sin cambios.
    /// </summary>
    public static int AplicarFceSiCorresponde(int codigoAfip, decimal comision) => (comision > 0, codigoAfip) switch
    {
        (true, 1) => 60,
        (true, 6) => 61,
        _ => codigoAfip
    };

    /// <summary>Obtiene el Id de alícuota IVA según el porcentaje.</summary>
    public static int ObtenerIdAlicuotaIva(decimal porcentaje) => (int)porcentaje switch
    {
        0  => 3,   // 0%
        10 => 4,   // 10.5%
        21 => 5,   // 21%
        27 => 6,   // 27%
        5  => 8,   // 5%
        2  => 9,   // 2.5%
        _  => 5    // default 21%
    };
}

// ══════════════════════════════════════════════════════════════════
// DTOs y configuración
// ══════════════════════════════════════════════════════════════════

public class AfipConfig
{
    public string  CUIT                 { get; set; } = string.Empty;
    public int     PuntoVenta           { get; set; } = 1;
    /// <summary>true = entorno de homologación (testing), false = producción</summary>
    public bool    Homologacion         { get; set; } = true;
    /// <summary>true = modo demo sin llamadas reales a AFIP (para desarrollo sin certificado)</summary>
    public bool    ModoDemo             { get; set; } = true;
    public string? RutaCertificado      { get; set; }
    public string? PasswordCertificado  { get; set; }
}

public class AfipToken
{
    public string   Token      { get; set; } = string.Empty;
    public string   Sign       { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
}

public class SolicitudCAE
{
    public int     PuntoVenta       { get; set; }
    public int     TipoComprobante  { get; set; }   // 1=FA,6=FB,11=FC,2=NDA,7=NDB,12=NDC,3=NCA,8=NCB,13=NCC
    public long    NroComprobante   { get; set; }
    public DateTime Fecha           { get; set; } = DateTime.UtcNow.Date;
    /// <summary>Concepto: 1=Productos, 2=Servicios, 3=Productos y Servicios</summary>
    public int     Concepto         { get; set; } = 1;
    /// <summary>Tipo de documento del receptor: 80=CUIT, 96=DNI, 99=Consumidor Final</summary>
    public int     TipoDocCliente   { get; set; } = 99;
    public long    NroDocCliente    { get; set; } = 0;
    public decimal ImporteNeto      { get; set; }
    public decimal ImporteIva       { get; set; }
    public decimal ImporteTotal     { get; set; }
    public decimal ImporteTotConc   { get; set; } = 0;
    public decimal ImporteOpEx      { get; set; } = 0;
    /// <summary>Requerido solo si Concepto = 2 o 3 (formato yyyyMMdd)</summary>
    public string? FchServDesde     { get; set; }
    public string? FchServHasta     { get; set; }
    public string? FchVtoPago       { get; set; }
    public List<AfipIva> Ivas       { get; set; } = [];
}

public class AfipIva
{
    /// <summary>Id: 3=0%, 4=10.5%, 5=21%, 6=27%, 8=5%, 9=2.5%</summary>
    public int     IdIva          { get; set; }
    public decimal BaseImponible  { get; set; }
    public decimal Importe        { get; set; }
}

public class SolicitudCAEResult
{
    public bool      Exito               { get; set; }
    public string?   CAE                 { get; set; }
    public DateTime? FechaVencimientoCAE { get; set; }
    public long      NroComprobante      { get; set; }
    public string?   Error               { get; set; }
    public string?   Observaciones       { get; set; }
    /// <summary>Códigos de error de AFIP (ej. "10016"), vacío si Exito=true.</summary>
    public List<string> CodigosError     { get; set; } = [];
    /// <summary>true si el CAE no vino de esta solicitud sino que se recuperó tras un error 10016.</summary>
    public bool      Recuperado          { get; set; }
    public string?   RequestXml          { get; set; }
    public string?   ResponseXml         { get; set; }

    /// <summary>
    /// Genera la URL del QR AFIP según especificación oficial (base64url, JSON interno).
    /// </summary>
    public string? GenerarQRAfip(string cuit, int puntoVenta, int tipoCbte, DateTime fecha, decimal total,
                                 int tipoDocRec = 99, long nroDocRec = 0)
    {
        if (!Exito || string.IsNullOrEmpty(CAE)) return null;

        var datos = new AfipQrData
        {
            ver       = 1,
            fecha     = fecha.ToString("yyyy-MM-dd"),
            cuit      = long.Parse(cuit.Replace("-", "").Replace(" ", "")),
            ptoVta    = puntoVenta,
            tipoCmp   = tipoCbte,
            nroCmp    = (int)NroComprobante,
            importe   = total,
            moneda    = "PES",
            ctz       = 1,
            tipoDocRec = tipoDocRec,
            nroDocRec = nroDocRec,
            tipoCodAut = "E",
            codAut    = long.Parse(CAE!)
        };

        var json    = JsonSerializer.Serialize(datos);
        // AFIP requiere base64url (URL-safe, sin padding)
        var b64     = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return $"https://www.afip.gob.ar/fe/qr/?p={b64}";
    }
}

/// <summary>Datos del QR según especificación AFIP RG4291.</summary>
internal class AfipQrData
{
    public int    ver        { get; set; }
    public string fecha      { get; set; } = "";
    public long   cuit       { get; set; }
    public int    ptoVta     { get; set; }
    public int    tipoCmp    { get; set; }
    public int    nroCmp     { get; set; }
    public decimal importe   { get; set; }
    public string moneda     { get; set; } = "PES";
    public int    ctz        { get; set; }
    public int    tipoDocRec { get; set; }
    public long   nroDocRec  { get; set; }
    public string tipoCodAut { get; set; } = "E";
    public long   codAut     { get; set; }
}

public class AfipServerStatus
{
    public string AppServer  { get; set; } = "";
    public string AuthServer { get; set; } = "";
    public string DbServer   { get; set; } = "";
    public bool   Online     { get; set; }
    public string? Error     { get; set; }
}

public class ComprobanteAfipInfo
{
    public string?   CAE              { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string    Resultado        { get; set; } = "";
}

public enum TipoComprobanteAfip
{
    Factura     = 0,
    NotaDebito  = 1,
    NotaCredito = 2
}
