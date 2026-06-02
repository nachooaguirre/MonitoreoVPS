-- ============================================================
-- SEED DE DATOS DE PRUEBA — SuperPOS
-- 60 ventas + 12 órdenes de compra + alertas de vencimiento
-- ============================================================

DO $$
DECLARE
    v_cbte_id   BIGINT;
    v_oc_id     INT;
    v_num       BIGINT := 2;   -- siguiente número de comprobante
    v_oc_num    INT    := 2;   -- siguiente número de OC
    v_fecha     TIMESTAMPTZ;
    v_dias      INT;
    i           INT;
    v_total     NUMERIC;
    v_subtotal  NUMERIC;
    v_iva21     NUMERIC;
    v_iva105    NUMERIC;

    -- Artículos disponibles (Id, PrecioVenta, AlicuotaIva)
    type_art RECORD;

    -- Secuencia de clientes
    clientes INT[] := ARRAY[1,3,4,5,6,7,1,3,5,1];

    -- Artículos con IVA 21%
    arts_21 INT[]  := ARRAY[19,20,21,22,23,28,29,30,31,32,33];
    -- Artículos con IVA 10.5%
    arts_105 INT[] := ARRAY[10,11,12,13,14,15,16,17,18,24,25,26,27,34,35,36,37,38];

    -- Precios de venta
    precio_art INT[] := ARRAY[
        2320,1390,890,1140,1520,610,1090,2840,2640,1860,
        1080,2260,2010,7500,1450,1020,2920,3750,1750,1080,
        4420,5050,2610,3330,1670,1560,570,2620,1090
    ];
    -- Mapea Id de artículo → precio (uso directo de BD)

    v_art_id    INT;
    v_cant      NUMERIC;
    v_precio    NUMERIC;
    v_iva_pct   NUMERIC;
    v_monto_iva NUMERIC;
    v_subtot_d  NUMERIC;
    j           INT;
    num_items   INT;

BEGIN

-- ════════════════════════════════════════════════════════════
-- 1. VENTAS (Comprobantes + Detalles)
-- ════════════════════════════════════════════════════════════

FOR i IN 1..65 LOOP

    -- Fecha aleatoria en los últimos 90 días
    v_dias  := (random() * 89)::INT;
    v_fecha := NOW() - (v_dias || ' days')::INTERVAL
             - (random() * 28800 || ' seconds')::INTERVAL;

    v_subtotal := 0;
    v_iva21    := 0;
    v_iva105   := 0;

    -- Insertar cabecera comprobante
    INSERT INTO "Comprobantes"(
        "IdTipoComprobante","Letra","PuntoVenta","Numero","Fecha",
        "IdCliente","IdCaja","IdSucursal","IdUsuario",
        "SubTotal","TotalDescuento","TotalIva21","TotalIva105","TotalIva0","Total",
        "Estado","EsFacturaElectronica"
    ) VALUES (
        2,'B',1, v_num, v_fecha,
        clientes[((i-1) % array_length(clientes,1)) + 1],
        1,1,1,
        0,0,0,0,0,0,
        1, false
    ) RETURNING "Id" INTO v_cbte_id;

    -- 2 a 5 ítems por venta
    num_items := 2 + (random()*3)::INT;

    FOR j IN 1..num_items LOOP
        -- Elegir artículo al azar entre los disponibles
        IF random() < 0.55 THEN
            v_art_id := arts_105[ ((j + i*3) % array_length(arts_105,1)) + 1 ];
            v_iva_pct := 10.5;
        ELSE
            v_art_id := arts_21[ ((j + i*2) % array_length(arts_21,1)) + 1 ];
            v_iva_pct := 21;
        END IF;

        -- Precio real del artículo
        SELECT "PrecioVenta" INTO v_precio FROM "Articulos" WHERE "Id" = v_art_id;
        v_cant     := 1 + (random()*4)::INT;
        v_subtot_d := ROUND(v_precio * v_cant * 100 / (1 + v_iva_pct/100), 0) / 100;
        v_monto_iva:= ROUND(v_precio * v_cant - v_subtot_d, 2);

        INSERT INTO "ComprobantesDetalle"(
            "IdComprobante","IdArticulo","Descripcion",
            "Cantidad","PrecioUnitario","PrecioUnitarioSinIva",
            "AlicuotaIva","MontoIva","PorcentajeDescuento","MontoDescuento","SubTotal"
        ) VALUES (
            v_cbte_id, v_art_id,
            (SELECT "Descripcion" FROM "Articulos" WHERE "Id"=v_art_id),
            v_cant, v_precio, ROUND(v_precio/(1+v_iva_pct/100),2),
            v_iva_pct, v_monto_iva, 0, 0, ROUND(v_precio*v_cant,2)
        );

        v_subtotal := v_subtotal + v_subtot_d;
        IF v_iva_pct = 21 THEN
            v_iva21 := v_iva21 + v_monto_iva;
        ELSE
            v_iva105 := v_iva105 + v_monto_iva;
        END IF;
    END LOOP;

    v_total := ROUND(v_subtotal + v_iva21 + v_iva105, 2);

    -- Actualizar totales
    UPDATE "Comprobantes" SET
        "SubTotal"    = ROUND(v_subtotal,2),
        "TotalIva21"  = ROUND(v_iva21,2),
        "TotalIva105" = ROUND(v_iva105,2),
        "Total"       = v_total
    WHERE "Id" = v_cbte_id;

    -- Insertar pago en efectivo
    INSERT INTO "ComprobantesPago"("IdComprobante","IdMedioPago","Importe","Vuelto")
    VALUES (v_cbte_id, 1, v_total, 0);

    -- Actualizar CantidadVendida y UltimaVenta en artículos
    UPDATE "Articulos" a SET
        "CantidadVendida" = "CantidadVendida" + d."Cantidad",
        "UltimaVenta"     = v_fecha
    FROM "ComprobantesDetalle" d
    WHERE d."IdComprobante" = v_cbte_id AND d."IdArticulo" = a."Id";

    v_num := v_num + 1;
