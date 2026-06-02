using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class HistorialPrecio
{
    public long Id { get; set; }
    public int IdArticulo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? IdUsuario { get; set; }
    public int? IdSucursal { get; set; }
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public string Campo { get; set; } = string.Empty; // 'C' = Costo, 'V' = Venta, 'A' = Alta

    // Navigation Properties
    public Articulo? Articulo { get; set; }
    public Usuario? Usuario { get; set; }
    public Sucursal? Sucursal { get; set; }
}
