-- ================================================================
-- SuperPOS - Seed CORREGIDO v3
-- ================================================================

-- Limpiar en orden correcto (sin CASCADE para evitar problemas)
DELETE FROM "ComprasDetalle";
DELETE FROM "Compras";
DELETE FROM "ComprobantesDetalle";
DELETE FROM "ComprobantesPago";
DELETE FROM "Comprobantes";
DELETE FROM "ArticulosCodigoBarras";
DELETE FROM "Articulos";
DELETE FROM "Familias";
DELETE FROM "Departamentos";
DELETE FROM "Marcas";
DELETE FROM "Proveedores";

-- Reiniciar secuencias explÃ­citamente
SELECT setval(pg_get_serial_sequence('"Departamentos"','"Id"'), 1, false);
SELECT setval(pg_get_serial_sequence('"Familias"','"Id"'), 1, false);
SELECT setval(pg_get_serial_sequence('"Marcas"','"Id"'), 1, false);
SELECT setval(pg_get_serial_sequence('"Proveedores"','"Id"'), 1, false);
SELECT setval(pg_get_serial_sequence('"Articulos"','"Id"'), 1, false);

-- ================================================================
-- DEPARTAMENTOS (IDs explÃ­citos, sin OVERRIDING)
-- ================================================================
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('AlmacÃ©n',    true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('Bebidas',    true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('LÃ¡cteos',    true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('CarnicerÃ­a', true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('VerdulerÃ­a', true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('Limpieza',   true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('PerfumerÃ­a', true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('PanaderÃ­a',  true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('Freezer',    true);
INSERT INTO "Departamentos" ("Nombre","Activo") VALUES ('Kiosco',     true);

-- Verificar IDs asignados
SELECT "Id", "Nombre" FROM "Departamentos" ORDER BY "Id";

-- ================================================================
-- FAMILIAS (subselect para no depender de "Id" hardcodeado)
-- ================================================================
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Aceites y Aderezos',   "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Arroces y Legumbres',  "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Fideos y Pastas',      "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Harinas y AzÃºcar',     "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Conservas',            "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Infusiones',           "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'LÃ¡cteos y Untables',   "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Jugos en Polvo',       "Id", true FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Agua y Gaseosas',      "Id", true FROM "Departamentos" WHERE "Nombre"='Bebidas';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Jugos',                "Id", true FROM "Departamentos" WHERE "Nombre"='Bebidas';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Cervezas',             "Id", true FROM "Departamentos" WHERE "Nombre"='Bebidas';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Vinos',                "Id", true FROM "Departamentos" WHERE "Nombre"='Bebidas';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Leche',                "Id", true FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Yogures',              "Id", true FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Quesos',               "Id", true FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Cremas',               "Id", true FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Carnes Frescas',       "Id", true FROM "Departamentos" WHERE "Nombre"='CarnicerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Fiambres',             "Id", true FROM "Departamentos" WHERE "Nombre"='CarnicerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Frutas',               "Id", true FROM "Departamentos" WHERE "Nombre"='VerdulerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Verduras',             "Id", true FROM "Departamentos" WHERE "Nombre"='VerdulerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Detergentes',          "Id", true FROM "Departamentos" WHERE "Nombre"='Limpieza';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Lavandinas',           "Id", true FROM "Departamentos" WHERE "Nombre"='Limpieza';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Jabones en Polvo',     "Id", true FROM "Departamentos" WHERE "Nombre"='Limpieza';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Desengrasantes',       "Id", true FROM "Departamentos" WHERE "Nombre"='Limpieza';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Shampoo',              "Id", true FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Dentales',             "Id", true FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Desodorantes',         "Id", true FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Cremas Corporales',    "Id", true FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Pan y Facturas',       "Id", true FROM "Departamentos" WHERE "Nombre"='PanaderÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Galletitas',           "Id", true FROM "Departamentos" WHERE "Nombre"='PanaderÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Tortas y Budines',     "Id", true FROM "Departamentos" WHERE "Nombre"='PanaderÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Snacks',               "Id", true FROM "Departamentos" WHERE "Nombre"='PanaderÃ­a';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Helados',              "Id", true FROM "Departamentos" WHERE "Nombre"='Freezer';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Precocidos',           "Id", true FROM "Departamentos" WHERE "Nombre"='Freezer';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Rebozados',            "Id", true FROM "Departamentos" WHERE "Nombre"='Freezer';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Chocolates',           "Id", true FROM "Departamentos" WHERE "Nombre"='Kiosco';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Caramelos',            "Id", true FROM "Departamentos" WHERE "Nombre"='Kiosco';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Chizitos y Papas',     "Id", true FROM "Departamentos" WHERE "Nombre"='Kiosco';
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'Golosinas Sueltas',    "Id", true FROM "Departamentos" WHERE "Nombre"='Kiosco';

-- ================================================================
-- MARCAS
-- ================================================================
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Arcor',          true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Marolio',        true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('La SerenÃ­sima',  true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Quilmes',        true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Molinos RÃ­o',    true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Bagley',         true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Unilever',       true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Procter&Gamble', true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Colgate',        true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Pepsico',        true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Coca-Cola',      true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('SanCor',         true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Natura',         true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Cepita',         true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Pato',           true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Skip',           true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Dove',           true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Rexona',         true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Milka',          true);
INSERT INTO "Marcas" ("Nombre","Activo") VALUES ('Sin Marca',      true);

-- ================================================================
-- PROVEEDORES (con Cuit obligatorio)
-- ================================================================
INSERT INTO "Proveedores" ("RazonSocial","NombreFantasia","Cuit","CondicionIva","CodigoProveedor","Telefono","Direccion","Localidad","Provincia","DiasEntrega","DiasVencimientoPago","SaldoCtaCte","Activo","FechaAlta") VALUES
  ('Distribuidora Norte S.A.', 'Dist. Norte',   '30-71000001-0', 1, 'DN001', '0341-423-0000', 'Av. ConstituciÃ³n 1500', 'Rosario', 'Santa Fe', 3, 30, 0.00, true, NOW());
INSERT INTO "Proveedores" ("RazonSocial","NombreFantasia","Cuit","CondicionIva","CodigoProveedor","Telefono","Direccion","Localidad","Provincia","DiasEntrega","DiasVencimientoPago","SaldoCtaCte","Activo","FechaAlta") VALUES
  ('Bebidas y MÃ¡s S.R.L.',     'Bebidas y MÃ¡s', '30-71000002-0', 1, 'BYM002','0341-456-7890', 'Pellegrini 2200',        'Rosario', 'Santa Fe', 2, 15, 0.00, true, NOW());

-- ================================================================
-- ARTÃCULOS con TODOS los campos NOT NULL
-- Helper function: get FK IDs by name
-- ================================================================

-- ALMACEN - Aceites y Aderezos
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010001001','A001','DN001','Aceite Girasol Natura 900ml','Aceite Natura',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Aceites y Aderezos'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Natura'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1450.0,2320.0,0.0,60.0, 0,0,0,0,0,0, 10.5,true,0.0, 12,1, false,0, 36.0,12.0,72.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010002002','A002','DN001','Arroz Largo Fino Gallo 1kg','Arroz Gallo 1kg',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Arroces y Legumbres'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Molinos RÃ­o'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  880.0,1390.0,0.0,58.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 48.0,12.0,96.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010003003','A003','DN001','Fideos Spaghetti Matarazzo 500g','Fideos 500g',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Fideos y Pastas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Molinos RÃ­o'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  560.0,890.0,0.0,59.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 72.0,24.0,144.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010004004','A004','DN001','Harina 000 Pureza 1kg','Harina 000 1kg',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Harinas y AzÃºcar'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Molinos RÃ­o'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  720.0,1140.0,0.0,58.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 48.0,12.0,96.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010005005','A005','DN001','AzÃºcar Ledesma 1kg','AzÃºcar 1kg',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Harinas y AzÃºcar'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Sin Marca'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  960.0,1520.0,0.0,58.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 60.0,24.0,120.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010006006','A006','DN001','Sal Fina La Flor 1kg','Sal Fina 1kg',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Harinas y AzÃºcar'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Sin Marca'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  380.0,610.0,0.0,61.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 60.0,24.0,120.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010007007','A007','DN001','Tomate Perita Arcor x400g','Tomate Perita',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Conservas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Arcor'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  680.0,1090.0,0.0,60.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 48.0,24.0,96.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010008008','A008','DN001','Yerba Mate TaragÃ¼i 500g','Yerba TaragÃ¼i',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Infusiones'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Molinos RÃ­o'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1780.0,2840.0,0.0,60.0, 0,0,0,0,0,0, 10.5,true,0.0, 12,1, false,0, 36.0,12.0,72.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010009009','A009','DN001','CafÃ© La Virginia x250g','CafÃ© Virginia',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='AlmacÃ©n'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Infusiones'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Molinos RÃ­o'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1650.0,2640.0,0.0,60.0, 0,0,0,0,0,0, 10.5,true,0.0, 12,1, false,0, 36.0,12.0,72.0,0.0, true,false,false,false, NOW(),0.0;

-- BEBIDAS
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010011011','B001','BYM002','Agua Villavicencio 1.5L s/Gas','Agua Villa 1.5L',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Bebidas'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Agua y Gaseosas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Sin Marca'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='BYM002'),
  680.0,1080.0,0.0,59.0, 0,0,0,0,0,0, 21.0,true,0.0, 12,1, false,0, 48.0,12.0,96.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010012012','B002','BYM002','Coca-Cola 2.25L','Coca-Cola 2.25L',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Bebidas'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Agua y Gaseosas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Coca-Cola'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='BYM002'),
  1450.0,2260.0,0.0,56.0, 0,0,0,0,0,0, 21.0,true,0.0, 6,1, false,0, 24.0,6.0,48.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010013013','B003','BYM002','7UP 2.25L','7UP 2.25L',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Bebidas'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Agua y Gaseosas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Pepsico'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='BYM002'),
  1280.0,2010.0,0.0,57.0, 0,0,0,0,0,0, 21.0,true,0.0, 6,1, false,0, 24.0,6.0,48.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010014014','B004','BYM002','Quilmes Cristal 1L x6','Quilmes 1L x6',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Bebidas'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Cervezas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Quilmes'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='BYM002'),
  4800.0,7500.0,0.0,56.0, 0,0,0,0,0,0, 21.0,true,0.0, 4,1, false,0, 12.0,4.0,24.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010015015','B005','BYM002','Cepita Naranja 1L','Cepita Naranja',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Bebidas'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Jugos'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Cepita'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='BYM002'),
  920.0,1450.0,0.0,58.0, 0,0,0,0,0,0, 10.5,true,0.0, 12,1, false,0, 48.0,12.0,96.0,0.0, true,false,false,false, NOW(),0.0;

-- LÃCTEOS
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010018018','L001','DN001','Leche La SerenÃ­sima Entera 1L','Leche Seren. 1L',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Leche'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='La SerenÃ­sima'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1180.0,1860.0,0.0,58.0, 0,0,0,0,0,0, 10.5,true,0.0, 12,1, false,0, 72.0,12.0,144.0,0.0, true,false,false,true, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010019019','L002','DN001','Yogur SanCor Natural x190g','Yogur SanCor',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Yogures'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='SanCor'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  650.0,1020.0,0.0,57.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 48.0,24.0,96.0,0.0, true,false,false,true, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010020020','L003','DN001','Queso Cremoso La SerenÃ­sima x400g','Queso Crem.',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Quesos'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='La SerenÃ­sima'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  2400.0,3750.0,0.0,56.0, 0,0,0,0,0,0, 10.5,true,0.0, 12,1, false,0, 24.0,12.0,48.0,0.0, true,false,false,true, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010021021','L004','DN001','Manteca Milkaut 200g','Manteca 200g',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='LÃ¡cteos'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Quesos'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='SanCor'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1850.0,2920.0,0.0,58.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 48.0,24.0,96.0,0.0, true,false,false,true, NOW(),0.0;

-- LIMPIEZA
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010023023','LI01','DN001','Lavandina Pato Concentrada 1L','Lavandina Pato',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Limpieza'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Lavandinas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Pato'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1100.0,1750.0,0.0,59.0, 0,0,0,0,0,0, 21.0,true,0.0, 12,1, false,0, 48.0,12.0,96.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010024024','LI02','DN001','Detergente Magistral 500ml','Detergen. 500ml',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Limpieza'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Detergentes'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Unilever'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  680.0,1080.0,0.0,59.0, 0,0,0,0,0,0, 21.0,true,0.0, 24,1, false,0, 72.0,24.0,144.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010025025','LI03','DN001','Skip Polvo Concentrado 1kg','Skip Polvo 1kg',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Limpieza'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Jabones en Polvo'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Skip'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  2800.0,4420.0,0.0,58.0, 0,0,0,0,0,0, 21.0,true,0.0, 6,1, false,0, 18.0,6.0,36.0,0.0, true,false,false,false, NOW(),0.0;

-- PERFUMERÃA
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010027027','P001','DN001','Shampoo Head & Shoulders 400ml','H&S 400ml',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Shampoo'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Procter&Gamble'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  3200.0,5050.0,0.0,58.0, 0,0,0,0,0,0, 21.0,true,0.0, 12,1, false,0, 36.0,12.0,72.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010028028','P002','DN001','Pasta Dental Colgate Triple 90g','Colgate 90g',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Dentales'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Colgate'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1650.0,2610.0,0.0,58.0, 0,0,0,0,0,0, 21.0,true,0.0, 24,1, false,0, 72.0,24.0,144.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010030030','P003','DN001','Desodorante Rexona Men 150ml','Rexona Men',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='PerfumerÃ­a'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Desodorantes'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Rexona'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  2100.0,3330.0,0.0,59.0, 0,0,0,0,0,0, 21.0,true,0.0, 12,1, false,0, 36.0,12.0,72.0,0.0, true,false,false,false, NOW(),0.0;

-- PANADERÃA / KIOSCO
INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010032032','G001','DN001','Galletitas Oreo Original 117g','Galletitas Oreo',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='PanaderÃ­a'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Galletitas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Arcor'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1050.0,1670.0,0.0,59.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 72.0,24.0,144.0,0.0, true,false,false,true, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010034034','K001','DN001','Chocolate Milka Leche 100g','Milka 100g',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Kiosco'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Chocolates'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Milka'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  980.0,1560.0,0.0,59.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 72.0,24.0,144.0,0.0, true,false,false,true, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010036036','K002','DN001','Caramelos Halls Menta x10','Halls Menta x10',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Kiosco'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Caramelos'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Arcor'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  350.0,570.0,0.0,63.0, 0,0,0,0,0,0, 10.5,true,0.0, 48,1, false,0, 120.0,48.0,240.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010037037','K003','DN001','Chizitos Arcor 55g','Chizitos 55g',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Kiosco'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Chizitos y Papas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Arcor'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  680.0,1090.0,0.0,60.0, 0,0,0,0,0,0, 10.5,true,0.0, 48,1, false,0, 120.0,48.0,240.0,0.0, true,false,false,false, NOW(),0.0;

INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","AplicaIva","ImpuestoInterno","UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","StockActual","StockMinimo","StockMaximo","StockDeposito","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","CantidadVendida")
SELECT '7791010038038','K004','DN001','Papas Fritas Pringles 124g','Pringles 124g',
  (SELECT "Id" FROM "Departamentos" WHERE "Nombre"='Kiosco'),
  (SELECT "Id" FROM "Familias" WHERE "Nombre"='Chizitos y Papas'),
  (SELECT "Id" FROM "Marcas" WHERE "Nombre"='Pepsico'),
  (SELECT "Id" FROM "Proveedores" WHERE "CodigoProveedor"='DN001'),
  1650.0,2620.0,0.0,59.0, 0,0,0,0,0,0, 10.5,true,0.0, 24,1, false,0, 72.0,24.0,144.0,0.0, true,false,false,false, NOW(),0.0;

-- ================================================================
-- CLIENTES ADICIONALES
-- ================================================================
INSERT INTO "Clientes" ("RazonSocial","NombreFantasia","Cuit","CondicionIva","Telefono","Celular","Email","Direccion","Localidad","Provincia","IdListaPrecio","TieneCtaCte","LimiteCredito","SaldoCtaCte","TipoSaldo","EsMoroso","DiasVencimientoCtaCte","PorcentajeDescuento","Activo","FechaAlta")
VALUES ('Garcia Juan Manuel',        null,'20-32145678-9',5,'4444-1111','11-5555-1111','garcia@gmail.com','Av. San MartÃ­n 1234','Buenos Aires','Buenos Aires',1,true, 50000.0,0.0,'H',false,30,0.0,true,NOW());
INSERT INTO "Clientes" ("RazonSocial","NombreFantasia","Cuit","CondicionIva","Telefono","Celular","Email","Direccion","Localidad","Provincia","IdListaPrecio","TieneCtaCte","LimiteCredito","SaldoCtaCte","TipoSaldo","EsMoroso","DiasVencimientoCtaCte","PorcentajeDescuento","Activo","FechaAlta")
VALUES ('Rodriguez Maria Elena',     null,'27-20987654-3',5,'4333-2222','11-6666-2222','',              'Belgrano 567',       'Buenos Aires','Buenos Aires',1,false,0.0,    0.0,'H',false,30,0.0,true,NOW());
INSERT INTO "Clientes" ("RazonSocial","NombreFantasia","Cuit","CondicionIva","Telefono","Celular","Email","Direccion","Localidad","Provincia","IdListaPrecio","TieneCtaCte","LimiteCredito","SaldoCtaCte","TipoSaldo","EsMoroso","DiasVencimientoCtaCte","PorcentajeDescuento","Activo","FechaAlta")
VALUES ('LÃ³pez Carlos Alberto',      null,'20-18765432-1',5,'4222-3333','11-7777-3333','',              'Corrientes 890',     'Buenos Aires','Buenos Aires',1,true,100000.0, 0.0,'H',false,60,5.0, true,NOW());

-- Verificar resultado final
SELECT 'Departamentos: ' || COUNT(*) FROM "Departamentos"
UNION ALL SELECT 'Familias: '    || COUNT(*) FROM "Familias"
UNION ALL SELECT 'Marcas: '      || COUNT(*) FROM "Marcas"
UNION ALL SELECT 'Proveedores: ' || COUNT(*) FROM "Proveedores"
UNION ALL SELECT 'ArtÃ­culos: '   || COUNT(*) FROM "Articulos"
UNION ALL SELECT 'Clientes: '    || COUNT(*) FROM "Clientes";


