namespace SuperPOS.Shared.Entities.Ventas;

public class Proveedor
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreFantasia { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public int CondicionIva { get; set; } = 1;
    public string? CodigoProveedor { get; set; }
    public string? Telefono { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
    public int DiasEntrega { get; set; }
    public int DiasVencimientoPago { get; set; } = 30;
    public decimal SaldoCtaCte { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}
