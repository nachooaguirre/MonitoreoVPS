-- =============================================================================
-- SuperPOS — carga MASIVA de datos de prueba (PostgreSQL)
-- (Esto es un script de INSERT/DELETE explícito: NO es inyección SQL de ataque,
--  solo rellená la base para desarrollo/QA.)
-- Uso: psql -U usuario -d superpos -f seed_demo_masivo.sql
--    o pegar en pgAdmin / DBeaver (ejecutar todo de una).
-- Re-ejecución: elimina filas con prefijos DEMO-*/DEMO-SEED antes de volver a insertar.
-- =============================================================================
BEGIN;

-- --- Parámetros (ajustá si hace falta) ------------------------------------------
-- Sucursal / caja / cliente / usuario de seed
--  IdCliente=1, IdCaja=1, IdSucursal=1, IdUsuario=1, IdSucursal=1, TiposComp Ticket=7

-- =============================================================================
-- 0) Limpieza de corridas anteriores (sólo filas de demo)
-- =============================================================================
DELETE FROM "ComprobantesPago" WHERE "IdComprobante" IN (
  SELECT c."Id" FROM "Comprobantes" c
  WHERE c."Observaciones" LIKE 'DEMO-SEED%'
);
DELETE FROM "ComprobantesDetalle" WHERE "IdComprobante" IN (
  SELECT c."Id" FROM "Comprobantes" c WHERE c."Observaciones" LIKE 'DEMO-SEED%'
);
DELETE FROM "Comprobantes" WHERE "Observaciones" LIKE 'DEMO-SEED%';

DELETE FROM "BonificacionesRango" WHERE "IdArticulo" IN (
  SELECT a."Id" FROM "Articulos" a WHERE a."Descripcion" LIKE 'DEMO-ART%'
);

DELETE FROM "ListasPrecioProveedorLineas" WHERE "IdLista" IN (
  SELECT l."Id" FROM "ListasPrecioProveedor" l
  WHERE l."Notas" LIKE 'DEMO-SEED%'
);
DELETE FROM "ListasPrecioProveedor" WHERE "Notas" LIKE 'DEMO-SEED%';

DELETE FROM "ArticulosPreciosListas" WHERE "IdLista" IN (2,3)
  AND "IdArticulo" IN (SELECT a."Id" FROM "Articulos" a WHERE a."Descripcion" LIKE 'DEMO-ART%');

DELETE FROM "ArticulosStockPorSucursal" WHERE "IdArticulo" IN (
  SELECT a."Id" FROM "Articulos" a WHERE a."Descripcion" LIKE 'DEMO-ART%'
);

DELETE FROM "Articulos" WHERE "Descripcion" LIKE 'DEMO-ART%';
DELETE FROM "Proveedores" WHERE "RazonSocial" LIKE 'DEMO-PROV%';

-- =============================================================================
-- 1) Proveedores demo (20)
-- =============================================================================
INSERT INTO "Proveedores" (
  "RazonSocial", "NombreFantasia", "Cuit", "CondicionIva",
  "DiasEntrega", "DiasVencimientoPago", "SaldoCtaCte", "FechaAlta", "Activo"
)
SELECT
  'DEMO-PROV ' || LPAD(s::text, 2, '0'),
  'Alias demo ' || s,
  '20-' || LPAD((3000000 + s)::text, 8, '0') || '-' || (s % 9)::text,
  1, 2 + (s % 5), 15 + (s % 20), 0,
  (TIMESTAMP '2024-01-01 12:00:00+00' + (s || ' days')::interval) AT TIME ZONE 'UTC',
  true
FROM generate_series(1, 30) s;

-- =============================================================================
-- 2) Array de Ids de proveedores (para aritmética)
-- =============================================================================
CREATE TEMP TABLE _demo_prov AS
SELECT "Id", row_number() OVER (ORDER BY "Id") - 1 AS ord
FROM "Proveedores"
WHERE "RazonSocial" LIKE 'DEMO-PROV%'
ORDER BY "Id";

