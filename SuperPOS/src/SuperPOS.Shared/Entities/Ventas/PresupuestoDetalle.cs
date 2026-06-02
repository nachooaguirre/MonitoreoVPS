using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class PresupuestoDetalle
{
    public long Id { get; set; }
    public long IdPresupuesto { get; set; }
    public int IdArticulo { get; set; }
    public int ItemNro { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Costo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Margen { get; set; }

    public decimal SubtotalCalculado => Cantidad * Precio;

    public Presupuesto? Presupuesto { get; set; }
    public Articulo? Articulo { get; set; }
}
