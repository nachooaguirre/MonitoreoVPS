namespace SuperPOS.Client.Models;

public class UsuarioInfo
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public int IdPerfil { get; set; }
}
