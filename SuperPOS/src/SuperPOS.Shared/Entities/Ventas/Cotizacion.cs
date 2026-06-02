using System;
using System.Collections.Generic;

namespace SuperPOS.Shared.Entities.Ventas;

public class Cotizacion
{
    public long Id { get; set; }
    public long Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int IdProveedor { get; set; }
    public string Descripcion { get; set; } = "";
    public string? Observacion { get; set; }
    public string? PlazoEntrega { get; set; }

    public Proveedor? Proveedor { get; set; }
    public ICollection<CotizacionDetalle> Detalles { get; set; } = [];
}
