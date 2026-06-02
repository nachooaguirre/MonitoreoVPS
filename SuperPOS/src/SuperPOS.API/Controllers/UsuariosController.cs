using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;
using SuperPOS.Shared.Entities.Ventas;
using System.Security.Cryptography;
using System.Text;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController(SuperPOSDbContext db) : ControllerBase
{
    private static string HashSha256(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes).ToLower();
    }

    // POST /api/usuarios/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var hash = HashSha256(req.Password);
        var user = await db.Usuarios
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.NombreUsuario == req.NombreUsuario && u.PasswordHash == hash && u.Activo);

        if (user is null)
            return Unauthorized(new { error = "Usuario o contraseña incorrectos" });

        user.UltimoAcceso = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? soloActivos)
    {
        var q = db.Usuarios.Include(u => u.Perfil).AsQueryable();
        if (soloActivos == true) q = q.Where(u => u.Activo);
        return Ok(await q.OrderBy(u => u.NombreUsuario).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var u = await db.Usuarios.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == id);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioRequest req)
    {
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == req.NombreUsuario))
            return BadRequest(new { error = "El nombre de usuario ya existe" });

        var user = new Usuario
        {
            NombreUsuario = req.NombreUsuario,
            NombreCompleto = req.NombreCompleto,
            PasswordHash = HashSha256(req.Password),
            IdPerfil = req.IdPerfil,
            Email = req.Email,
            Telefono = req.Telefono,
            Activo = true,
            FechaAlta = DateTime.UtcNow
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioRequest req)
    {
        var user = await db.Usuarios.FindAsync(id);
        if (user is null) return NotFound();

        user.NombreCompleto = req.NombreCompleto;
        user.IdPerfil = req.IdPerfil;
        user.Email = req.Email;
        user.Telefono = req.Telefono;
        user.Activo = req.Activo;

        if (!string.IsNullOrWhiteSpace(req.NuevaPassword))
            user.PasswordHash = HashSha256(req.NuevaPassword);

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == 1) return BadRequest(new { error = "No se puede eliminar el administrador principal" });
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        u.Activo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Perfiles
    [HttpGet("/api/perfiles")]
    public async Task<IActionResult> GetPerfiles() =>
        Ok(await db.Perfiles.OrderBy(p => p.Nombre).ToListAsync());

    [HttpGet("/api/perfiles/{id}")]
    public async Task<IActionResult> GetPerfil(int id)
    {
        var p = await db.Perfiles.FindAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost("/api/perfiles")]
    public async Task<IActionResult> CreatePerfil([FromBody] Perfil perfil)
    {
        db.Perfiles.Add(perfil);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPerfil), new { id = perfil.Id }, perfil);
    }

    [HttpPut("/api/perfiles/{id}")]
    public async Task<IActionResult> UpdatePerfil(int id, [FromBody] Perfil perfil)
    {
        if (id != perfil.Id) return BadRequest();
        db.Entry(perfil).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record LoginRequest(string NombreUsuario, string Password);
public record CreateUsuarioRequest(string NombreUsuario, string NombreCompleto, string Password, int IdPerfil, string? Email, string? Telefono);
public record UpdateUsuarioRequest(string NombreCompleto, int IdPerfil, bool Activo, string? NuevaPassword, string? Email, string? Telefono);