-- =============================================================================
-- 3) Artículos demo (500) con precios, stock, bonif. en ficha, EAN y código prov.
-- =============================================================================
INSERT INTO "Articulos" (
  "CodigoBarras", "CodigoInterno", "CodigoProveedor", "Descripcion", "DescripcionCorta",
  "IdDepartamento", "IdFamilia", "IdMarca", "IdProveedor",
  "PrecioCosto", "PrecioVenta", "PrecioOferta", "MargenGanancia",
  "Bonificacion1", "Bonificacion2", "Bonificacion3", "Bonificacion4", "Bonificacion5", "Recargo1",
  "AlicuotaIva", "AplicaIva", "ImpuestoInterno",
  "UnidadesPorBulto", "CajasPorBulto", "EsPesable", "BanderaEAN",
  "StockActual", "StockMinimo", "StockMaximo", "StockDeposito",
  "Activo", "RequiereNroSerie", "RequiereNroLote", "RequiereFechaVencimiento",
  "FechaAlta", "CantidadVendida",
  "DepartamentoId", "FamiliaId", "MarcaId", "ProveedorId"
)
SELECT
  '7790DEM' || LPAD(n::text, 5, '0'),
  'DIN' || LPAD(n::text, 5, '0'),
  'CP-OK' || LPAD(n::text, 4, '0'),
  'DEMO-ART ' || n || ' ' ||
    (ARRAY['Gaseosa 2.25L','Leche sachet','Yerba 1kg','Arroz 1kg','Aceite 900ml',
           'Detergente 1L','Papel higiénico','Cerveza lata','Fideos 500g',
           'Azúcar 1kg','Sal fina 1kg','Manteca 200g'])[1 + n % 11],
  'A' || n,
  1, 1, 1,
  p."Id",
  ROUND(50 + n * 0.37, 2),
  ROUND(85 + n * 0.55, 2),
  CASE WHEN n % 6 = 0 THEN ROUND(75 + n * 0.4, 2) ELSE 0 END,
  28.0,
  (n % 5) * 1.0, 0, 0, 0, 0, 0,
  CASE WHEN n % 17 = 0 THEN 10.5 ELSE 21 END,
  true, 0,
  1, 1, false, 0,
  LEAST(500, 10 + n % 120),
  5 + n % 20,
  200 + n % 300,
  0,
  true, false, n % 11 = 0, false,
  (TIMESTAMP '2024-06-01 00:00:00+00' + (n % 200 || ' days')::interval) AT TIME ZONE 'UTC',
  0,
  1, 1, 1, p."Id"
FROM generate_series(1, 500) n
JOIN LATERAL (
  SELECT "Id" FROM _demo_prov WHERE ord = (n - 1) % (SELECT COUNT(*)::int FROM _demo_prov)
) p ON true;

-- Stock por sucursal (1) para no romper módulos de stock
INSERT INTO "ArticulosStockPorSucursal" ("IdArticulo", "IdSucursal", "Cantidad")
SELECT a."Id", 1, 30 + (a."Id" % 100)
FROM "Articulos" a
WHERE a."Descripcion" LIKE 'DEMO-ART%'
ON CONFLICT ("IdArticulo", "IdSucursal") DO UPDATE SET "Cantidad" = EXCLUDED."Cantidad";

-- Listas al público (2=Mayorista, 3=Empleados): precios ajuste por id lista
-- Tabla: IdLista, IdArticulo, Precio, PorcentajeAjuste
INSERT INTO "ArticulosPreciosListas" ("IdLista", "IdArticulo", "Precio", "PorcentajeAjuste")
SELECT
  2, a."Id", ROUND(a."PrecioVenta" * 0.92, 2), -8
FROM "Articulos" a
WHERE a."Descripcion" LIKE 'DEMO-ART%' AND a."Id" % 3 = 0
ON CONFLICT ("IdLista", "IdArticulo") DO UPDATE
SET "Precio" = EXCLUDED."Precio", "PorcentajeAjuste" = EXCLUDED."PorcentajeAjuste";

INSERT INTO "ArticulosPreciosListas" ("IdLista", "IdArticulo", "Precio", "PorcentajeAjuste")
SELECT
  3, a."Id", ROUND(a."PrecioVenta" * 0.88, 2), -12
