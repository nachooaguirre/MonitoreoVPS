using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Services;

public class AlertasBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlertasBackgroundService> _logger;
    private bool _isProcessing;
    private TcpListener? _tcpListener;

    public AlertasBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AlertasBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando servicio de Alertas...");

        // 1. Iniciar servidor TCP Socket en puerto 9905
        var port = _configuration.GetValue<int>("Alertas:PuertoAlertas", 9905);
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            _logger.LogInformation("Servidor TCP de Alertas escuchando en puerto {Port}", port);

            // Escuchar clientes TCP de forma asíncrona
            _ = EscucharClientesTcpAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar el servidor TCP de Alertas en el puerto {Port}", port);
        }

        // 2. Ejecutar timer periódico para correr todas las alertas
        var intervalMs = _configuration.GetValue<int>("Alertas:IntervaloTimerAlertas", 1499700); // 25 min default
        _logger.LogInformation("Ejecución periódica de Alertas configurada cada {Interval} ms", intervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(intervalMs, stoppingToken);

                _logger.LogInformation("Ejecutando alertas programadas periódicamente...");
                await RunAlertsAsync(-1, stoppingToken);
                await EnviarMailingsPendientesAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Apagado normal
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo periódico del servicio de Alertas");
            }
        }

        _logger.LogInformation("Deteniendo servicio de Alertas...");
        _tcpListener?.Stop();
    }

    /// <summary>Envía por SMTP los mailings encolados con Estado='P' (generados por RunAlertsAsync).</summary>
    private async Task EnviarMailingsPendientesAsync(CancellationToken stoppingToken)
    {
        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
            return; // SMTP no configurado: los mailings quedan pendientes hasta que se configure

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SuperPOSDbContext>();

        var pendientes = await db.Mailings.Where(m => m.Estado == 'P').ToListAsync(stoppingToken);
        if (pendientes.Count == 0) return;

        var port = _configuration.GetValue<int>("Smtp:Port", 587);
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);
        var from = _configuration["Smtp:From"] ?? user ?? "no-reply@superpos.local";

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, password);

        foreach (var mail in pendientes)
        {
            try
            {
                using var msg = new MailMessage(from, mail.Destino, mail.Asunto, mail.Cuerpo);
                await client.SendMailAsync(msg, stoppingToken);
                mail.Estado = 'E';
            }
            catch (Exception ex)
            {
                mail.Estado = 'F';
                _logger.LogError(ex, "Error al enviar mailing {Id} a {Destino}", mail.Id, mail.Destino);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private async Task EscucharClientesTcpAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _tcpListener != null)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync(stoppingToken);
                _ = ProcesarClienteTcpAsync(client, stoppingToken);
            }
            catch (ObjectDisposedException)
            {
                break; // Listener cerrado
            }
            catch (Exception ex)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error al aceptar cliente TCP");
                }
            }
        }
    }

    private async Task ProcesarClienteTcpAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                if (bytesRead == 0) return;

                var command = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                _logger.LogInformation("Comando TCP recibido: '{Command}'", command);

                if (command.Equals("ACTIVAR", StringComparison.OrdinalIgnoreCase))
                {
                    await LogToFileAsync("Alerta ServerSocket: Activar");
                    // No responde nada (igual al legacy)
                }
                else if (command.Equals("TODAS", StringComparison.OrdinalIgnoreCase))
                {
                    if (_isProcessing)
                    {
                        var responseBytes = Encoding.ASCII.GetBytes("OCUPADO");
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, stoppingToken);
                    }
                    else
                    {
                        await LogToFileAsync("Alerta ServerSocket: Leer Todas Las Alertas");
                        // Procesar de forma asíncrona
                        _ = Task.Run(async () =>
                        {
                            await RunAlertsAsync(-1, CancellationToken.None);
                            try
                            {
                                var responseBytes = Encoding.ASCII.GetBytes("TODAS");
                                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                            }
                            catch { }
                        }, CancellationToken.None);
                    }
                }
                else
                {
                    // Intentar parsear como ID de Alerta
                    if (int.TryParse(command, out var idAlerta))
                    {
                        if (_isProcessing)
                        {
                            var responseBytes = Encoding.ASCII.GetBytes("OCUPADO");
                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, stoppingToken);
                        }
                        else
                        {
                            await LogToFileAsync($"Alerta ServerSocket: Leer Alerta {idAlerta}");
                            _ = Task.Run(async () =>
                            {
                                await RunAlertsAsync(idAlerta, CancellationToken.None);
                                try
                                {
                                    var responseBytes = Encoding.ASCII.GetBytes("ALERTA");
                                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                                }
                                catch { }
                            }, CancellationToken.None);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Comando TCP no reconocido: '{Command}'", command);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar cliente TCP");
            }
        }
    }

    public async Task RunAlertsAsync(int idAlerta, CancellationToken cancellationToken)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SuperPOSDbContext>();

            var todayStr = DateTime.Now.DayOfWeek switch
            {
                DayOfWeek.Sunday => "DO",
                DayOfWeek.Monday => "LU",
                DayOfWeek.Tuesday => "MA",
                DayOfWeek.Wednesday => "MI",
                DayOfWeek.Thursday => "JU",
                DayOfWeek.Friday => "VI",
                DayOfWeek.Saturday => "SA",
                _ => "DO"
            };

            var now = DateTime.UtcNow;

            var query = db.Alertas
                .Include(a => a.AlertasUsuarios)
                    .ThenInclude(au => au.Usuario)
                .Where(a => a.Activo 
                    && a.DiasSemanaAlerta.Contains(todayStr)
                    && now >= a.FechaDesde && now <= a.FechaHasta);

            if (idAlerta != -1)
            {
                query = query.Where(a => a.Id == idAlerta);
            }

            var alerts = await query.ToListAsync(cancellationToken);
            _logger.LogInformation("Procesando {Count} alertas programadas para el día de hoy...", alerts.Count);

            foreach (var alert in alerts)
            {
                if (!IsSafeSql(alert.ConsultaSQL))
                {
                    await LogToFileAsync($"ALERTA SEGURIDAD: Consulta SQL no permitida para Alerta {alert.Id}: {alert.ConsultaSQL}");
                    continue;
                }

                try
                {
                    var results = await ExecuteCustomQueryAsync(db, alert.ConsultaSQL, cancellationToken);
                    if (results.Count > 0)
                    {
                        var json = JsonSerializer.Serialize(results);
                        var newMd5 = CalculateMd5(json);

                        foreach (var au in alert.AlertasUsuarios)
                        {
                            if (au.Usuario == null) continue;

                            var lastSent = await db.AlertasEnviadas
                                .Where(ae => ae.IdAlerta == alert.Id && ae.IdUsuario == au.Usuario.Id)
                                .OrderByDescending(ae => ae.FechaHoraCreacion)
                                .FirstOrDefaultAsync(cancellationToken);

                            bool mustSend = false;
                            if (lastSent == null)
                            {
                                mustSend = true;
                            }
                            else
                            {
                                var isToday = lastSent.FechaHoraCreacion.ToLocalTime().Date == DateTime.Today;
                                if (lastSent.Md5Registros != newMd5 || !isToday)
                                {
                                    mustSend = true;
                                }
                            }

                            if (mustSend)
                            {
                                var logString = FormatLogString(results);

                                var ae = new AlertaEnviada
                                {
                                    IdAlerta = alert.Id,
                                    IdUsuario = au.Usuario.Id,
                                    Md5Registros = newMd5,
                                    FechaHoraCreacion = DateTime.UtcNow,
                                    Log = logString,
                                    Detalle = alert.Descripcion
                                };
                                db.AlertasEnviadas.Add(ae);

                                if (!string.IsNullOrEmpty(au.Usuario.Email))
                                {
                                    var mailLog = logString.Replace("^", "\t").Replace("¨", "\r");
                                    if (mailLog.Length > 4000) mailLog = mailLog.Substring(0, 4000);

                                    var mail = new Mailing
                                    {
                                        FechaCreado = DateTime.UtcNow,
                                        Destino = au.Usuario.Email,
                                        Asunto = $"Alerta: {alert.Descripcion}",
                                        Cuerpo = mailLog,
                                        Estado = 'P'
                                    };
                                    db.Mailings.Add(mail);

                                    await LogToFileAsync($"Alerta Enviada por Mail: {alert.Descripcion} al destino {au.Usuario.Email}");
                                }

                                await LogToFileAsync($"Alerta Enviada al Menú: {au.Usuario.NombreCompleto} - {alert.Id}: {alert.Descripcion}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al ejecutar alerta {Id}: {Descripcion}", alert.Id, alert.Descripcion);
                    await LogToFileAsync($"ERROR al ejecutar Alerta {alert.Id}: {ex.Message}");
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en el procesamiento general de alertas");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private bool IsSafeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var trimmed = sql.TrimStart().ToUpperInvariant();
        if (!trimmed.StartsWith("SELECT")) return false;

        var forbidden = new[] { "UPDATE ", "INSERT ", "DELETE ", "DROP ", "CREATE ", "ALTER ", "TRUNCATE " };
        foreach (var term in forbidden)
        {
            if (trimmed.Contains(term)) return false;
        }
        return true;
    }

    private async Task<List<Dictionary<string, object>>> ExecuteCustomQueryAsync(SuperPOSDbContext db, string sql, CancellationToken cancellationToken)
    {
        var list = new List<Dictionary<string, object>>();
        var connection = db.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            await connection.OpenAsync(cancellationToken);
        }
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = reader.GetValue(i);
                    row[name] = val == DBNull.Value ? null : val;
                }
                list.Add(row);
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
        return list;
    }

    private string CalculateMd5(string input)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private string FormatLogString(List<Dictionary<string, object>> results)
    {
        if (results.Count == 0) return new string('º', 4000);

        var columns = results[0].Keys.ToList();
        var sb = new StringBuilder();
        sb.Append(string.Join("^", columns));

        foreach (var row in results)
        {
            sb.Append("¨");
            var values = columns.Select(c => row[c]?.ToString() ?? "");
            sb.Append(string.Join("^", values));
        }

        var result = sb.ToString();
        if (result.Length > 4000)
        {
            result = result.Substring(0, 3997) + "...";
        }
        else if (result.Length < 4000)
        {
            result = result.PadRight(4000, 'º');
        }
        return result;
    }

    private async Task LogToFileAsync(string message)
    {
        try
        {
            var auditDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit");
            if (!Directory.Exists(auditDir))
            {
                Directory.CreateDirectory(auditDir);
            }

            var fileName = $"{DateTime.Today:ddmmyyyy}_logAlertas.txt";
            var filePath = Path.Combine(auditDir, fileName);
            var timestamp = DateTime.Now.ToString("dd/MM/yy HH:mm:ss");

            using var sw = new StreamWriter(filePath, append: true, Encoding.UTF8);
            await sw.WriteLineAsync($"[{timestamp}] {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al escribir en el archivo de log de alertas");
        }
    }
}
