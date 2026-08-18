using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperPOS.API.Data;

namespace SuperPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController(SuperPOSDbContext db) : ControllerBase
{
    /// <summary>
    /// Pobla la base con datos de demo: proveedores, categorias, ~113 productos,
    /// stock, ordenes de compra, remitos, presupuestos y 450 ventas. Pensado para
    /// correr UNA vez sobre una base vacia (tiene guard contra doble ejecucion).
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("seed-demo")]
    public async Task<IActionResult> SeedDemo()
    {
        await db.Database.ExecuteSqlRawAsync(SeedDemoSql);
        return Ok("Seed de demo aplicado correctamente.");
    }

    private const string SeedDemoSql = """
        DO $$
        DECLARE
            v_dep_almacen      int;
            v_dep_bebidas      int;
            v_dep_lacteos      int;
            v_dep_limpieza     int;
            v_dep_perfumeria   int;
            v_dep_congelados   int;
            v_dep_panaderia    int;

            v_fam_almacen1 int; v_fam_almacen2 int;
            v_fam_bebidas1 int; v_fam_bebidas2 int;
            v_fam_lacteos1 int; v_fam_lacteos2 int;
            v_fam_limpieza1 int; v_fam_limpieza2 int;
            v_fam_perfumeria1 int; v_fam_perfumeria2 int;
            v_fam_congelados1 int;
            v_fam_panaderia1 int;

            v_marca_ids int[];
            v_prov_ids int[];
            v_cliente_ids int[];

            v_articulo_ids int[];
            v_articulo_count int;

            rec record;
            i int;
            j int;
            n_items int;
            art_id int;
            prov_id int;
            marca_id int;
            costo numeric;
            margen numeric;
            venta numeric;
            stock numeric;

            v_oc_num int := 1;
            oc_id int;
            cant numeric;
            subtotal numeric;
            total_sin_iva numeric;
            total_iva numeric;

            v_rem_num int := 1;
            rem_id int;

            v_num bigint;
            comp_id bigint;
            v_fecha timestamptz;
            v_cliente int;
            v_total numeric;
            v_subtotal numeric;
            precio numeric;
            precio_sin_iva numeric;
            monto_iva numeric;
            v_medio int;

            v_pre_num bigint;
            pre_id bigint;
        BEGIN
            IF EXISTS (SELECT 1 FROM "Proveedores" WHERE "RazonSocial" = 'Distribuidora Norte S.A.') THEN
                RAISE EXCEPTION 'El seed de datos de demo ya fue ejecutado anteriormente.';
            END IF;

            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Almacén', true) RETURNING "Id" INTO v_dep_almacen;
            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Bebidas', true) RETURNING "Id" INTO v_dep_bebidas;
            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Lácteos', true) RETURNING "Id" INTO v_dep_lacteos;
            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Limpieza', true) RETURNING "Id" INTO v_dep_limpieza;
            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Perfumería', true) RETURNING "Id" INTO v_dep_perfumeria;
            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Congelados', true) RETURNING "Id" INTO v_dep_congelados;
            INSERT INTO "Departamentos" ("Nombre", "Activo") VALUES ('Panadería', true) RETURNING "Id" INTO v_dep_panaderia;

            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Almacén seco', v_dep_almacen, true) RETURNING "Id" INTO v_fam_almacen1;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Condimentos y salsas', v_dep_almacen, true) RETURNING "Id" INTO v_fam_almacen2;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Gaseosas', v_dep_bebidas, true) RETURNING "Id" INTO v_fam_bebidas1;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Cervezas y vinos', v_dep_bebidas, true) RETURNING "Id" INTO v_fam_bebidas2;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Leches y yogures', v_dep_lacteos, true) RETURNING "Id" INTO v_fam_lacteos1;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Quesos y fiambres', v_dep_lacteos, true) RETURNING "Id" INTO v_fam_lacteos2;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Limpieza hogar', v_dep_limpieza, true) RETURNING "Id" INTO v_fam_limpieza1;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Lavado de ropa', v_dep_limpieza, true) RETURNING "Id" INTO v_fam_limpieza2;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Higiene personal', v_dep_perfumeria, true) RETURNING "Id" INTO v_fam_perfumeria1;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Cuidado capilar', v_dep_perfumeria, true) RETURNING "Id" INTO v_fam_perfumeria2;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Congelados varios', v_dep_congelados, true) RETURNING "Id" INTO v_fam_congelados1;
            INSERT INTO "Familias" ("Nombre", "IdDepartamento", "Activo") VALUES ('Panificados', v_dep_panaderia, true) RETURNING "Id" INTO v_fam_panaderia1;

            INSERT INTO "Marcas" ("Nombre", "Activo")
            SELECT unnest(ARRAY['Coca-Cola','Pepsi','Quilmes','La Serenísima','Ilolay','Sancor',
                'Matarazzo','Gallo','Natura','Ledesma','Playadito','Cruz de Malta','Higienol','Elite',
                'Magistral','Ayudín','Sedal','Dove','Bimbo','Fargo','Paty','McCain','Frigor','Molto',
                'Arcor','Georgalos','Terrabusi','Knorr','Hellmann''s','Cepita']), true;

            SELECT array_agg("Id" ORDER BY "Id") INTO v_marca_ids FROM "Marcas" WHERE "Nombre" != 'Sin marca';

            INSERT INTO "Proveedores" ("RazonSocial","NombreFantasia","Cuit","CondicionIva","Telefono","Email","Direccion","Localidad","Provincia","DiasEntrega","DiasVencimientoPago","SaldoCtaCte","Activo","FechaAlta")
            VALUES
                ('Distribuidora Norte S.A.','Norte Distribuciones','30-70123456-1',1,'011-4555-0101','ventas@dnorte.com.ar','Av. San Martín 1230','San Miguel','Buenos Aires',2,30,0,true,NOW()),
                ('Coca-Cola Andina Argentina S.A.','Andina','30-70234567-2',1,'011-4555-0202','pedidos@andina.com.ar','Ruta 8 Km 45','Pilar','Buenos Aires',1,15,0,true,NOW()),
                ('Mastellone Hnos. S.A.','La Serenísima','30-70345678-3',1,'011-4555-0303','distribucion@laserenisima.com.ar','Av. Del Libertador 2000','General Rodríguez','Buenos Aires',1,21,0,true,NOW()),
                ('Molinos Río de la Plata S.A.','Molinos','30-70456789-4',1,'011-4555-0404','ventas@molinos.com.ar','Av. Corrientes 800','CABA','CABA',3,30,0,true,NOW()),
                ('Arcor S.A.I.C.','Arcor','30-70567890-5',1,'0351-455-0505','pedidos@arcor.com','Ruta 9 Km 5','Arroyito','Córdoba',4,45,0,true,NOW()),
                ('Distribuidora Yerba del Litoral S.R.L.','Litoral Yerba','30-70678901-6',1,'03758-42-0606','ventas@litoralyerba.com.ar','Av. Illia 500','Posadas','Misiones',5,30,0,true,NOW()),
                ('Unilever de Argentina S.A.','Unilever','30-70789012-7',1,'011-4555-0707','pedidos@unilever.com.ar','Camino Gral. Belgrano 4550','Llavallol','Buenos Aires',3,45,0,true,NOW()),
                ('Procter & Gamble Argentina S.R.L.','P&G','30-70890123-8',1,'011-4555-0808','ventas@pg.com','Av. Bernardo Houssay 1455','Vicente López','Buenos Aires',3,45,0,true,NOW()),
                ('Bimbo de Argentina S.A.','Bimbo','30-70901234-9',1,'011-4555-0909','pedidos@bimbo.com.ar','Ruta Panamericana Km 30','Garín','Buenos Aires',1,15,0,true,NOW()),
                ('Paty S.A.','Paty','30-71012345-0',1,'011-4555-1010','ventas@paty.com.ar','Av. Riestra 3000','CABA','CABA',2,30,0,true,NOW()),
                ('McCain Argentina S.A.','McCain','30-71123456-1',1,'02323-45-1111','pedidos@mccain.com.ar','Ruta 5 Km 200','Balcarce','Buenos Aires',6,30,0,true,NOW()),
                ('Cervecería y Maltería Quilmes S.A.','Quilmes','30-71234567-2',1,'011-4555-1212','distribucion@cmq.com.ar','Av. 12 de Octubre 3000','Quilmes','Buenos Aires',1,21,0,true,NOW()),
                ('Distribuidora del Sur S.R.L.','Sur Distribuciones','30-71345678-3',1,'0291-455-1313','ventas@dsur.com.ar','Av. Colón 500','Bahía Blanca','Buenos Aires',3,30,0,true,NOW()),
                ('Georgalos Hnos. S.A.I.C.A.','Georgalos','30-71456789-4',1,'0223-455-1414','pedidos@georgalos.com.ar','Av. Independencia 2500','Mar del Plata','Buenos Aires',4,30,0,true,NOW()),
                ('Distribuidora Central de Alimentos S.A.','DCA','30-71567890-5',1,'011-4555-1515','ventas@dca.com.ar','Av. Eva Perón 4000','CABA','CABA',2,30,0,true,NOW());

            SELECT array_agg("Id" ORDER BY "Id") INTO v_prov_ids FROM "Proveedores";

            INSERT INTO "Clientes" ("RazonSocial","Cuit","CondicionIva","Telefono","Email","Direccion","Localidad","Provincia","IdListaPrecio","TieneCtaCte","LimiteCredito","SaldoCtaCte","TipoSaldo","EsMoroso","DiasVencimientoCtaCte","PorcentajeDescuento","Activo","FechaAlta")
            VALUES
                ('Almacén Don Pedro','20-25123456-3',1,'011-4222-1001','donpedro@mail.com','Av. Rivadavia 3400','CABA','CABA',2,true,50000,0,'H',false,15,5,true,NOW()),
                ('Kiosco La Esquina','20-26123456-4',5,'011-4222-1002','','San Martín 120','San Miguel','Buenos Aires',1,false,0,0,'H',false,0,0,true,NOW()),
                ('Restaurante El Fogón S.R.L.','30-71678901-6',1,'011-4222-1003','fogon@mail.com','Av. Cabildo 2200','CABA','CABA',2,true,80000,0,'H',false,30,8,true,NOW()),
                ('María González','27-30123456-5',5,'011-15-4222-1004','mgonzalez@mail.com','Calle Falsa 123','Morón','Buenos Aires',1,false,0,0,'H',false,0,0,true,NOW()),
                ('Comedor Escolar San José','30-71789012-7',4,'011-4222-1005','','Av. Belgrano 800','San Miguel','Buenos Aires',3,true,30000,0,'H',false,30,10,true,NOW()),
                ('Juan Carlos Pérez','20-31123456-6',5,'011-15-4222-1006','','Av. Mitre 500','San Miguel','Buenos Aires',1,false,0,0,'H',false,0,0,true,NOW()),
                ('Panadería y Confitería Sol','30-71890123-8',1,'011-4222-1007','','Av. San Martín 900','San Miguel','Buenos Aires',2,true,40000,0,'H',false,15,5,true,NOW()),
                ('Rotisería Doña Rosa','20-32123456-7',5,'011-4222-1008','','Belgrano 450','San Miguel','Buenos Aires',1,false,0,0,'H',false,0,0,true,NOW()),
                ('Distribuidora Barrial S.A.','30-71901234-9',1,'011-4222-1009','','Av. de Mayo 1200','CABA','CABA',2,true,100000,0,'H',false,30,10,true,NOW()),
                ('Ana Fernández','27-33123456-8',5,'011-15-4222-1010','','Sarmiento 300','San Miguel','Buenos Aires',1,false,0,0,'H',false,0,0,true,NOW());

            SELECT array_agg("Id" ORDER BY "Id") INTO v_cliente_ids FROM "Clientes" WHERE "Id" != 1;

            FOR rec IN
                SELECT * FROM (VALUES
                    ('Fideos Matarazzo Mostachol 500g'::text, v_dep_almacen, v_fam_almacen1, 21::numeric, 800::numeric),
                    ('Fideos Matarazzo Tallarín 500g',        v_dep_almacen, v_fam_almacen1, 21, 850),
                    ('Fideos Matarazzo Moñito 500g',           v_dep_almacen, v_fam_almacen1, 21, 850),
                    ('Arroz Gallo Largo Fino 1kg',              v_dep_almacen, v_fam_almacen1, 10.5, 1200),
                    ('Arroz Gallo Doble Carolina 1kg',          v_dep_almacen, v_fam_almacen1, 10.5, 1400),
                    ('Arroz Gallo Integral 500g',                v_dep_almacen, v_fam_almacen1, 10.5, 1000),
                    ('Aceite Natura Girasol 900ml',              v_dep_almacen, v_fam_almacen1, 10.5, 2200),
                    ('Aceite Natura Maíz 900ml',                  v_dep_almacen, v_fam_almacen1, 10.5, 2100),
                    ('Aceite Natura Oliva 500ml',                  v_dep_almacen, v_fam_almacen1, 10.5, 3500),
                    ('Azúcar Ledesma 1kg',                          v_dep_almacen, v_fam_almacen1, 10.5, 900),
                    ('Azúcar Ledesma Impalpable 500g',               v_dep_almacen, v_fam_almacen1, 10.5, 1100),
                    ('Yerba Playadito 1kg',                           v_dep_almacen, v_fam_almacen1, 21, 2800),
                    ('Yerba Playadito con Palo 500g',                  v_dep_almacen, v_fam_almacen1, 21, 1500),
                    ('Yerba Cruz de Malta 1kg',                          v_dep_almacen, v_fam_almacen1, 21, 3000),
                    ('Harina 0000 Morixe 1kg',                             v_dep_almacen, v_fam_almacen1, 10.5, 1000),
                    ('Harina Leudante Blancaflor 1kg',                       v_dep_almacen, v_fam_almacen1, 10.5, 1100),
                    ('Puré de Tomate Arcor 520g',                              v_dep_almacen, v_fam_almacen2, 10.5, 700),
                    ('Salsa de Tomate Arcor 340g',                               v_dep_almacen, v_fam_almacen2, 10.5, 600),
                    ('Polenta Arcor 500g',                                        v_dep_almacen, v_fam_almacen1, 10.5, 900),
                    ('Lentejas Arcor 500g',                                        v_dep_almacen, v_fam_almacen1, 10.5, 1300),
                    ('Garbanzos Arcor 500g',                                        v_dep_almacen, v_fam_almacen1, 10.5, 1500),
                    ('Porotos Arcor 500g',                                           v_dep_almacen, v_fam_almacen1, 10.5, 1400),
                    ('Café La Virginia Molido 250g',                                  v_dep_almacen, v_fam_almacen1, 21, 4500),
                    ('Té La Virginia x25 saquitos',                                    v_dep_almacen, v_fam_almacen1, 21, 3000),
                    ('Galletitas Terrabusi Cerealitas 200g',                             v_dep_almacen, v_fam_almacen1, 21, 1800),
                    ('Galletitas Terrabusi Melba 150g',                                    v_dep_almacen, v_fam_almacen1, 21, 1600),
                    ('Mermelada Arcor Durazno 454g',                                        v_dep_almacen, v_fam_almacen2, 21, 1900),
                    ('Mayonesa Hellmann''s 475g',                                            v_dep_almacen, v_fam_almacen2, 21, 2200),
                    ('Ketchup Hellmann''s 380g',                                              v_dep_almacen, v_fam_almacen2, 21, 1800),
                    ('Mostaza Hellmann''s 250g',                                               v_dep_almacen, v_fam_almacen2, 21, 1600),
                    ('Caldo Knorr Gallina x6',                                                  v_dep_almacen, v_fam_almacen2, 21, 900),
                    ('Caldo Knorr Verdura x6',                                                   v_dep_almacen, v_fam_almacen2, 21, 900),
                    ('Sopa Knorr Fideos 70g',                                                     v_dep_almacen, v_fam_almacen2, 21, 600),
                    ('Vinagre de Alcohol Genérico 500ml',                                          v_dep_almacen, v_fam_almacen1, 10.5, 500),
                    ('Sal Fina Celusal 500g',                                                       v_dep_almacen, v_fam_almacen1, 10.5, 700),
                    ('Coca-Cola 2.25L',            v_dep_bebidas, v_fam_bebidas1, 21, 1900),
                    ('Coca-Cola 1.5L',              v_dep_bebidas, v_fam_bebidas1, 21, 1500),
                    ('Coca-Cola Sin Azúcar 2.25L',    v_dep_bebidas, v_fam_bebidas1, 21, 1900),
                    ('Coca-Cola Lata 354ml',           v_dep_bebidas, v_fam_bebidas1, 21, 650),
                    ('Sprite 2.25L',                    v_dep_bebidas, v_fam_bebidas1, 21, 1800),
                    ('Fanta Naranja 2.25L',              v_dep_bebidas, v_fam_bebidas1, 21, 1800),
                    ('Pepsi 2.25L',                        v_dep_bebidas, v_fam_bebidas1, 21, 1700),
                    ('7UP 2.25L',                            v_dep_bebidas, v_fam_bebidas1, 21, 1700),
                    ('Paso de los Toros Pomelo 2.25L',         v_dep_bebidas, v_fam_bebidas1, 21, 1700),
                    ('Agua Mineral Villavicencio 1.5L',          v_dep_bebidas, v_fam_bebidas1, 21, 900),
                    ('Agua Mineral Villavicencio sin gas 1.5L',    v_dep_bebidas, v_fam_bebidas1, 21, 850),
                    ('Soda Villa del Sur 2L',                        v_dep_bebidas, v_fam_bebidas1, 21, 1100),
                    ('Cepita Jugo Naranja 1L',                         v_dep_bebidas, v_fam_bebidas1, 10.5, 1200),
                    ('Cepita Jugo Manzana 1L',                           v_dep_bebidas, v_fam_bebidas1, 10.5, 1200),
                    ('Cerveza Quilmes Clásica 1L',                          v_dep_bebidas, v_fam_bebidas2, 21, 1900),
                    ('Cerveza Quilmes Lata 473ml',                            v_dep_bebidas, v_fam_bebidas2, 21, 1300),
                    ('Cerveza Stella Artois 473ml',                             v_dep_bebidas, v_fam_bebidas2, 21, 1500),
                    ('Vino Toro Tinto 750ml',                                     v_dep_bebidas, v_fam_bebidas2, 21, 3000),
                    ('Vino Toro Blanco 750ml',                                      v_dep_bebidas, v_fam_bebidas2, 21, 3000),
                    ('Fernet Branca 750ml',                                           v_dep_bebidas, v_fam_bebidas2, 21, 7000),
                    ('Leche La Serenísima Entera 1L',      v_dep_lacteos, v_fam_lacteos1, 10.5, 1200),
                    ('Leche La Serenísima Descremada 1L',    v_dep_lacteos, v_fam_lacteos1, 10.5, 1200),
                    ('Leche Ilolay Entera 1L',                 v_dep_lacteos, v_fam_lacteos1, 10.5, 1100),
                    ('Yogur Ilolay Bebible Frutilla 1L',         v_dep_lacteos, v_fam_lacteos1, 10.5, 1400),
                    ('Yogur Ilolay Bebible Durazno 1L',            v_dep_lacteos, v_fam_lacteos1, 10.5, 1400),
                    ('Yogur La Serenísima Firme Vainilla 190g',      v_dep_lacteos, v_fam_lacteos1, 10.5, 500),
                    ('Manteca La Serenísima 200g',                     v_dep_lacteos, v_fam_lacteos1, 21, 1500),
                    ('Crema de Leche La Serenísima 200ml',               v_dep_lacteos, v_fam_lacteos1, 21, 1300),
                    ('Queso Cremoso Sancor 1kg',                           v_dep_lacteos, v_fam_lacteos2, 21, 4200),
                    ('Queso Rallado Sancor 100g',                            v_dep_lacteos, v_fam_lacteos2, 21, 1000),
                    ('Dulce de Leche Sancor 400g',                             v_dep_lacteos, v_fam_lacteos1, 21, 1600),
                    ('Jamón Cocido Fiambre 1kg',                                 v_dep_lacteos, v_fam_lacteos2, 21, 4000),
                    ('Salame Fiambre 1kg',                                         v_dep_lacteos, v_fam_lacteos2, 21, 5500),
                    ('Mortadela Fiambre 1kg',                                       v_dep_lacteos, v_fam_lacteos2, 21, 3200),
                    ('Queso Fresco Sancor 500g',                                      v_dep_lacteos, v_fam_lacteos2, 21, 2400),
                    ('Detergente Magistral Limón 750ml',       v_dep_limpieza, v_fam_limpieza1, 21, 900),
                    ('Detergente Magistral Original 750ml',      v_dep_limpieza, v_fam_limpieza1, 21, 900),
                    ('Lavandina Ayudín 1L',                        v_dep_limpieza, v_fam_limpieza1, 21, 700),
                    ('Lavandina Ayudín Perfumada 1L',                v_dep_limpieza, v_fam_limpieza1, 21, 750),
                    ('Suavizante Vivere 900ml',                        v_dep_limpieza, v_fam_limpieza2, 21, 1600),
                    ('Jabón en Polvo Skip 800g',                          v_dep_limpieza, v_fam_limpieza2, 21, 2600),
                    ('Jabón en Polvo Ariel 800g',                            v_dep_limpieza, v_fam_limpieza2, 21, 2800),
                    ('Jabón Blanco en Panes Federal x3',                        v_dep_limpieza, v_fam_limpieza1, 21, 900),
                    ('Limpiador Cif Cremoso 500ml',                                v_dep_limpieza, v_fam_limpieza1, 21, 1400),
                    ('Limpiador Poett Multiuso 500ml',                               v_dep_limpieza, v_fam_limpieza1, 21, 1300),
                    ('Papel Higiénico Higienol x4',                                     v_dep_limpieza, v_fam_limpieza1, 21, 1600),
                    ('Papel Higiénico Elite x4',                                           v_dep_limpieza, v_fam_limpieza1, 21, 1700),
                    ('Rollo de Cocina Elite x2',                                             v_dep_limpieza, v_fam_limpieza1, 21, 1200),
                    ('Esponja Scotch Brite x3',                                                v_dep_limpieza, v_fam_limpieza1, 21, 900),
                    ('Trapo de Piso Genérico',                                                    v_dep_limpieza, v_fam_limpieza1, 21, 800),
                    ('Shampoo Sedal 400ml',                     v_dep_perfumeria, v_fam_perfumeria2, 21, 2200),
                    ('Acondicionador Sedal 400ml',                 v_dep_perfumeria, v_fam_perfumeria2, 21, 2200),
                    ('Jabón de Tocador Dove 90g',                    v_dep_perfumeria, v_fam_perfumeria1, 21, 700),
                    ('Jabón Líquido Dove 250ml',                        v_dep_perfumeria, v_fam_perfumeria1, 21, 1800),
                    ('Desodorante Rexona Aerosol 150ml',                   v_dep_perfumeria, v_fam_perfumeria1, 21, 2100),
                    ('Desodorante Axe Aerosol 150ml',                         v_dep_perfumeria, v_fam_perfumeria1, 21, 2000),
                    ('Pasta Dental Colgate 90g',                                 v_dep_perfumeria, v_fam_perfumeria1, 21, 1200),
                    ('Cepillo Dental Colgate',                                      v_dep_perfumeria, v_fam_perfumeria1, 21, 900),
                    ('Máquina de Afeitar Gillette Prestobarba x2',                     v_dep_perfumeria, v_fam_perfumeria1, 21, 1900),
                    ('Toallitas Femeninas Always x8',                                     v_dep_perfumeria, v_fam_perfumeria1, 21, 1700),
                    ('Pañales Pampers Talle M x30',                                          v_dep_perfumeria, v_fam_perfumeria1, 21, 8500),
                    ('Algodón Estrella 100g',                                                   v_dep_perfumeria, v_fam_perfumeria1, 21, 900),
                    ('Hamburguesas Paty x4',           v_dep_congelados, v_fam_congelados1, 21, 2200),
                    ('Hamburguesas Paty x8',              v_dep_congelados, v_fam_congelados1, 21, 4000),
                    ('Papas Fritas McCain 1kg',              v_dep_congelados, v_fam_congelados1, 21, 3000),
                    ('Papas Rústicas McCain 1kg',               v_dep_congelados, v_fam_congelados1, 21, 3100),
                    ('Medallones de Pollo McCain x10',            v_dep_congelados, v_fam_congelados1, 21, 3600),
                    ('Helado Frigor Vainilla 1L',                    v_dep_congelados, v_fam_congelados1, 21, 4500),
                    ('Helado Frigor Chocolate 1L',                      v_dep_congelados, v_fam_congelados1, 21, 4700),
                    ('Palitos de Merluza Congelados x10',                  v_dep_congelados, v_fam_congelados1, 21, 3200),
                    ('Pan Lactal Bimbo Grande',      v_dep_panaderia, v_fam_panaderia1, 10.5, 1500),
                    ('Pan Lactal Bimbo Chico',          v_dep_panaderia, v_fam_panaderia1, 10.5, 1300),
                    ('Pan Lactal Fargo Integral',           v_dep_panaderia, v_fam_panaderia1, 10.5, 1600),
                    ('Facturas Surtidas x6',                  v_dep_panaderia, v_fam_panaderia1, 10.5, 1400),
                    ('Medialunas de Manteca x6',                  v_dep_panaderia, v_fam_panaderia1, 10.5, 1500),
                    ('Bizcochos de Grasa x250g',                     v_dep_panaderia, v_fam_panaderia1, 10.5, 1600),
                    ('Pan Rallado Genérico 500g',                       v_dep_panaderia, v_fam_panaderia1, 10.5, 700),
                    ('Tostadas Fargo Clásicas 200g',                       v_dep_panaderia, v_fam_panaderia1, 10.5, 900)
                ) AS t(nombre, id_depto, id_familia, iva, costo)
            LOOP
                i := COALESCE(i, 0) + 1;
                marca_id := v_marca_ids[1 + (i % array_length(v_marca_ids, 1))];
                prov_id := v_prov_ids[1 + (i % array_length(v_prov_ids, 1))];
                margen := 25 + (random() * 45);
                venta := round(rec.costo * (1 + margen / 100), 2);
                stock := floor(random() * 80 + 10)::numeric;

                INSERT INTO "Articulos" (
                    "CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta",
                    "IdDepartamento","IdFamilia","IdMarca","IdProveedor",
                    "PrecioCosto","PrecioVenta","PrecioOferta","MargenGanancia",
                    "Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1",
                    "AlicuotaIva","AplicaIva","ImpuestoInterno",
                    "UnidadesPorBulto","CajasPorBulto","EsPesable","BanderaEAN","ContenidoValor","ContenidoUnidad",
                    "StockActual","StockMinimo","StockMaximo","StockDeposito",
                    "Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento",
                    "FechaAlta","CantidadVendida","MultiplicadorStock"
                ) VALUES (
                    '779' || lpad(i::text, 10, '0'),
                    'ART' || lpad(i::text, 5, '0'),
                    'PROV' || lpad((i % 500)::text, 4, '0'),
                    rec.nombre,
                    left(rec.nombre, 40),
                    rec.id_depto, rec.id_familia, marca_id, prov_id,
                    rec.costo, venta, venta, margen,
                    0,0,0,0,0,0,
                    rec.iva, true, 0,
                    1, 1, false, 0, 1, 'UN',
                    stock, floor(stock * 0.2), floor(stock * 2 + 20), floor(random() * 40)::numeric,
                    true, false, false, false,
                    NOW() - (random() * 180 || ' days')::interval, floor(random() * 200)::numeric, 1
                ) RETURNING "Id" INTO art_id;
            END LOOP;

            SELECT array_agg("Id" ORDER BY "Id") INTO v_articulo_ids FROM "Articulos";
            v_articulo_count := array_length(v_articulo_ids, 1);

            INSERT INTO "ArticulosStockPorSucursal" ("IdArticulo","IdSucursal","Cantidad")
            SELECT "Id", 1, "StockActual" FROM "Articulos"
            ON CONFLICT ("IdArticulo","IdSucursal") DO NOTHING;

            INSERT INTO "ArticulosStockPorSucursal" ("IdArticulo","IdSucursal","Cantidad")
            SELECT "Id", 2, floor(random() * 15)::numeric FROM "Articulos"
            ON CONFLICT ("IdArticulo","IdSucursal") DO NOTHING;

            SELECT COALESCE(MAX("NroOrden"), 0) + 1 INTO v_oc_num FROM "OrdenesCompra";

            FOR i IN 1..25 LOOP
                prov_id := v_prov_ids[1 + floor(random() * array_length(v_prov_ids, 1))::int];
                v_fecha := NOW() - (random() * 60 || ' days')::interval;
                total_sin_iva := 0;
                total_iva := 0;

                INSERT INTO "OrdenesCompra" (
                    "IdProveedor","IdUsuario","NroOrden","Fecha","FechaEntregaEsperada","Estado",
                    "TotalSinIva","TotalIva","Total","Observaciones"
                ) VALUES (
                    prov_id, 1, v_oc_num, v_fecha, v_fecha + interval '5 days',
                    CASE WHEN i <= 15 THEN 2 WHEN i <= 22 THEN 0 WHEN i = 23 THEN 1 ELSE 4 END,
                    0, 0, 0, 'OC generada por script de datos de demo'
                ) RETURNING "Id" INTO oc_id;

                n_items := 4 + floor(random() * 4)::int;
                FOR j IN 1..n_items LOOP
                    art_id := v_articulo_ids[1 + floor(random() * v_articulo_count)::int];
                    SELECT "PrecioCosto", "AlicuotaIva" INTO costo, margen FROM "Articulos" WHERE "Id" = art_id;
                    cant := floor(random() * 40 + 10)::numeric;
                    subtotal := round(costo * cant, 2);
                    total_sin_iva := total_sin_iva + subtotal;
                    total_iva := total_iva + round(subtotal * margen / 100, 2);

                    INSERT INTO "OrdenesCompraDetalle" (
                        "IdOrdenCompra","IdArticulo","CantidadPedida","CantidadRecibida","PrecioCosto","AlicuotaIva","Subtotal"
                    ) VALUES (
                        oc_id, art_id, cant,
                        CASE WHEN i <= 15 THEN cant ELSE 0 END,
                        costo, margen, subtotal
                    );
                END LOOP;

                UPDATE "OrdenesCompra"
                SET "TotalSinIva" = total_sin_iva, "TotalIva" = total_iva, "Total" = total_sin_iva + total_iva,
                    "FechaRecepcion" = CASE WHEN i <= 15 THEN v_fecha + interval '4 days' ELSE NULL END,
                    "IdUsuarioRecepcion" = CASE WHEN i <= 15 THEN 1 ELSE NULL END
                WHERE "Id" = oc_id;

                v_oc_num := v_oc_num + 1;
            END LOOP;

            SELECT COALESCE(MAX("NroRemito"), 0) + 1 INTO v_rem_num FROM "Remitos" WHERE "Tipo" = 0;

            FOR i IN 1..18 LOOP
                prov_id := v_prov_ids[1 + floor(random() * array_length(v_prov_ids, 1))::int];
                v_fecha := NOW() - (random() * 45 || ' days')::interval;

                INSERT INTO "Remitos" (
                    "NroRemito","Tipo","Fecha","IdProveedor","IdUsuario","NroRemitoExterno","Transportista","Estado","Observaciones"
                ) VALUES (
                    v_rem_num, 0, v_fecha, prov_id, 1,
                    'RP-' || lpad((1000 + i)::text, 6, '0'),
                    (ARRAY['Transporte Rápido S.A.','Cargo Express','Andreani','OCA Cargas','Flete Propio'])[1 + floor(random() * 5)::int],
                    CASE WHEN i <= 14 THEN 1 ELSE 0 END,
                    'Remito generado por script de datos de demo'
                ) RETURNING "Id" INTO rem_id;

                n_items := 3 + floor(random() * 4)::int;
                FOR j IN 1..n_items LOOP
                    art_id := v_articulo_ids[1 + floor(random() * v_articulo_count)::int];
                    SELECT "PrecioCosto" INTO costo FROM "Articulos" WHERE "Id" = art_id;
                    cant := floor(random() * 30 + 5)::numeric;

                    INSERT INTO "RemitosDetalle" (
                        "IdRemito","IdArticulo","CantidadRemitida","CantidadRecibida","PrecioCosto"
                    ) VALUES (
                        rem_id, art_id, cant,
                        CASE WHEN i <= 14 THEN cant ELSE 0 END,
                        costo
                    );
                END LOOP;

                v_rem_num := v_rem_num + 1;
            END LOOP;

            SELECT COALESCE(MAX("Numero"), 0) + 1 INTO v_num FROM "Comprobantes" WHERE "IdTipoComprobante" = 7;

            FOR i IN 1..450 LOOP
                v_fecha := NOW() - (random() * 90 || ' days')::interval;
                v_cliente := CASE WHEN random() < 0.6 THEN 1 ELSE v_cliente_ids[1 + floor(random() * array_length(v_cliente_ids, 1))::int] END;

                INSERT INTO "Comprobantes" (
                    "IdTipoComprobante","Letra","PuntoVenta","Numero","Fecha","IdCliente","IdCaja","IdSucursal","IdUsuario",
                    "Comision","SubTotal","TotalDescuento","TotalIva21","TotalIva105","TotalIva0","Total","Estado","EsFacturaElectronica"
                ) VALUES (
                    7, 'B', 1, v_num, v_fecha, v_cliente, 1, 1, 1,
                    0, 0, 0, 0, 0, 0, 0, 1, false
                ) RETURNING "Id" INTO comp_id;

                v_total := 0;
                v_subtotal := 0;
                n_items := 2 + floor(random() * 5)::int;

                FOR j IN 1..n_items LOOP
                    art_id := v_articulo_ids[1 + floor(random() * v_articulo_count)::int];
                    SELECT "PrecioVenta","AlicuotaIva" INTO precio, margen
                        FROM "Articulos" WHERE "Id" = art_id;

                    cant := floor(random() * 4 + 1)::numeric;
                    precio_sin_iva := round(precio / (1 + margen / 100), 4);
                    monto_iva := round(precio - precio_sin_iva, 4);
                    subtotal := round(precio * cant, 2);
                    v_total := v_total + subtotal;
                    v_subtotal := v_subtotal + subtotal;

                    UPDATE "Articulos" SET "StockActual" = GREATEST("StockActual" - cant, 0),
                        "CantidadVendida" = "CantidadVendida" + cant, "UltimaVenta" = v_fecha
                        WHERE "Id" = art_id;

                    INSERT INTO "ComprobantesDetalle" (
                        "IdComprobante","IdArticulo","Descripcion","Cantidad","PrecioUnitario","PrecioUnitarioSinIva",
                        "AlicuotaIva","MontoIva","PorcentajeDescuento","MontoDescuento","SubTotal"
                    )
                    SELECT comp_id, art_id, a."Descripcion", cant, a."PrecioVenta", precio_sin_iva, a."AlicuotaIva", monto_iva, 0, 0, subtotal
                    FROM "Articulos" a WHERE a."Id" = art_id;
                END LOOP;

                v_medio := (ARRAY[1,1,1,2,3,5])[1 + floor(random() * 6)::int];
                INSERT INTO "ComprobantesPago" ("IdComprobante","IdMedioPago","Importe","Vuelto")
                VALUES (comp_id, v_medio, v_total, 0);

                UPDATE "Comprobantes"
                SET "Total" = v_total, "SubTotal" = v_subtotal,
                    "TotalIva21" = round(v_total * 0.17, 2), "TotalIva0" = 0
                WHERE "Id" = comp_id;

                v_num := v_num + 1;
            END LOOP;

            SELECT COALESCE(MAX("Numero"), 0) + 1 INTO v_pre_num FROM "Presupuestos";

            FOR i IN 1..20 LOOP
                v_fecha := NOW() - (random() * 30 || ' days')::interval;
                v_cliente := v_cliente_ids[1 + floor(random() * array_length(v_cliente_ids, 1))::int];
                v_total := 0;
                v_subtotal := 0;

                INSERT INTO "Presupuestos" (
                    "Numero","Fecha","IdCliente","IdUsuario","IdSucursal","PlazoValidezDias",
                    "Contacto","Detalle","Observacion","FormaPago","SubTotal","Total","Estado"
                ) VALUES (
                    v_pre_num, v_fecha, v_cliente, 1, 1, 15,
                    'Contacto comercial', 'Presupuesto de demo generado por script', 'Sujeto a disponibilidad de stock',
                    'Contado / Cta. Cte.', 0, 0,
                    CASE WHEN i <= 6 THEN 3 WHEN i <= 14 THEN 1 ELSE 0 END
                ) RETURNING "Id" INTO pre_id;

                n_items := 2 + floor(random() * 4)::int;
                FOR j IN 1..n_items LOOP
                    art_id := v_articulo_ids[1 + floor(random() * v_articulo_count)::int];
                    SELECT "PrecioCosto","PrecioVenta" INTO costo, precio FROM "Articulos" WHERE "Id" = art_id;
                    cant := floor(random() * 10 + 1)::numeric;
                    subtotal := round(precio * cant, 2);
                    v_total := v_total + subtotal;
                    v_subtotal := v_subtotal + subtotal;

                    INSERT INTO "PresupuestosDetalle" (
                        "IdPresupuesto","IdArticulo","ItemNro","Descripcion","Costo","Cantidad","Precio","Margen"
                    )
                    SELECT pre_id, art_id, j, a."Descripcion", costo, cant, precio,
                           CASE WHEN costo > 0 THEN round((precio - costo) / costo * 100, 2) ELSE 0 END
                    FROM "Articulos" a WHERE a."Id" = art_id;
                END LOOP;

                UPDATE "Presupuestos" SET "SubTotal" = v_subtotal, "Total" = v_total WHERE "Id" = pre_id;

                v_pre_num := v_pre_num + 1;
            END LOOP;

        END $$;
        """;

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