FROM "Articulos" a
WHERE a."Descripcion" LIKE 'DEMO-ART%' AND a."Id" % 5 = 0
ON CONFLICT ("IdLista", "IdArticulo") DO UPDATE
SET "Precio" = EXCLUDED."Precio", "PorcentajeAjuste" = EXCLUDED."PorcentajeAjuste";

-- =============================================================================
-- 4) Listas de precio de PROVEEDOR con JSON de bonificaciones (escalones)
-- =============================================================================
WITH
ins_lista AS (
  INSERT INTO "ListasPrecioProveedor" (
    "IdProveedor", "Nombre", "Notas", "FechaCargaUtc", "ArchivoOrigenNombre", "Activo"
  )
  SELECT p."Id",
         'DEMO tarifa Abril-2026',
         'DEMO-SEED lista 1: escalas 6/10/20 cajas',
         NOW() AT TIME ZONE 'UTC',
         'synthetic',
         true
  FROM (SELECT "Id" FROM "Proveedores" WHERE "RazonSocial" = 'DEMO-PROV 01' LIMIT 1) p
  RETURNING "Id" AS id_lista, "IdProveedor"
)
INSERT INTO "ListasPrecioProveedorLineas" (
  "IdLista", "IdArticulo", "CodigoProveedor", "Descripcion", "PrecioUnitario", "IvaPorcentaje", "BonificacionesJson"
)
SELECT
  l.id_lista,
  a."Id",
  a."CodigoProveedor",
  a."Descripcion",
  a."PrecioCosto" * 0.95,
  10.5,
  '[{"cantidadMin":6,"porcentaje":2,"nota":">=6 cajas"},{"cantidadMin":10,"porcentaje":3.5,"nota":">=10"},{"cantidadMin":20,"porcentaje":5,"nota":">=20 piso"}]'::varchar
FROM ins_lista l
JOIN "Articulos" a ON a."IdProveedor" = l."IdProveedor" AND a."Descripcion" LIKE 'DEMO-ART%'
WHERE a."Id" % 2 = 0
LIMIT 100;

-- Segunda lista: otro patrón de bonificaciones
WITH p AS (
  SELECT "Id" AS id_proveedor FROM "Proveedores" WHERE "RazonSocial" = 'DEMO-PROV 02' LIMIT 1
),
ins2 AS (
  INSERT INTO "ListasPrecioProveedor" (
    "IdProveedor", "Nombre", "Notas", "FechaCargaUtc", "ArchivoOrigenNombre", "Activo"
  )
  SELECT p.id_proveedor, 'DEMO oferta perecederos (bonif. bulto)',
    'DEMO-SEED lista 2: picos 12 y 50 unid.', NOW() AT TIME ZONE 'UTC', 'synthetic-2', true
  FROM p
  RETURNING "Id", "IdProveedor"
)
INSERT INTO "ListasPrecioProveedorLineas" (
  "IdLista", "IdArticulo", "CodigoProveedor", "Descripcion", "PrecioUnitario", "IvaPorcentaje", "BonificacionesJson"
)
SELECT
  i."Id",
  a."Id",
  a."CodigoProveedor",
  a."Descripcion",
  a."PrecioCosto" * 0.88,
  21,
  '[{"cantidadMin":12,"porcentaje":4,"nota":">=12 bulto"},{"cantidadMin":25,"porcentaje":6},{"cantidadMin":50,"porcentaje":8.5,"nota":"camión"}]'::varchar
FROM ins2 i
JOIN "Articulos" a ON a."IdProveedor" = i."IdProveedor" AND a."Descripcion" LIKE 'DEMO-ART%'
WHERE a."Id" % 3 = 0
LIMIT 100;

