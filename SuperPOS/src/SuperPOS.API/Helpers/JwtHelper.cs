using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SuperPOS.Shared.Entities.Ventas;

namespace SuperPOS.API.Helpers;

public static class JwtHelper
{
    public static SymmetricSecurityKey GetSigningKey(IConfiguration config) =>
        new(Encoding.UTF8.GetBytes(config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Secret en appsettings.json")));

    public static string GenerarToken(IConfiguration config, Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new("idPerfil", usuario.IdPerfil.ToString()),
            new("esAdministrador", (usuario.Perfil?.EsAdministrador ?? false).ToString())
        };

        var expMinutos = int.TryParse(config["Jwt:ExpirationMinutes"], out var m) ? m : 720;
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutos),
            signingCredentials: new SigningCredentials(GetSigningKey(config), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
