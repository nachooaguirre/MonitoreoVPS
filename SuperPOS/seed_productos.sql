-- ================================================================
-- SuperPOS - Script de carga inicial de productos
-- Ejecutar en pgAdmin sobre la base de datos SuperPOS
-- ================================================================

-- Limpiar datos existentes (conserva usuarios y configuración)
TRUNCATE TABLE "ComprobantesDetalle" CASCADE;
TRUNCATE TABLE "ComprobantesPago" CASCADE;
TRUNCATE TABLE "Comprobantes" CASCADE;
TRUNCATE TABLE "ArticulosCodigoBarras" CASCADE;
TRUNCATE TABLE "Articulos" CASCADE;
TRUNCATE TABLE "Familias" CASCADE;
TRUNCATE TABLE "Departamentos" CASCADE;
TRUNCATE TABLE "Marcas" CASCADE;
TRUNCATE TABLE "Proveedores" CASCADE;

-- Reiniciar secuencias
ALTER SEQUENCE "Departamentos_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Familias_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Marcas_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Proveedores_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Articulos_Id_seq" RESTART WITH 1;

-- ================================================================
-- DEPARTAMENTOS
-- ================================================================
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES
  ('Almacén',     true),
  ('Bebidas',     true),
  ('Lácteos',     true),
  ('Carnicería',  true),
  ('Verdulería',  true),
  ('Limpieza',    true),
  ('Perfumería',  true),
  ('Panadería',   true),
  ('Freezer',     true),
  ('Kiosco',      true);

-- ================================================================
-- FAMILIAS
-- ================================================================
-- Almacén (IdDepartamento=1)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Aceites y Aderezos', 1, true),
  ('Arroces y Legumbres',1, true),
  ('Fideos y Pastas',    1, true),
  ('Harinas',            1, true),
  ('Conservas',          1, true),
  ('Infusiones',         1, true),
  ('Azúcar y Sal',       1, true),
  ('Salsas y Caldos',    1, true);
-- Bebidas (IdDepartamento=2)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Gaseosas',           2, true),
  ('Aguas',              2, true),
  ('Cervezas',           2, true),
  ('Jugos y Néctares',   2, true),
  ('Energizantes',       2, true);
-- Lácteos (IdDepartamento=3)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Leche',              3, true),
  ('Yogures',            3, true),
  ('Quesos',             3, true),
  ('Manteca y Crema',    3, true);
-- Carnicería (IdDepartamento=4)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Vacuno',             4, true),
  ('Cerdo',              4, true),
  ('Pollo',              4, true);
-- Verdulería (IdDepartamento=5)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Verduras',           5, true),
  ('Frutas',             5, true);
-- Limpieza (IdDepartamento=6)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Lavandinas y Cloro', 6, true),
  ('Detergentes',        6, true),
  ('Desinfectantes',     6, true),
  ('Papel',              6, true);
-- Perfumería (IdDepartamento=7)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Shampoo y Acondicionador', 7, true),
  ('Jabones',                  7, true),
  ('Desodorantes',             7, true),
  ('Cremas',                   7, true);
-- Panadería (IdDepartamento=8)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Panes',              8, true),
  ('Galletitas',         8, true),
  ('Snacks',             8, true);
-- Freezer (IdDepartamento=9)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Helados',            9, true),
  ('Rebozados',          9, true),
  ('Congelados',         9, true);
-- Kiosco (IdDepartamento=10)
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") VALUES
  ('Chocolates',         10, true),
  ('Caramelos',          10, true),
  ('Chicles',            10, true);

-- ================================================================
-- MARCAS
-- ================================================================
INSERT INTO "Marcas" ("Nombre","Activo") VALUES
  ('Arcor',             true),
  ('La Serenísima',     true),
  ('SanCor',            true),
  ('Coca-Cola',         true),
  ('PepsiCo',           true),
  ('Quilmes',           true),
  ('Molinos Río de la Plata', true),
  ('La Campagnola',     true),
  ('Knorr',             true),
  ('Hellmann''s',       true),
  ('Matarazzo',         true),
  ('Ledesma',           true),
  ('Taragüi',           true),
  ('Unilever',          true),
  ('Procter & Gamble',  true),
  ('Marolio',           true),
  ('Natura',            true),
  ('Don Satur',         true),
  ('Dove',              true),
  ('Pato',              true);

