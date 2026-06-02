using System;
using System.Collections.Generic;

namespace SuperPOS.Shared.Entities.Ventas;

public class Presupuesto
{
    public long Id { get; set; }
    public long Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public int IdSucursal { get; set; }
    public int PlazoValidezDias { get; set; } = 30;
    public string? Contacto { get; set; }
    public string Detalle { get; set; } = "";
    public string? Observacion { get; set; }
    public string? FormaPago { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.Pendiente;

    public Cliente? Cliente { get; set; }
    public Usuario? Usuario { get; set; }
    public Sucursal? Sucursal { get; set; }
    public ICollection<PresupuestoDetalle> Detalles { get; set; } = [];
}

public enum EstadoPresupuesto
{
    Pendiente = 0,
    Aprobado = 1,
    Rechazado = 2,
    Facturado = 3
}
