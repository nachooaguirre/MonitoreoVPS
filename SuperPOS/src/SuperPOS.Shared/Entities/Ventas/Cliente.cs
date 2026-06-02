namespace SuperPOS.Shared.Entities.Ventas;

public class Cliente
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreFantasia { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public int CondicionIva { get; set; } = 5;       // 5=Cons.Final, 1=RI, 4=Exento
    public string? Telefono { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Localidad { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Provincia { get; set; }
    public int IdListaPrecio { get; set; } = 1;
    public int? IdVendedor { get; set; }
    public bool TieneCtaCte { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal SaldoCtaCte { get; set; }
    public char TipoSaldo { get; set; } = 'H';       // H=Haber D=Deudor
    public bool EsMoroso { get; set; }
    public int DiasVencimientoCtaCte { get; set; } = 30;
    public decimal PorcentajeDescuento { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaVtoCtaCte { get; set; }
    public string? Observaciones { get; set; }
}
