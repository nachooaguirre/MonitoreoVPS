namespace SuperPOS.Shared.Entities.Ventas;

public class ConfiguracionEmpresa
{
    public int Id { get; set; } = 1;
    public string NombreEmpresa { get; set; } = "Mi Empresa";
    public string? NombreFantasia { get; set; }
    public string Cuit { get; set; } = "00-00000000-0";
    public string? IngresosBrutos { get; set; }
    public string? Direccion { get; set; }
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public int PuntoVenta { get; set; } = 1;
    public bool AfipHomologacion { get; set; } = true;
    public string? AfipCertificadoPath { get; set; }
    public string? AfipCertificadoPassword { get; set; }
    public string? ImpresoraFiscalModelo { get; set; }
    public string? ImpresoraFiscalPuerto { get; set; }
    public string? ImpresoraTicketNombre { get; set; }
    public string? MensajePiePagina { get; set; }
    public bool ControlaStock { get; set; } = true;
    public bool PrecioConIva { get; set; } = true;
    public string? BackupRuta { get; set; }
}
