-- Update Providers
UPDATE "Proveedores" SET "RazonSocial" = 'Distribuidora Coca-Cola' WHERE "Id" = 1;
UPDATE "Proveedores" SET "RazonSocial" = 'Distribuidora Pepsico' WHERE "Id" = 2;

INSERT INTO "Proveedores" ("RazonSocial", "Cuit", "CondicionIva", "Telefono", "Email", "DiasEntrega", "DiasVencimientoPago", "Activo", "FechaAlta")
VALUES 
('Distribuidora Arcor', '30-11111111-1', 1, '11-1111-1111', 'arcor@dist.com', 2, 30, true, NOW()),
('Distribuidora Láctea (La Serenísima/SanCor)', '30-22222222-2', 1, '11-2222-2222', 'lacteos@dist.com', 1, 15, true, NOW()),
('Mayorista Maxiconsumo', '30-33333333-3', 1, '11-3333-3333', 'maxi@dist.com', 7, 30, true, NOW());

-- Map products to correct providers
-- Coca-Cola to Prov 1
UPDATE "Articulos" SET "IdProveedor" = 1 WHERE "IdMarca" = 4;
-- PepsiCo to Prov 2
UPDATE "Articulos" SET "IdProveedor" = 2 WHERE "IdMarca" = 5;
-- Arcor to Prov 3
UPDATE "Articulos" SET "IdProveedor" = 3 WHERE "IdMarca" = 1;
-- Lácteos (La Serenísima = 2, SanCor = 3) to Prov 4
UPDATE "Articulos" SET "IdProveedor" = 4 WHERE "IdMarca" IN (2, 3);
-- Others to Maxiconsumo (Prov 5)
UPDATE "Articulos" SET "IdProveedor" = 5 WHERE "IdProveedor" NOT IN (1, 2, 3, 4);

-- Give some random mixed products to show the functionality
-- e.g., Maxiconsumo also sells Coca-Cola 1.5L and Pepsi 2.25L but we need a different mechanism for that if it's the SAME product.
-- Since Articulo has ONE IdProveedor, the easiest way to have multiple providers for the same product is using ListaPrecioProveedor.
-- Wait, the user said "randoms que compartan productos pero de distintas marcas como por ejemplo cocacola que tengan todas sus bebidas y peposi por ejemplo que tienen las suyas, son bebidas pero distintas".
-- This just means: In "Bebidas" (or "Gaseosas"), there are Coca-Cola brand products (supplied by Coca-Cola) and Pepsi brand products (supplied by Pepsi). They are different brands but same product category.
-- And when you search "Gaseosa", you see Coca-Cola (Prov: Coca-Cola) and Pepsi (Prov: Pepsico).
-- Then you select both, and the app creates 2 orders. This is EXACTLY what the current table structure supports!
