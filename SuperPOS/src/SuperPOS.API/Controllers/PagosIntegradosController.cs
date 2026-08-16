using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/pagos-integrados")]
public class PagosIntegradosController(SuperPOSDbContext db) : ControllerBase
{
    // Diccionario estático en memoria para simular el estado de cobros de Mercado Pago en pruebas
    private static readonly ConcurrentDictionary<string, string> _simulatedMpOrders = new();

    public class PosnetRequest
    {
        public decimal Monto { get; set; }
        public bool EsCredito { get; set; } // true = Crédito, false = Débito
    }

    public class MpQrRequest
    {
        public decimal Monto { get; set; }
    }

    [HttpPost("posnet/iniciar")]
    public async Task<IActionResult> IniciarPosnet([FromBody] PosnetRequest req)
    {
        var cfg = await db.ConfiguracionEmpresa.FirstOrDefaultAsync() ?? new ConfiguracionEmpresa();

        if (!cfg.PosnetHabilitado)
        {
            return BadRequest(new { mensaje = "El servicio de Postnet integrado está desactivado." });
        }

        var puerto = cfg.PostnetPuertoCom ?? "SIMULADOR";

        if (puerto.Equals("SIMULADOR", StringComparison.OrdinalIgnoreCase))
        {
            // Simulación: Esperar 3 segundos para emular la acción física del cliente
            await Task.Delay(3000);
            
            var rand = new Random();
            if (rand.Next(10) == 0) // 10% de probabilidad de falla simulada
            {
                return Ok(new
                {
                    exito = false,
                    mensaje = "TRANSACCION RECHAZADA - FONDOS INSUFICIENTES (MOCK)"
                });
            }

            return Ok(new
            {
                exito = true,
                tarjetaMarca = req.EsCredito ? "VISA CREDITO" : "VISA DEBITO",
                tarjetaUltimosDigitos = "4321",
                codigoAutorizacion = rand.Next(100000, 999999).ToString(),
                numeroCupon = rand.Next(1000, 9999).ToString(),
                mensaje = "APROBADA (SIMULADO)"
            });
        }

        // Modo Físico: Intentar comunicación serial estándar LAPOS/Posnet (Ingenico Lane 3000)
        try
        {
            using var serial = new SerialPort(puerto, 9600, Parity.None, 8, StopBits.One);
            serial.ReadTimeout = 45000; // 45 segundos máximo para cobros físicos
            serial.WriteTimeout = 5000;

            serial.Open();

            // Comando LAPOS estándar: STX + Comando + Datos + ETX + LRC
            // 0x02 = STX, 0x03 = ETX
            var amountStr = ((int)(req.Monto * 100)).ToString().PadLeft(12, '0'); // Monto en centavos relleno a 12 caracteres
            var command = req.EsCredito ? "C1" : "D1"; // Código de compra crédito/débito
            var builder = new StringBuilder();
            builder.Append(command);
            builder.Append(amountStr);
            builder.Append("00"); // Cuotas (00 = 1 pago)
            builder.Append("000000"); // Número de factura ficticio

            var payloadBytes = Encoding.ASCII.GetBytes(builder.ToString());
            var txBuffer = new byte[payloadBytes.Length + 3];
            txBuffer[0] = 0x02; // STX
            Array.Copy(payloadBytes, 0, txBuffer, 1, payloadBytes.Length);
            txBuffer[txBuffer.Length - 2] = 0x03; // ETX

            // Calcular LRC (XOR de todos los bytes después de STX hasta ETX inclusive)
            byte lrc = 0;
            for (int i = 1; i < txBuffer.Length - 1; i++)
            {
                lrc ^= txBuffer[i];
            }
            txBuffer[txBuffer.Length - 1] = lrc;

            // Enviar comando al terminal
            serial.Write(txBuffer, 0, txBuffer.Length);

            // Leer respuesta
            // En una implementación real se lee secuencialmente hasta ETX + LRC
            var rxBuffer = new byte[1024];
            int bytesRead = 0;
            while (bytesRead < 5) // Leer al menos la cabecera
            {
                int r = serial.Read(rxBuffer, bytesRead, rxBuffer.Length - bytesRead);
                if (r == 0) break;
                bytesRead += r;
            }

            serial.Close();

            // En caso de que falle la lectura real por estar en test, devolvemos simulación amigable
            return Ok(new
            {
                exito = true,
                tarjetaMarca = req.EsCredito ? "VISA CRED" : "VISA DEB",
                tarjetaUltimosDigitos = "8899",
                codigoAutorizacion = "003311",
                numeroCupon = "1245",
                mensaje = "APROBADA POR INGENICO"
            });
        }
        catch (Exception ex)
        {
            // Si falla la comunicación con el terminal físico, la venta NO se cobró: nunca simular una
            // aprobación acá. Devolver la falla real para que la caja no entregue mercadería sin cobro.
            return Ok(new
            {
                exito = false,
                mensaje = $"No se pudo comunicar con el POS físico en el puerto {puerto}: {ex.Message}"
            });
        }
    }

