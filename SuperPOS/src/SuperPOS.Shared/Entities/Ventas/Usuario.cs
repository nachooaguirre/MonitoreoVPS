namespace SuperPOS.Shared.Entities.Ventas;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;   // SHA256 hex
    public int IdPerfil { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool AccesoZebra { get; set; } = false;
    public Perfil? Perfil { get; set; }
}

public class Perfil
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsAdministrador { get; set; }

    // ——— Acceso a secciones ———
    public bool AccesoCaja { get; set; } = true;
    public bool AccesoArticulos { get; set; }
    public bool AccesoClientes { get; set; }
    public bool AccesoProveedores { get; set; }
    public bool AccesoCompras { get; set; }
    public bool AccesoStock { get; set; }
    public bool AccesoCtaCte { get; set; }
    public bool AccesoReportes { get; set; }
    public bool AccesoConfiguracion { get; set; }
    public bool AccesoUsuarios { get; set; }

    // ——— Permisos de operación ———
    public bool PuedeVender { get; set; } = true;
    public bool PuedeAnularVentas { get; set; }
    public bool PuedeHacerDescuentos { get; set; }
    public decimal MaximoDescuento { get; set; }
    public bool PuedeCambiarPrecios { get; set; }
    public bool PuedeVerCostos { get; set; }
    public bool PuedeModificarStock { get; set; }
    public bool PuedeAbrirCaja { get; set; } = true;
    public bool PuedeCerrarCaja { get; set; }
}
