namespace SuperPOS.Shared.Entities.Ventas;

public class Departamento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public ICollection<Familia> Familias { get; set; } = [];
}

public class Familia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdDepartamento { get; set; }
    public bool Activo { get; set; } = true;
    public Departamento? Departamento { get; set; }
}

public class Marca
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class ArticuloCodigoBarras
{
    public int Id { get; set; }
    public int IdArticulo { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public Articulo? Articulo { get; set; }
}

