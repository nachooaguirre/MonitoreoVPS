using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class AlertaEnviada
{
    public int Id { get; set; }
    public int IdAlerta { get; set; }
    public int IdUsuario { get; set; }
    public string Md5Registros { get; set; } = "";
    public DateTime FechaHoraCreacion { get; set; } = DateTime.UtcNow;
    public string Log { get; set; } = ""; // Datos serializados
    public string? Detalle { get; set; }

    public Alerta? Alerta { get; set; }
    public Usuario? Usuario { get; set; }
}
