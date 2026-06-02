using System;
using System.Collections.Generic;

namespace SuperPOS.Shared.Entities.Ventas;

public class Promocion
{
    public int Id { get; set; }
    public int CodigoPromocion { get; set; }
    public int TipoAccion { get; set; }
    public string Descripcion { get; set; } = "";
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public string HoraInicio { get; set; } = "";
    public string HoraFin { get; set; } = "";
    public string DiasSemana { get; set; } = "";
    public string Sucursales { get; set; } = "";
    public bool Activa { get; set; } = true;

    public ICollection<PromocionCondicion> Condiciones { get; set; } = [];
    public ICollection<PromocionParametroAccion> ParametrosAccion { get; set; } = [];
}
