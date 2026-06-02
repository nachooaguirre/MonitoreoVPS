const { Client } = require('pg');

const client = new Client({
  user: 'postgres',
  host: 'localhost',
  database: 'superpos',
  password: 'Chupamela10',
  port: 5432,
});

async function main() {
  await client.connect();
  console.log('Connected to DB');

  const sql = `
-- Update Providers
UPDATE "Proveedores" SET "RazonSocial" = 'Distribuidora Coca-Cola' WHERE "Id" = 1;
UPDATE "Proveedores" SET "RazonSocial" = 'Distribuidora Pepsico' WHERE "Id" = 2;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Arcor') THEN
        INSERT INTO "Proveedores" ("RazonSocial", "Cuit", "CondicionIva", "Telefono", "Email", "DiasEntrega", "DiasVencimientoPago", "SaldoCtaCte", "Activo", "FechaAlta")
        VALUES ('Distribuidora Arcor', '30-11111111-1', 1, '11-1111-1111', 'arcor@dist.com', 2, 30, 0, true, NOW());
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Láctea (La Serenísima/SanCor)') THEN
        INSERT INTO "Proveedores" ("RazonSocial", "Cuit", "CondicionIva", "Telefono", "Email", "DiasEntrega", "DiasVencimientoPago", "SaldoCtaCte", "Activo", "FechaAlta")
        VALUES ('Distribuidora Láctea (La Serenísima/SanCor)', '30-22222222-2', 1, '11-2222-2222', 'lacteos@dist.com', 1, 15, 0, true, NOW());
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "Proveedores" WHERE "RazonSocial" = 'Mayorista Maxiconsumo') THEN
        INSERT INTO "Proveedores" ("RazonSocial", "Cuit", "CondicionIva", "Telefono", "Email", "DiasEntrega", "DiasVencimientoPago", "SaldoCtaCte", "Activo", "FechaAlta")
        VALUES ('Mayorista Maxiconsumo', '30-33333333-3', 1, '11-3333-3333', 'maxi@dist.com', 7, 30, 0, true, NOW());
    END IF;
END $$;

-- Coca-Cola to Prov 1
UPDATE "Articulos" SET "IdProveedor" = 1 WHERE "IdMarca" = 4;
-- PepsiCo to Prov 2
UPDATE "Articulos" SET "IdProveedor" = 2 WHERE "IdMarca" = 5;
-- Arcor to Prov 3
UPDATE "Articulos" SET "IdProveedor" = (SELECT "Id" FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Arcor' LIMIT 1) WHERE "IdMarca" = 1;
-- Lácteos (La Serenísima = 2, SanCor = 3) to Prov 4
UPDATE "Articulos" SET "IdProveedor" = (SELECT "Id" FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Láctea (La Serenísima/SanCor)' LIMIT 1) WHERE "IdMarca" IN (2, 3);
-- Others to Maxiconsumo (Prov 5)
UPDATE "Articulos" SET "IdProveedor" = (SELECT "Id" FROM "Proveedores" WHERE "RazonSocial" = 'Mayorista Maxiconsumo' LIMIT 1) WHERE "IdProveedor" NOT IN (1, 2, (SELECT "Id" FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Arcor' LIMIT 1), (SELECT "Id" FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Láctea (La Serenísima/SanCor)' LIMIT 1));
  `;

  await client.query(sql);
  console.log('Updated Proveedores and Articulos');

  await client.end();
}

main().catch(e => console.error(e));
