using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;

namespace SuperPOS.API.Helpers;

/// <summary>
/// Determina a qué sucursales tiene acceso el usuario logueado: null = sin restricción (admin),
/// lista = solo esas (asignadas vía UsuarioSucursal). Reusar en cualquier endpoint que deba
/// filtrar/restringir datos por sucursal según quién está pidiendo.
/// </summary>
public static class SucursalScopeHelper
{
    public static async Task<List<int>?> ObtenerPermitidasAsync(ClaimsPrincipal user, SuperPOSDbContext db)
    {
        if (user.FindFirstValue("esAdministrador") == "True") return null;

        var idUsuario = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        return await db.UsuariosSucursales.Where(x => x.IdUsuario == idUsuario)
            .Select(x => x.IdSucursal).ToListAsync();
    }
}
