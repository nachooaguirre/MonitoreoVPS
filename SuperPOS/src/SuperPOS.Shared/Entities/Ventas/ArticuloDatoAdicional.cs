using System;

namespace SuperPOS.Shared.Entities.Ventas;

public class ArticuloDatoAdicional
{
    public int Id { get; set; }
    
    public int IdArticulo { get; set; }
    public Articulo? Articulo { get; set; }

    public string Campo { get; set; } = "";
    public string Dato { get; set; } = "";
}
