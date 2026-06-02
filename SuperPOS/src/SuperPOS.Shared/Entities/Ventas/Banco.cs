using System;

namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Catálogo formal de entidades bancarias
/// </summary>
public class Banco
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;  // Ej: "011", "014"
    public string Nombre { get; set; } = string.Empty;  // Ej: "BANCO NACION", "BANCO PROVINCIA"
    public bool Activo { get; set; } = true;
}