-- Tercera lista: 4 tramos de bonif. (DEMO-PROV 03)
WITH p3 AS (
  SELECT "Id" AS idp FROM "Proveedores" WHERE "RazonSocial" = 'DEMO-PROV 03' LIMIT 1
),
ins3 AS (
  INSERT INTO "ListasPrecioProveedor" (
    "IdProveedor", "Nombre", "Notas", "FechaCargaUtc", "ArchivoOrigenNombre", "Activo"
  )
  SELECT p3.idp, 'DEMO tarifa escalonada 4x',
    'DEMO-SEED lista 3: 4 bordes consecutivos + camión 100',
    NOW() AT TIME ZONE 'UTC', 'synthetic-3', true
  FROM p3
  RETURNING "Id", "IdProveedor"
)
INSERT INTO "ListasPrecioProveedorLineas" (
  "IdLista", "IdArticulo", "CodigoProveedor", "Descripcion", "PrecioUnitario", "IvaPorcentaje", "BonificacionesJson"
)
SELECT
  i."Id",
  a."Id", a."CodigoProveedor", a."Descripcion", ROUND(a."PrecioCosto" * 0.91, 2), 10.5,
  '[{"cantidadMin":4,"porcentaje":1.2},{"cantidadMin":8,"porcentaje":2.1},{"cantidadMin":16,"porcentaje":3.5},{"cantidadMin":32,"porcentaje":5.5,"nota":"bulto"},{"cantidadMin":100,"porcentaje":8,"nota":"camión"}]'::varchar
FROM ins3 i
JOIN "Articulos" a ON a."IdProveedor" = i."IdProveedor" AND a."Descripcion" LIKE 'DEMO-ART%'
WHERE a."Id" % 2 = 1
LIMIT 90;

-- Bonificaciones por rango (volumen) en ficha, para probar otras pantallas
INSERT INTO "BonificacionesRango" ("IdArticulo", "CantidadDesde", "CantidadHasta", "PorcentajeDescuento")
SELECT a."Id", 6, 12, 1.5
FROM "Articulos" a
WHERE a."Descripcion" LIKE 'DEMO-ART%' AND a."Id" % 11 = 0;
INSERT INTO "BonificacionesRango" ("IdArticulo", "CantidadDesde", "CantidadHasta", "PorcentajeDescuento")
SELECT a."Id", 13, 30, 3.0
FROM "Articulos" a
WHERE a."Descripcion" LIKE 'DEMO-ART%' AND a."Id" % 11 = 0;
INSERT INTO "BonificacionesRango" ("IdArticulo", "CantidadDesde", "CantidadHasta", "PorcentajeDescuento")
SELECT a."Id", 50, 99999, 6.0
FROM "Articulos" a
WHERE a."Descripcion" LIKE 'DEMO-ART%' AND a."Id" % 19 = 0;

-- =============================================================================
-- 5) Ventas: ~500 tickets en 40 días, 1-6 ítems c/u, ticket + pago
-- =============================================================================
DO $$
DECLARE
  v_tipo    int := 7;   -- Ticket
  v_cli     int := 1;   -- Consumidor final
  v_caja    int := 1;
  v_suc     int := 1;
  v_user    int := 1;
  v_estado  int := 1;   -- Emitido
  d         date;
  t         int;
  n_base    bigint;
  hdr_id    bigint;
  a_rec     record;
  num       bigint;
  letra     char := 'B';
  pto       int  := 1;
  v_total   numeric(18,2);
  v_sub     numeric(18,2);
  v_iva21   numeric(18,2);
  v_iva105  numeric(18,2);
  v_line    numeric(18,2);
  v_net     numeric(18,2);
  v_iva     numeric(18,2);
  p_unit    numeric(18,4);
  qty       numeric(18,3);
  arr_ids   int[];
  i         int;
  j         int;
  lines     int;
