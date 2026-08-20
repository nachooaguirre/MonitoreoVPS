using System.Net.Sockets;
using System.Text;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.Client.Services;

/// <summary>
/// Cliente TCP/IP del protocolo Kretz (balanzas Report Nx) para transmitirles PLU/precio
/// directo, sin pasar por el software iTegra/JDataGate del fabricante. Corre en la PC que está
/// en la misma red local que la balanza (la nube no puede alcanzar una IP de red interna).
/// ponytail: implementado siguiendo el manual de protocolo Kretz al pie de la letra, pero sin
/// verificar todavía contra una balanza real — antes de confiar en el resultado, probarlo con
/// hardware conectado (igual que se dejó pendiente con el Posnet físico).
/// </summary>
public class KretzBalanzaService(string? ip, int puerto = 1001, string idEquipo = "01")
{
    private const byte STX = 0x02;
    private const byte ETX = 0x04;
    private const byte STX_RESPUESTA = 0x07;

    public bool Configurada => !string.IsNullOrWhiteSpace(ip);

    /// <summary>Comando 0002: test de comunicación sin sonido.</summary>
    public Task<bool> TestConexionAsync(CancellationToken ct = default) => EnviarAsync("0002", "", ct);

    /// <summary>Comando 2005: alta/modificación de PLU. Usa el Id del artículo como número de PLU.</summary>
    public Task<bool> EnviarPrecioAsync(Articulo art, CancellationToken ct = default)
    {
        var datos =
            Campo(art.Id.ToString(), 6) +               // Número del PLU
            "001" +                                       // Código de departamento (simplificado: uno solo)
            "001" +                                       // Código de familia (simplificado: una sola)
            Campo(art.Descripcion, 26) +                  // Nombre del PLU
            Campo(art.DescripcionCorta, 26) +              // Descripción
            Campo(art.CodigoInterno, 5) +                  // Código del PLU
            (art.EsPesable ? "P" : "N") +                   // Tipo de PLU
            "0000000" +                                    // Valor fijo (no usado)
            CampoNumerico((long)Math.Round(art.PrecioVenta * 100), 7) +  // Precio (centavos)
            CampoNumerico((long)Math.Round(art.PrecioVenta * 100), 7) +  // Precio alternativo
            "0000000" +                                    // Posición decimal: toma la de la moneda configurada
            CampoNumerico((long)Math.Round(art.AlicuotaIva * 100), 6) +   // Impuesto 1
            "000000" +                                     // Impuesto 2
            "00000" + "00000" +                             // Tara preempaque / Tara público
            "00" + "0000" + "0000" +                        // Cód. etiqueta / receta / nutricional
            "1" + "000" + "0000";                           // Fecha envase / vencimiento / imagen

        return EnviarAsync("2005", datos, ct);
    }

    private static string Campo(string valor, int largo)
    {
        valor = (valor ?? "").ToUpperInvariant();
        return valor.Length >= largo ? valor[..largo] : valor.PadRight(largo);
    }

    private static string CampoNumerico(long valor, int largo)
    {
        var s = valor.ToString();
        return s.Length >= largo ? s[^largo..] : s.PadLeft(largo, '0');
    }

    private async Task<bool> EnviarAsync(string comando, string datos, CancellationToken ct)
    {
        if (!Configurada) return false;

        var payload = Encoding.ASCII.GetBytes("C" + idEquipo + comando + datos);
        var trama = new byte[1 + payload.Length + 2 + 1];
        trama[0] = STX;
        payload.CopyTo(trama, 1);
        var (checksumH, checksumL) = CalcularChecksum(trama.AsSpan(0, 1 + payload.Length));
        trama[1 + payload.Length] = checksumH;
        trama[1 + payload.Length + 1] = checksumL;
        trama[^1] = ETX;

        using var cliente = new TcpClient();
        cliente.SendTimeout = 3000;
        cliente.ReceiveTimeout = 3000;
        await cliente.ConnectAsync(ip!, puerto, ct);
        var stream = cliente.GetStream();
        await stream.WriteAsync(trama, ct);

        var buffer = new byte[512];
        var leidos = await stream.ReadAsync(buffer, ct);
        if (leidos < 8) return false;

        // Respuesta: 0x07 'C' ID(2) GRUPO(2) RESPUESTA(2) DATOS CHECKSUM(2) 0x04. "01" = OK.
        var texto = Encoding.ASCII.GetString(buffer, 0, leidos);
        var inicio = texto.IndexOf((char)STX_RESPUESTA);
        if (inicio < 0 || inicio + 7 > texto.Length) return false;
        var codigoRespuesta = texto.Substring(inicio + 6, 2);
        return codigoRespuesta == "01";
    }

    private static (byte h, byte l) CalcularChecksum(ReadOnlySpan<byte> bytesPrevios)
    {
        var suma = 0;
        foreach (var b in bytesPrevios) suma += b;
        var ultimoByte = suma & 0xFF;
        var h = (byte)(0x30 + ((ultimoByte >> 4) & 0xF));
        var l = (byte)(0x30 + (ultimoByte & 0xF));
        return (h, l);
    }
}
