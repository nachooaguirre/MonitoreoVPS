using System;

namespace SuperPOS.Shared.Entities.Ventas;

/// <summary>
/// Talonario de cheques propios asociado a una cuenta corriente bancaria
/// </summary>
public class Chequera
{
    public int Id { get; set; }
    public int IdCuentaTesoreria { get; set; }
    public string Nombre { get; set; } = string.Empty;       // Ej: "Chequera Tradicional Nro 1"
    public string Desde { get; set; } = string.Empty;        // Rango inicial. Ej: "000100"
    public string Hasta { get; set; } = string.Empty;        // Rango final. Ej: "000150"
    public string SiguienteNumero { get; set; } = string.Empty; // Siguiente número a emitir. Ej: "000100"
    public TipoChequera Tipo { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    // Relación con CuentaTesoreria
    public CuentaTesoreria? Cuenta { get; set; }
}

public enum TipoChequera
{
    Tradicional = 0,
    PagoDiferido = 1
}
