using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class Mailing
{
    public int Id { get; set; }
    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
    public string Destino { get; set; } = "";
    public string Asunto { get; set; } = "";
    public string Cuerpo { get; set; } = "";
    public char Estado { get; set; } = 'P'; // 'P' = Pendiente, 'E' = Enviado, 'F' = Fallido
}