-- ================================================================
-- PROVEEDORES
-- ================================================================
INSERT INTO "Proveedores" ("RazonSocial","Cuit","CondicionIva","Telefono","Email","DiasEntrega","DiasVencimientoPago","Activo","FechaAlta") VALUES
  ('DISTRIBUIDORA DEL NORTE SRL', '30-12345678-9', 1, '011-4444-5555', 'ventas@distnorte.com.ar', 3, 30, true, NOW()),
  ('PROVEEDORA CENTRAL SA',       '30-98765432-1', 1, '011-3333-6666', 'compras@procentral.com.ar', 5, 45, true, NOW());

-- ================================================================
-- ARTÍCULOS
-- Formato: CodigoBarras, CodigoInterno, Descripcion, IdDept, IdFam, IdMarca, IdProv,
--          PrecioCosto, AlicuotaIva, MargenGanancia, PrecioVenta, UxB, StockActual, StockMinimo, Activo, FechaAlta
-- ================================================================

-- Helper: precio venta = costo * (1 + iva/100) * (1 + margen/100) — valores ya calculados

-- =============== ALMACÉN - ACEITES (Depto 1, Fam 1) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790895000114','A001','Aceite Girasol Natura 900cc',          1,1,17,1, 1890,10.5,35, 2820, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790895000121','A002','Aceite Girasol Natura 1.5L',           1,1,17,1, 2690,10.5,33, 3930, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798062540123','A003','Aceite Oliva Marolio Extra Virgen 500ml',1,1,16,1,3200,10.5,40, 4940, 6,1,  24, 6, 48,true,true,NOW(),false,0),
('7798059900011','A004','Aceite Maíz Natura 900cc',             1,1,17,1, 1990,10.5,32, 2910, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798170001234','A005','Mayonesa Hellmann''s 450g',             1,1,10,1,1800,10.5,38, 2740, 12,1, 60,12,120,true,true,NOW(),false,0),
('7798170001241','A006','Mayonesa Hellmann''s 500g Sachet',      1,1,10,1,2100,10.5,35, 3160, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798066310278','A007','Salsa Ketchup Hellmann''s 397g',        1,1,10,1,1650,10.5,40, 2540, 12,1, 36,12, 72,true,true,NOW(),false,0);

-- =============== ALMACÉN - ARROCES (Depto 1, Fam 2) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790580505047','A008','Arroz Blanco Largo Fino Gallo 500g',    1,2,7,1,  890,10.5,38, 1340, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790580505054','A009','Arroz Blanco Largo Fino Gallo 1kg',     1,2,7,1, 1650,10.5,36, 2480, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798062540167','A010','Lentejas Marolio 500g',                 1,2,16,1,  720,10.5,40, 1100, 24,1, 48,12, 96,true,true,NOW(),false,0),
('7798062540174','A011','Garbanzos Marolio 500g',                1,2,16,1,  890,10.5,38, 1340, 24,1, 36,12, 72,true,true,NOW(),false,0),
('7798062540181','A012','Porotos Negros Marolio 500g',           1,2,16,1,  750,10.5,40, 1140, 24,1, 36,12, 72,true,true,NOW(),false,0);

-- =============== ALMACÉN - FIDEOS (Depto 1, Fam 3) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790070000067','A013','Fideos Spaghetti Matarazzo 400g',       1,3,11,1,  780,10.5,38, 1180, 24,1, 96,24,192,true,true,NOW(),false,0),
('7790070000074','A014','Fideos Moños Matarazzo 400g',           1,3,11,1,  780,10.5,38, 1180, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790070000081','A015','Fideos Tirabuzón Matarazzo 400g',       1,3,11,1,  780,10.5,38, 1180, 24,1, 60,24,120,true,true,NOW(),false,0),
('7790070000098','A016','Fideos Coditos Matarazzo 500g',         1,3,11,1,  890,10.5,36, 1340, 20,1, 48,20, 96,true,true,NOW(),false,0),
('7790070000104','A017','Ñoquis Matarazzo Secos 500g',           1,3,11,1,  950,10.5,35, 1420, 12,1, 36,12, 72,true,true,NOW(),false,0);

-- =============== ALMACÉN - HARINAS (Depto 1, Fam 4) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7792170010014','A018','Harina 0000 Blancaflor 1kg',            1,4,7,1,  980,10.5,40, 1480, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7792170010021','A019','Harina 000 Blancaflor 1kg',             1,4,7,1,  950,10.5,40, 1430, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7792170010038','A020','Premezcla Blancaflor Bizcochuelo 500g', 1,4,7,1, 1650,10.5,38, 2480, 12,1, 24,12, 48,true,true,NOW(),false,0);

-- =============== ALMACÉN - CONSERVAS (Depto 1, Fam 5) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790045000047','A021','Tomate Triturado La Campagnola 400g',   1,5,8,2,  620,10.5,42,  980, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790045000054','A022','Tomate Perita La Campagnola 400g',      1,5,8,2,  680,10.5,40, 1060, 24,1, 60,24,120,true,true,NOW(),false,0),
('7790045000061','A023','Salsa de Tomate La Campagnola 520g',    1,5,8,2,  890,10.5,38, 1340, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790045000078','A024','Atún en Aceite La Campagnola x3',       1,5,8,2, 2100,10.5,42, 3290, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790045000085','A025','Choclo en Grano La Campagnola 340g',    1,5,8,2,  780,10.5,40, 1210, 24,1, 48,24, 96,true,true,NOW(),false,0);

-- =============== ALMACÉN - INFUSIONES (Depto 1, Fam 6) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790380010018','A026','Yerba Mate Taragüí 1kg',                1,6,13,2,2980,10.5,41, 4650, 12,1, 60,12,120,true,true,NOW(),false,0),
('7790380010025','A027','Yerba Mate Taragüí 500g',               1,6,13,2,1550,10.5,40, 2420, 20,1, 48,20, 96,true,true,NOW(),false,0),
('7790380010032','A028','Yerba Mate Taragüí con Palo 1kg',       1,6,13,2,2780,10.5,40, 4340, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790895000220','A029','Café Nescafé Classic 200g',             1,6,7,2, 4200,10.5,38, 6530, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790895000237','A030','Café Nescafé Classic 100g',             1,6,7,2, 2200,10.5,40, 3440, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7791010045018','A031','Mate Cocido Clipper x50 sobres',        1,6,7,2, 1480,10.5,42, 2320, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7791010045025','A032','Té Lipton Amarillo x20 sobres',         1,6,14,2, 980,10.5,40, 1530, 24,1, 60,24,120,true,true,NOW(),false,0);

-- =============== ALMACÉN - AZÚCAR Y SAL (Depto 1, Fam 7) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790435008014','A033','Azúcar Ledesma 1kg',                    1,7,12,1,1280,10.5,45, 2050, 12,1, 72,12,144,true,true,NOW(),false,0),
('7790435008021','A034','Azúcar Ledesma 2kg',                    1,7,12,1,2450,10.5,42, 3840, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798047600014','A035','Sal Fina Celusal 1kg',                  1,7,7,1,  580,10.5,55,  990, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7798047600021','A036','Sal Gruesa Celusal 1kg',                1,7,7,1,  560,10.5,55,  960, 24,1, 36,24, 72,true,true,NOW(),false,0);

-- =============== ALMACÉN - SALSAS (Depto 1, Fam 8) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790335000012','A037','Caldo de Res Knorr x8 cubos',           1,8,9,2,  920,10.5,40, 1430, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7790335000029','A038','Caldo de Verduras Knorr x8 cubos',      1,8,9,2,  890,10.5,40, 1380, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7790335000036','A039','Crema de Tomate Knorr 65g',             1,8,9,2,  780,10.5,42, 1230, 24,1, 36,24, 72,true,true,NOW(),false,0),
('7790335000043','A040','Sopas Knorr Pollo 65g',                 1,8,9,2,  680,10.5,42, 1070, 24,1, 36,24, 72,true,true,NOW(),false,0);

-- =============== BEBIDAS - GASEOSAS (Depto 2, Fam 9) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790895000312','B001','Coca-Cola 2.25L',                       2,9,4,2, 2100,21,35, 3430, 8,1, 48, 8, 96,true,true,NOW(),false,0),
('7790895000329','B002','Coca-Cola 1.5L',                        2,9,4,2, 1600,21,35, 2610, 8,1, 48, 8, 96,true,true,NOW(),false,0),
('7790895000336','B003','Coca-Cola 500ml',                       2,9,4,2,  780,21,40, 1310, 24,1, 96,24,192,true,true,NOW(),false,0),
('7790895000343','B004','Sprite 2.25L',                          2,9,4,2, 1980,21,35, 3230, 8,1, 40, 8, 80,true,true,NOW(),false,0),
('7790895000350','B005','Fanta Naranja 2.25L',                   2,9,4,2, 1980,21,35, 3230, 8,1, 32, 8, 64,true,true,NOW(),false,0),
('7790895000367','B006','Pepsi 2.25L',                           2,9,5,2, 1850,21,35, 3020, 8,1, 40, 8, 80,true,true,NOW(),false,0),
('7790895000374','B007','7UP 2.25L',                             2,9,5,2, 1850,21,35, 3020, 8,1, 32, 8, 64,true,true,NOW(),false,0),
('7798047600080','B008','Manaos Cola 2L',                        2,9,7,2,  680,21,42, 1160, 8,1, 48, 8, 96,true,true,NOW(),false,0),
('7798047600097','B009','Manaos Naranja 2L',                     2,9,7,2,  680,21,42, 1160, 8,1, 36, 8, 72,true,true,NOW(),false,0),
('7798047600103','B010','Cunnington Cola 2.25L',                 2,9,7,2,  720,21,40, 1210, 8,1, 40, 8, 80,true,true,NOW(),false,0);

-- =============== BEBIDAS - AGUAS (Depto 2, Fam 10) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7798062540210','B011','Agua Mineral Villa del Sur 1.5L',       2,10,7,2,  820,21,38, 1360, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798062540227','B012','Agua Mineral Villa del Sur 500ml',      2,10,7,2,  490,21,40,  820, 24,1, 72,24,144,true,true,NOW(),false,0),
('7798062540234','B013','Agua Mineral Eco de los Andes 1.5L',    2,10,4,2,  780,21,38, 1290, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798062540241','B014','Agua Saborizada Cepita Pomelo 1.5L',    2,10,4,2,  980,21,40, 1640, 12,1, 36,12, 72,true,true,NOW(),false,0);

-- =============== BEBIDAS - CERVEZAS (Depto 2, Fam 11) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790040000013','B015','Cerveza Quilmes 970ml Retornable',      2,11,6,2, 1380,21,50, 2490, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790040000020','B016','Cerveza Quilmes Lata 354ml',            2,11,6,2,  780,21,42, 1330, 24,1, 96,24,192,true,true,NOW(),false,0),
('7790040000037','B017','Cerveza Stella Artois 950ml',           2,11,6,2, 1650,21,45, 2880, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790040000044','B018','Cerveza Palermo 1L',                    2,11,6,2,  990,21,45, 1730, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790040000051','B019','Cerveza Imperial Lata 473ml',           2,11,6,2,  850,21,42, 1450, 24,1, 72,24,144,true,true,NOW(),false,0);

-- =============== BEBIDAS - JUGOS (Depto 2, Fam 12) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7798047600130','B020','Cepita Naranja 1L',                     2,12,4,2,  920,10.5,38, 1410, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798047600147','B021','Cepita Durazno 1L',                     2,12,4,2,  920,10.5,38, 1410, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798047600154','B022','Jugo Tang Naranja x10 sobres',          2,12,7,2,  650,10.5,40, 1000, 24,1, 60,24,120,true,true,NOW(),false,0),
('7798047600161','B023','Jugo Tang Durazno x10 sobres',          2,12,7,2,  650,10.5,40, 1000, 24,1, 48,24, 96,true,true,NOW(),false,0);

-- =============== LÁCTEOS - LECHE (Depto 3, Fam 14) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790080000016','L001','Leche La Serenísima Entera 1L',         3,14,2,2, 1180,10.5,40, 1820, 12,1, 72,12,144,true,true,NOW(),false,0),
('7790080000023','L002','Leche La Serenísima Descremada 1L',     3,14,2,2, 1220,10.5,38, 1870, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790080000030','L003','Leche La Serenísima Entera 3L',         3,14,2,2, 3300,10.5,38, 5060, 6,1, 36, 6, 72,true,true,NOW(),false,0),
('7798034000012','L004','Leche SanCor Entera 1L',               3,14,3,2, 1150,10.5,40, 1770, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798034000029','L005','Leche en Polvo La Serenísima 800g',     3,14,2,2, 3800,10.5,38, 5820, 6,1, 24, 6, 48,true,true,NOW(),false,0);

-- =============== LÁCTEOS - YOGURES (Depto 3, Fam 15) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7798034000036','L006','Yogur SanCor Bebible Frutilla 200g',   3,15,3,2,  680,10.5,42, 1060, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7798034000043','L007','Yogur SanCor Natural Firme 190g',       3,15,3,2,  650,10.5,40, 1010, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7790080000047','L008','Danette Chocolate La Serenísima 200g', 3,15,2,2, 850,10.5,42, 1330, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790080000054','L009','Yogur Griego La Serenísima 190g',       3,15,2,2,1050,10.5,40, 1630, 12,1, 36,12, 72,true,true,NOW(),false,0);

-- =============== LÁCTEOS - QUESOS (Depto 3, Fam 16) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790080000061','L010','Queso Crema La Serenísima 190g',        3,16,2,2,1350,10.5,38, 2070, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798034000050','L011','Queso Cremoso SanCor 350g',             3,16,3,2,2800,10.5,38, 4290, 6,1, 24, 6, 48,true,true,NOW(),false,0),
('7790080000078','L012','Manteca La Serenísima 200g',            3,17,2,2,1650,10.5,36, 2520, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798034000067','L013','Crema de Leche SanCor 200cc',           3,17,3,2, 980,10.5,40, 1510, 12,1, 48,12, 96,true,true,NOW(),false,0);

-- =============== LIMPIEZA (Depto 6, Fam 23-26) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7798047600190','LI01','Lavandina Pato Concentrada 1L',         6,23,20,1,1100,21,40, 1850, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798047600207','LI02','Lavandina Pato 2L',                     6,23,20,1,1850,21,38, 3070, 8,1, 32, 8, 64,true,true,NOW(),false,0),
('7798047600214','LI03','Detergente Skip Limón 500ml',           6,24,14,1,1450,21,40, 2440, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798047600221','LI04','Detergente Magistral 1L',               6,24,7,1,1680,21,38, 2790, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798047600238','LI05','Ajax Limpiador Multiusos 500ml',        6,25,14,1,1380,21,42, 2320, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7798047600245','LI06','Esponjita Scotch Brite x2',             6,25,15,1, 890,21,45, 1540, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7798047600252','LI07','Papel Higiénico Elite x4 Triple Hoja', 6,26,14,1,2200,10.5,40, 3380, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7798047600269','LI08','Papel Higiénico Elite x8',              6,26,14,1,3900,10.5,38, 5980, 6,1, 24, 6, 48,true,true,NOW(),false,0),
('7798047600276','LI09','Servilletas Pañuelos 100u',             6,26,7,1,  680,10.5,42, 1070, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7798047600283','LI10','Bolsas Residuos Grandes x10',           6,25,7,1,  580,21,50,  1040, 24,1, 60,24,120,true,true,NOW(),false,0);

-- =============== PERFUMERÍA (Depto 7, Fam 27-30) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7791010045100','P001','Shampoo Head & Shoulders Control Caspa 400ml',7,27,15,1,3200,21,40, 5380, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7791010045117','P002','Shampoo Dove Hidratación 400ml',         7,27,19,1,2800,21,40, 4710, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7791010045124','P003','Acondicionador Dove 400ml',              7,27,19,1,2900,21,38, 4820, 12,1, 24,12, 48,true,true,NOW(),false,0),
('7791010045131','P004','Jabón Dove Original 90g x3',             7,28,19,1,1800,21,42, 3040, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7791010045148','P005','Jabón Activex x3',                       7,28,14,1,1650,21,40, 2770, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7791010045155','P006','Desodorante Dove Men Roll-On 50ml',      7,29,19,1,2200,21,40, 3700, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7791010045162','P007','Desodorante Nivea Women Aerosol 150ml', 7,29,7,1, 2100,21,42, 3550, 24,1, 36,24, 72,true,true,NOW(),false,0),
('7791010045169','P008','Crema Ponds Hidratante 200g',            7,30,14,1,2500,21,38, 4160, 12,1, 24,12, 48,true,true,NOW(),false,0),
('7791010045176','P009','Vaselina Pura 100g',                    7,30,7,1,  680,21,50, 1230, 24,1, 48,24, 96,true,true,NOW(),false,0),
('7791010045183','P010','Crema de Manos Neutrogena 75ml',         7,30,15,1,1950,21,40, 3280, 24,1, 24,24, 48,true,true,NOW(),false,0);

-- =============== PANADERÍA / GALLETITAS (Depto 8, Fam 32) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790040000068','G001','Galletitas Oreo Original 117g',          8,32,1,1, 1050,10.5,45, 1680, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790040000075','G002','Galletitas Pepitos Chocolate 109g',      8,32,1,1,  980,10.5,42, 1540, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790040000082','G003','Galletitas Rogel Vainilla 210g',         8,32,1,1, 1280,10.5,40, 1990, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790040000089','G004','Galletitas Bagley Surtido 300g',         8,32,1,1, 1580,10.5,40, 2450, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790040000096','G005','Facturas de Manteca x6',                 8,31,18,1,1800,10.5,35, 2690, 1,1,  20, 1, 50,true,true,NOW(),false,0),
('7790040000102','G006','Tostadas de Pan Bimbo 300g',             8,31,18,1,1350,10.5,38, 2070, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790040000119','G007','Medialunas Manteca x6',                  8,31,18,1,1650,10.5,35, 2470, 1,1,  20, 1, 40,true,true,NOW(),false,0);

-- =============== FREEZER (Depto 9) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790040000126','F001','Helado Agua Palito Arcor x6',            9,34,1,1, 1200,10.5,40, 1860, 12,1, 24,12, 48,true,true,NOW(),false,0),
('7790040000133','F002','Helado Fruttare Frutilla x4',            9,34,4,1, 2100,10.5,40, 3250, 6,1, 18, 6, 36,true,true,NOW(),false,0),
('7790040000140','F003','Milanesa de Pollo Don Satur x4 420g',    9,35,18,1,2800,10.5,38, 4280, 12,1, 24, 6, 48,true,true,NOW(),false,0),
('7790040000157','F004','Bastones de Merluza 500g',               9,35,18,1,3200,10.5,35, 4800, 8,1, 16, 4, 32,true,true,NOW(),false,0),
('7790040000164','F005','Papas pre-fritas La Española 700g',      9,36,7,1, 2100,10.5,40, 3250, 8,1, 24, 8, 48,true,true,NOW(),false,0),
('7790040000171','F006','Empanadas de Carne Frutos del País x12', 9,36,18,1,3800,10.5,38, 5820, 6,1, 12, 4, 24,true,true,NOW(),false,0);

-- =============== KIOSCO (Depto 10, Fam 37-39) ===============
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","Descripcion","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","AlicuotaIva","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","AplicaIva","Activo","FechaAlta","EsPesable","BanderaEAN") VALUES
('7790040000178','K001','Chocolate Milka Leche 100g',             10,37,1,1,  980,10.5,50, 1640, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790040000185','K002','Chocolate Cofler Orígenes 100g',         10,37,1,1,  920,10.5,48, 1520, 24,1, 60,24,120,true,true,NOW(),false,0),
('7790040000192','K003','Alfajor Havanna Chocolate x2',           10,37,1,2, 2100,10.5,45, 3390, 12,1, 36,12, 72,true,true,NOW(),false,0),
('7790040000208','K004','Alfajor Jorgito x3',                     10,37,1,2, 1200,10.5,50, 2000, 12,1, 48,12, 96,true,true,NOW(),false,0),
('7790040000215','K005','Caramelos Arcor Masticables x14',        10,38,1,2,  280,10.5,60,  500, 48,1, 96,48,192,true,true,NOW(),false,0),
('7790040000222','K006','Caramelos Mr. Pop x6',                   10,38,1,2,  320,10.5,56,  560, 48,1, 96,48,192,true,true,NOW(),false,0),
('7790040000229','K007','Chicles Beldent Menta x30',              10,39,14,2, 680,10.5,47, 1110, 24,1, 72,24,144,true,true,NOW(),false,0),
('7790040000236','K008','Chicles Trident Sandía x10',             10,39,14,2, 480,10.5,50,  800, 48,1, 96,48,192,true,true,NOW(),false,0),
('7790040000243','K009','Alfajor Terrabusi x12',                  10,37,7,2, 2800,10.5,40, 4350, 6,1, 24, 6, 48,true,true,NOW(),false,0),
('7790040000250','K010','Chupetín Pop Rock Arcor',                10,38,1,2,  120,10.5,67,  220,100,1,200,50,400,true,true,NOW(),false,0);

-- ================================================================
-- CLIENTES ADICIONALES (ficticios)
-- ================================================================
INSERT INTO "Clientes" ("RazonSocial","Cuit","CondicionIva","Telefono","Celular","Email","Direccion","Localidad","Provincia","TieneCtaCte","LimiteCredito","SaldoCtaCte","TipoSaldo","DiasVencimientoCtaCte","PorcentajeDescuento","Activo","FechaAlta","IdListaPrecio") VALUES
('GARCIA JUAN MANUEL',    '20-32145678-9',5,'4444-1111','11-5555-1111','garcia@gmail.com','Av. San Martín 1234','Buenos Aires','Buenos Aires',true,  50000, 0,'H',30,0,true,NOW(),1),
('RODRIGUEZ MARIA LAURA', '27-45678912-3',5,'4444-2222','11-5555-2222','rodriguez@gmail.com','Corrientes 567','Capital Federal','Buenos Aires',true, 30000, 0,'H',30,5,true,NOW(),1),
('DISTRIBUIDORA EL TREBOL SRL','30-55556666-7',1,'4444-3333','11-5555-3333','trebol@empresa.com','Av. Rivadavia 890','Lomas de Zamora','Buenos Aires',true,200000, 0,'H',30,10,true,NOW(),1),
('SUPER CHANG HONG',      '20-77889900-1',6,'4444-4444','11-5555-4444',null,'Mitre 234','Quilmes','Buenos Aires',true,100000, 0,'H',30,8,true,NOW(),1),
('GONZALEZ CARLOS',       '20-98765432-1',5,'4444-5555','11-5555-5555',null,'Belgrano 456','Avellaneda','Buenos Aires',false,     0, 0,'H', 0,0,true,NOW(),1),
('MINIMARKET LA ESQUINA', '30-12398765-4',6,'4444-6666','11-5555-6666','esquina@mail.com','San Juan 100','Lanús','Buenos Aires',true, 80000, 0,'H',30,5,true,NOW(),1),
('FERNANDEZ ANA BEATRIZ', '27-11223344-5',5,'4444-7777','11-5555-7777',null,'Sarmiento 789','Lomas de Zamora','Buenos Aires',false,    0, 0,'H', 0,0,true,NOW(),1),
('RESTO BAR EL GAUCHO SRL','30-44556677-8',1,'4444-8888','11-5555-8888','gaucho@resto.com','Av. Constitución 1010','CABA','Buenos Aires',true,150000, 0,'H',30,12,true,NOW(),1);

-- ================================================================
-- VERIFICACIÓN
-- ================================================================
SELECT 'Departamentos: ' || COUNT(*) FROM "Departamentos"
UNION ALL SELECT 'Familias: '     || COUNT(*) FROM "Familias"
UNION ALL SELECT 'Marcas: '       || COUNT(*) FROM "Marcas"
UNION ALL SELECT 'Proveedores: '  || COUNT(*) FROM "Proveedores"
UNION ALL SELECT 'Artículos: '    || COUNT(*) FROM "Articulos"
UNION ALL SELECT 'Clientes: '     || COUNT(*) FROM "Clientes";
