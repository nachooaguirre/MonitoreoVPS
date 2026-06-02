namespace SuperPOS.Shared.Entities.Ventas;

public class Caja
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdSucursal { get; set; }
    public bool Activo { get; set; } = true;
    public Sucursal? Sucursal { get; set; }
}

public class Sucursal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool EsCentral { get; set; }
    public bool Activo { get; set; } = true;
}

public class TurnoCaja
{
    public long Id { get; set; }
    public int IdCaja { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Apertura { get; set; } = DateTime.UtcNow;
    public DateTime? Cierre { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal SaldoFinal { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Abierto;
    public Caja? Caja { get; set; }
}

public enum EstadoTurno
{
    Abierto = 1,
    Cerrado = 2
}
