using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController(SuperPOSDbContext db) : ControllerBase
{
    [HttpGet("run-script")]
    public async Task<IActionResult> RunScript()
    {
        var sql = @"
-- Actualizar marcas si no existen y asignarles productos
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM ""Proveedores"" WHERE ""RazonSocial"" = 'Distribuidora Coca-Cola') THEN
        INSERT INTO ""Proveedores"" (""RazonSocial"", ""Cuit"", ""CondicionIva"", ""Telefono"", ""Email"", ""DiasEntrega"", ""DiasVencimientoPago"", ""Activo"", ""FechaAlta"")
        VALUES ('Distribuidora Coca-Cola', '30-11111111-1', 1, '11-1111-1111', 'coca@dist.com', 2, 30, true, NOW());
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM ""Proveedores"" WHERE ""RazonSocial"" = 'Distribuidora Pepsi') THEN
        INSERT INTO ""Proveedores"" (""RazonSocial"", ""Cuit"", ""CondicionIva"", ""Telefono"", ""Email"", ""DiasEntrega"", ""DiasVencimientoPago"", ""Activo"", ""FechaAlta"")
        VALUES ('Distribuidora Pepsi', '30-22222222-2', 1, '11-2222-2222', 'pepsi@dist.com', 2, 30, true, NOW());
    END IF;
END $$;

-- Coca-Cola a Distribuidora Coca-Cola
UPDATE ""Articulos"" SET ""IdProveedor"" = (SELECT ""Id"" FROM ""Proveedores"" WHERE ""RazonSocial"" = 'Distribuidora Coca-Cola' LIMIT 1) WHERE ""Descripcion"" ILIKE '%Coca-Cola%' OR ""Descripcion"" ILIKE '%Sprite%' OR ""Descripcion"" ILIKE '%Fanta%';

-- Pepsi a Distribuidora Pepsi
UPDATE ""Articulos"" SET ""IdProveedor"" = (SELECT ""Id"" FROM ""Proveedores"" WHERE ""RazonSocial"" = 'Distribuidora Pepsi' LIMIT 1) WHERE ""Descripcion"" ILIKE '%Pepsi%' OR ""Descripcion"" ILIKE '%7UP%' OR ""Descripcion"" ILIKE '%Paso de los Toros%';
";
        await db.Database.ExecuteSqlRawAsync(sql);
        return Ok("Done");
    }
}
