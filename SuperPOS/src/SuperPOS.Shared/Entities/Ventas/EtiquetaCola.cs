using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class EtiquetaCola
{
    public int Id { get; set; }
    public int IdArticulo { get; set; }
    public int Cantidad { get; set; } = 1;
    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;

    public Articulo? Articulo { get; set; }
}