END LOOP;


-- ════════════════════════════════════════════════════════════
-- 2. ÓRDENES DE COMPRA (OrdenesCompra + Detalles)
-- ════════════════════════════════════════════════════════════

FOR i IN 1..12 LOOP
    v_fecha := NOW() - ((i * 7) || ' days')::INTERVAL;
    v_total  := 0;
    v_iva21  := 0;
    v_iva105 := 0;
    v_subtotal := 0;

    INSERT INTO "OrdenesCompra"(
        "IdProveedor","IdUsuario","NroOrden","Fecha","FechaEntregaEsperada",
        "Estado","TotalSinIva","TotalIva","Total","Observaciones"
    ) VALUES (
        CASE WHEN i % 2 = 0 THEN 3 ELSE 4 END,
        1, v_oc_num, v_fecha,
        v_fecha + '7 days'::INTERVAL,
        CASE WHEN i <= 8 THEN 2 ELSE 0 END,  -- 2=Recibida, 0=Pendiente
        0, 0, 0,
        'Orden generada para pruebas'
    ) RETURNING "Id" INTO v_oc_id;

    -- 3 a 6 artículos por OC
    num_items := 3 + (random()*3)::INT;
    FOR j IN 1..num_items LOOP
        IF random() < 0.6 THEN
            v_art_id := arts_105[((j+i) % array_length(arts_105,1)) + 1];
            v_iva_pct := 10.5;
        ELSE
            v_art_id := arts_21[((j+i*2) % array_length(arts_21,1)) + 1];
            v_iva_pct := 21;
        END IF;

        SELECT "PrecioCosto" INTO v_precio FROM "Articulos" WHERE "Id" = v_art_id;
        v_cant     := 10 + (random()*30)::INT;
        v_subtot_d := ROUND(v_precio * v_cant, 2);
        v_monto_iva:= ROUND(v_subtot_d * v_iva_pct / 100, 2);

        INSERT INTO "OrdenesCompraDetalle"(
            "IdOrdenCompra","IdArticulo",
            "CantidadPedida","PrecioCosto",
            "AlicuotaIva","Subtotal","CantidadRecibida"
        ) VALUES (
            v_oc_id, v_art_id,
            v_cant, v_precio,
            v_iva_pct, v_subtot_d,
            CASE WHEN i <= 8 THEN v_cant ELSE 0 END
        );

        v_subtotal := v_subtotal + v_subtot_d;
        v_iva21    := v_iva21    + CASE WHEN v_iva_pct = 21 THEN v_monto_iva ELSE 0 END;
        v_iva105   := v_iva105   + CASE WHEN v_iva_pct = 10.5 THEN v_monto_iva ELSE 0 END;
    END LOOP;

    v_total := ROUND(v_subtotal + v_iva21 + v_iva105, 2);
    UPDATE "OrdenesCompra" SET
        "TotalSinIva" = ROUND(v_subtotal,2),
        "TotalIva"    = ROUND(v_iva21 + v_iva105, 2),
        "Total"       = v_total
    WHERE "Id" = v_oc_id;

    v_oc_num := v_oc_num + 1;
END LOOP;


-- ════════════════════════════════════════════════════════════
-- 3. ALERTAS DE VENCIMIENTO (TrazabilidadEventos con FechaVencimiento)
-- ════════════════════════════════════════════════════════════

