using System;
using System.Collections.Generic;

namespace SuperPOS.Shared.Entities.Ventas;

public class Alerta
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    public string? Detalle { get; set; }
    public string ConsultaSQL { get; set; } = "";
    public string DiasSemanaAlerta { get; set; } = ""; // Ej: "DO,LU,MA,MI,JU,VI,SA"
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<AlertaUsuario> AlertasUsuarios { get; set; } = [];
}