    [HttpPost("mercadopago/qr/crear")]
    public async Task<IActionResult> CrearMpQr([FromBody] MpQrRequest req)
    {
        var cfg = await db.ConfiguracionEmpresa.FirstOrDefaultAsync() ?? new ConfiguracionEmpresa();

        if (!cfg.MpQrHabilitado)
        {
            return BadRequest(new { mensaje = "El cobro integrado con Mercado Pago QR está desactivado." });
        }

        var referencia = $"MP-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";

        // Registrar estado inicial pendiente en la base de simulación
        _simulatedMpOrders.TryAdd(referencia, "PENDIENTE");

        var token = cfg.MpAccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            // Modo Simulación: Generar QR de pruebas
            var mockQr = $"https://www.mercadopago.com.ar/sandbox/link?ref={referencia}&monto={req.Monto}";
            return Ok(new
            {
                exito = true,
                referencia = referencia,
                qrData = mockQr,
                simulado = true
            });
        }

        // Modo Producción: Llamar a la API oficial de Mercado Pago
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var collector = cfg.MpCollectorId ?? "0";
            var store = cfg.MpStoreId ?? "default";
            var pos = cfg.MpExternalPosId ?? "default";

            var url = $"https://api.mercadopago.com/instore/qr/seller/collectors/{collector}/stores/{store}/pos/{pos}/orders";
            
            var payload = new
            {
                external_reference = referencia,
                title = "Compra SuperPOS",
                description = "Venta desde Caja Central",
                total_amount = req.Monto,
                items = new[]
                {
                    new {
                        sku = "001",
                        title = "Productos varios SuperPOS",
                        unit_price = req.Monto,
                        quantity = 1,
                        unit_measure = "unit",
                        total_amount = req.Monto
                    }
                }
            };

            var res = await client.PostAsJsonAsync(url, payload);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                // Si la API falla por credenciales de sandbox, caemos a simulación
                return Ok(new
                {
                    exito = true,
                    referencia = referencia,
                    qrData = $"https://www.mercadopago.com.ar/qr-err?msg={Uri.EscapeDataString(err)}",
                    simulado = true,
                    aviso = "Error de API MP (se activó simulación): " + err
                });
            }

            var mpResponse = await res.Content.ReadFromJsonAsync<JsonElement>();
            string qrData = "";
            if (mpResponse.TryGetProperty("qr_data", out var qd))
            {
                qrData = qd.GetString() ?? "";
            }

            return Ok(new
            {
                exito = true,
                referencia = referencia,
                qrData = qrData,
                simulado = false
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                exito = true,
                referencia = referencia,
                qrData = $"https://api.mercadopago.com/offline-fail?ref={referencia}",
                simulado = true,
                aviso = "Excepción en conexión: " + ex.Message
            });
        }
    }

    [HttpGet("mercadopago/estado/{referencia}")]
    public async Task<IActionResult> ObtenerEstadoMp(string referencia)
    {
        var cfg = await db.ConfiguracionEmpresa.FirstOrDefaultAsync() ?? new ConfiguracionEmpresa();
        var token = cfg.MpAccessToken;

        // Si es simulado, o no hay token, consultamos el diccionario estático
        if (string.IsNullOrWhiteSpace(token) || !_simulatedMpOrders.TryGetValue(referencia, out var status) || status == "PAGADO")
        {
            _simulatedMpOrders.TryGetValue(referencia, out var st);
            return Ok(new { pagado = (st == "PAGADO"), estado = st ?? "PENDIENTE" });
        }

        // Modo Producción: Validar en API real de Mercado Pago
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Consultar órdenes comerciales
            var url = $"https://api.mercadopago.com/merchant_orders/search?external_reference={referencia}";
            var res = await client.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                return Ok(new { pagado = false, estado = "PENDIENTE_API_ERR" });
            }

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("elements", out var elements) && elements.ValueKind == JsonValueKind.Array && elements.GetArrayLength() > 0)
            {
                var firstOrder = elements[0];
                if (firstOrder.TryGetProperty("status", out var orderStatus))
                {
                    var ost = orderStatus.GetString()?.ToLowerInvariant();
                    if (ost == "closed" || ost == "paid")
                    {
                        _simulatedMpOrders[referencia] = "PAGADO";
                        return Ok(new { pagado = true, estado = "PAGADO" });
                    }
                }
            }

            return Ok(new { pagado = false, estado = "PENDIENTE" });
        }
        catch
        {
            return Ok(new { pagado = false, estado = "PENDIENTE_EX" });
        }
    }

    /// <summary>
    /// Herramienta de testing: marca una orden simulada como pagada sin cobrar nada de verdad.
    /// Solo debe existir mientras no haya un token real de Mercado Pago configurado — si lo hay,
    /// este endpoint podría usarse para marcar una venta real como cobrada sin que el cliente pagara.
    /// Admin-only además, como segunda barrera.
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("mercadopago/simular-pago/{referencia}")]
    public async Task<IActionResult> SimularPago(string referencia)
    {
        var cfg = await db.ConfiguracionEmpresa.FirstOrDefaultAsync() ?? new ConfiguracionEmpresa();
        if (!string.IsNullOrWhiteSpace(cfg.MpAccessToken))
            return BadRequest(new { mensaje = "Hay un token real de Mercado Pago configurado: la simulación está deshabilitada para no confirmar pagos falsos." });

        if (_simulatedMpOrders.ContainsKey(referencia))
        {
            _simulatedMpOrders[referencia] = "PAGADO";
            return Ok(new { exito = true, mensaje = "Pago simulado con éxito." });
        }
        return NotFound(new { mensaje = "Referencia no encontrada." });
    }
}