-- Artículos con vencimiento (lácteos, snacks, bebidas)
-- Críticos: vencen en < 7 días
INSERT INTO "TrazabilidadEventos"("Fecha","IdArticulo","Cantidad","Tipo","Ubicacion","IdUsuario","LoteNro","FechaVencimiento","Observaciones")
VALUES
    (NOW()-'5 days'::INTERVAL, 24, 36, 6, 'Depósito', 1, 'LOT-2024-001', NOW()+'3 days'::INTERVAL, 'Leche vence pronto'),
    (NOW()-'3 days'::INTERVAL, 25, 24, 6, 'Heladera A', 1, 'LOT-2024-002', NOW()+'2 days'::INTERVAL, 'Yogur crítico'),
    (NOW()-'4 days'::INTERVAL, 26, 12, 6, 'Heladera B', 1, 'LOT-2024-003', NOW()+'5 days'::INTERVAL, 'Queso próximo'),
    (NOW()-'2 days'::INTERVAL, 27, 18, 6, 'Heladera A', 1, 'LOT-2024-004', NOW()+'4 days'::INTERVAL, 'Manteca stock bajo'),

-- Alta: vencen entre 8 y 15 días
    (NOW()-'10 days'::INTERVAL, 24, 72, 6, 'Depósito', 1, 'LOT-2024-005', NOW()+'10 days'::INTERVAL, NULL),
    (NOW()-'8 days'::INTERVAL, 16, 48, 6, 'Depósito', 1, 'LOT-2024-006', NOW()+'12 days'::INTERVAL, NULL),
    (NOW()-'6 days'::INTERVAL, 15, 60, 6, 'Góndola', 1, 'LOT-2024-007', NOW()+'14 days'::INTERVAL, NULL),
    (NOW()-'7 days'::INTERVAL, 11, 96, 6, 'Depósito', 1, 'LOT-2024-008', NOW()+'11 days'::INTERVAL, NULL),

-- Normal: vencen entre 15 y 30 días
    (NOW()-'15 days'::INTERVAL, 20, 24, 6, 'Depósito', 1, 'LOT-2024-009', NOW()+'20 days'::INTERVAL, NULL),
    (NOW()-'12 days'::INTERVAL, 22, 12, 6, 'Depósito', 1, 'LOT-2024-010', NOW()+'25 days'::INTERVAL, NULL),
    (NOW()-'20 days'::INTERVAL, 17, 36, 6, 'Depósito', 1, 'LOT-2024-011', NOW()+'28 days'::INTERVAL, NULL),
    (NOW()-'18 days'::INTERVAL, 23, 48, 6, 'Depósito', 1, 'LOT-2024-012', NOW()+'22 days'::INTERVAL, NULL);


-- ════════════════════════════════════════════════════════════
-- 4. BAJAR STOCK de varios artículos para que la IA sugiera compras
-- ════════════════════════════════════════════════════════════

-- Dejar stock muy bajo (menor que StockMinimo) en artículos clave
UPDATE "Articulos" SET "StockActual" = 3,  "StockDeposito" = 0 WHERE "Id" = 24;  -- Leche
UPDATE "Articulos" SET "StockActual" = 2,  "StockDeposito" = 0 WHERE "Id" = 20;  -- Coca-Cola
UPDATE "Articulos" SET "StockActual" = 0,  "StockDeposito" = 0 WHERE "Id" = 25;  -- Yogur
UPDATE "Articulos" SET "StockActual" = 1,  "StockDeposito" = 0 WHERE "Id" = 12;  -- Fideos
UPDATE "Articulos" SET "StockActual" = 5,  "StockDeposito" = 0 WHERE "Id" = 14;  -- Azúcar
UPDATE "Articulos" SET "StockActual" = 4,  "StockDeposito" = 2 WHERE "Id" = 10;  -- Aceite
UPDATE "Articulos" SET "StockActual" = 3,  "StockDeposito" = 0 WHERE "Id" = 17;  -- Yerba
UPDATE "Articulos" SET "StockActual" = 2,  "StockDeposito" = 0 WHERE "Id" = 22;  -- Quilmes
UPDATE "Articulos" SET "StockActual" = 6,  "StockDeposito" = 3 WHERE "Id" = 27;  -- Manteca
UPDATE "Articulos" SET "StockActual" = 4,  "StockDeposito" = 0 WHERE "Id" = 32;  -- Pasta dental

-- Sincronizar ArticulosStockPorSucursal con los nuevos valores
UPDATE "ArticulosStockPorSucursal" SET "Cantidad" = a."StockActual"
FROM "Articulos" a
WHERE "ArticulosStockPorSucursal"."IdArticulo" = a."Id"
  AND "ArticulosStockPorSucursal"."IdSucursal" = 1;

RAISE NOTICE '✅ Datos de prueba generados correctamente:';
RAISE NOTICE '   - 65 ventas en los últimos 90 días';
RAISE NOTICE '   - 12 órdenes de compra';
RAISE NOTICE '   - 12 lotes con fechas de vencimiento';
RAISE NOTICE '   - 10 artículos con stock bajo para sugerencias IA';

END $$;