BEGIN
  SELECT COALESCE(MAX("Numero"), 5000000) + 1 INTO n_base
  FROM "Comprobantes" WHERE "PuntoVenta" = pto AND "Letra" = letra AND "IdSucursal" = v_suc;

  -- ids de artículos demo (orden aleatorio)
  SELECT coalesce(array_agg(t."Id"), ARRAY[]::int[]) INTO arr_ids
  FROM (SELECT s."Id" FROM "Articulos" s
        WHERE s."Descripcion" LIKE 'DEMO-ART%' ORDER BY random()) t;

  IF coalesce(array_length(arr_ids, 1), 0) < 3 THEN
    RAISE NOTICE 'No hay artículos DEMO-ART. Abortando comprobantes.';
    RETURN;
  END IF;

  num := n_base;
  FOR t IN 1..500 LOOP
    d := (CURRENT_DATE - (random() * 40)::int);
    v_sub := 0; v_iva21 := 0; v_iva105 := 0; v_total := 0;
    v_net := 0; v_iva := 0;

    INSERT INTO "Comprobantes" (
      "IdTipoComprobante", "Letra", "PuntoVenta", "Numero", "Fecha", "IdCliente", "IdCaja", "IdSucursal", "IdUsuario",
      "SubTotal", "TotalDescuento", "TotalIva21", "TotalIva105", "TotalIva0", "Total",
      "Estado", "EsFacturaElectronica", "Observaciones"
    ) VALUES (
      v_tipo, letra, pto, num, d::timestamptz + (random() * 86400 || ' second')::interval,
      v_cli, v_caja, v_suc, v_user,
      0, 0, 0, 0, 0, 0, v_estado, false, 'DEMO-SEED ticket ' || t
    ) RETURNING "Id" INTO hdr_id;

    lines := 1 + (t % 6);
    v_sub := 0; v_iva21 := 0; v_iva105 := 0; v_total := 0;
    FOR j IN 1..lines LOOP
      i := arr_ids[1 + ((t * 17 + j * 3) % array_length(arr_ids,1))];
      SELECT a."Id", a."Descripcion", a."PrecioVenta", a."AlicuotaIva" INTO a_rec
      FROM "Articulos" a WHERE a."Id" = i;
      IF NOT FOUND THEN CONTINUE; END IF;
      qty := 1 + (j % 4);
      p_unit := a_rec."PrecioVenta";
      v_line := round(qty * p_unit, 2);
      v_net := v_line;
      v_iva := 0;
      IF a_rec."AlicuotaIva" = 21 THEN
        v_net := round(v_line / 1.21, 2);
        v_iva := round(v_line - v_net, 2);
        v_iva21 := v_iva21 + v_iva;
      ELSIF a_rec."AlicuotaIva" = 10.5 THEN
        v_net := round(v_line / 1.105, 2);
        v_iva := round(v_line - v_net, 2);
        v_iva105 := v_iva105 + v_iva;
      END IF;
      v_sub := v_sub + v_net;
      v_total := v_total + v_line;

      INSERT INTO "ComprobantesDetalle" (
        "IdComprobante", "IdArticulo", "Descripcion", "Cantidad", "PrecioUnitario", "PrecioUnitarioSinIva",
        "AlicuotaIva", "MontoIva", "PorcentajeDescuento", "MontoDescuento", "SubTotal"
      ) VALUES (
        hdr_id, a_rec."Id", a_rec."Descripcion", qty, p_unit,
        round(p_unit / (1.0 + a_rec."AlicuotaIva" / 100.0), 4),
        a_rec."AlicuotaIva", v_iva, 0, 0, v_line
      );
    END LOOP;

    UPDATE "Comprobantes" SET
      "SubTotal"   = v_sub,
      "Total"      = v_total,
      "TotalIva21" = v_iva21,
      "TotalIva105"= v_iva105,
      "TotalIva0"  = 0
    WHERE "Id" = hdr_id;

    -- Pago efectivo
    INSERT INTO "ComprobantesPago" ("IdComprobante", "IdMedioPago", "Importe", "Vuelto", "Referencia")
    VALUES (hdr_id, 1, v_total, 0, 'DEMO');

    num := num + 1;
  END LOOP;
END $$;

DROP TABLE _demo_prov;

COMMIT;

-- Hecho. Verificá:
-- SELECT count(*) FROM "Proveedores" WHERE "RazonSocial" LIKE 'DEMO-PROV%';
-- SELECT count(*) FROM "Articulos" WHERE "Descripcion" LIKE 'DEMO-ART%';
-- SELECT count(*) FROM "Comprobantes" WHERE "Observaciones" LIKE 'DEMO-SEED%';
-- SELECT count(*) FROM "ListasPrecioProveedor" WHERE "Notas" LIKE 'DEMO-SEED%';
