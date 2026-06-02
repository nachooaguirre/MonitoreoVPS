const { Client } = require('pg');

const client = new Client({
  host: 'localhost',
  database: 'superpos',
  user: 'postgres',
  password: 'Chupamela10',
  port: 5432,
});

async function run() {
  console.log('Conectando a PostgreSQL para simular datos...');
  await client.connect();
  
  const sql = `
    -- 1. Actualizar Stock Minimo y Maximo
    UPDATE "Articulos" 
    SET "StockMinimo" = floor(random() * 15 + 5)::numeric,
        "StockMaximo" = floor(random() * 15 + 5)::numeric + floor(random() * 30 + 10)::numeric;

    -- 2. Crear 500 comprobantes de ventas aleatorios en los ultimos 30 dias
    DO $$
    DECLARE
        i int;
        j int;
        n_items int;
        comp_id bigint;
        art_record RECORD;
        v_cant numeric;
        v_total numeric;
        v_fecha timestamp;
    BEGIN
        -- Insertar un cliente Consumidor Final si no existe
        INSERT INTO "Clientes" ("Id", "RazonSocial", "Cuit", "CondicionIva", "IdListaPrecio", "TieneCtaCte", "LimiteCredito", "SaldoCtaCte", "TipoSaldo", "EsMoroso", "DiasVencimientoCtaCte", "PorcentajeDescuento", "Activo", "FechaAlta")
        VALUES (1, 'Consumidor Final', '00-00000000-0', 5, 1, false, 0, 0, 'H', false, 0, 0, true, NOW())
        ON CONFLICT ("Id") DO NOTHING;

        -- Insertar un tipo de comprobante si no existe
        INSERT INTO "TiposComprobante" ("Id", "Nombre", "Abreviatura", "EsVenta", "EsCompra", "RequiereCAE", "Activo")
        VALUES (1, 'Ticket', 'TK', true, false, false, true)
        ON CONFLICT ("Id") DO NOTHING;

        FOR i IN 1..500 LOOP
            v_fecha := NOW() - (random() * 30 || ' days')::interval;
            
            -- Insert Comprobante
            INSERT INTO "Comprobantes" (
                "IdTipoComprobante", "Letra", "PuntoVenta", "Numero", "Fecha", "IdCliente", "IdCaja", "IdSucursal", "IdUsuario", 
                "SubTotal", "TotalDescuento", "TotalIva21", "TotalIva105", "TotalIva0", "Total", "Estado", "EsFacturaElectronica"
            ) VALUES (
                1, 'B', 1, 10000 + i, v_fecha, 1, 1, 1, 1,
                0, 0, 0, 0, 0, 0, 1, false
            ) RETURNING "Id" INTO comp_id;
            
            v_total := 0;
            n_items := floor(random() * 8 + 2)::int;
            
            FOR j IN 1..n_items LOOP
                -- Seleccionar un articulo al azar
                SELECT * INTO art_record FROM "Articulos" ORDER BY random() LIMIT 1;
                
                v_cant := floor(random() * 5 + 1)::numeric;
                v_total := v_total + (art_record."PrecioVenta" * v_cant);
                
                -- Bajar stock
                UPDATE "Articulos" SET "StockActual" = "StockActual" - v_cant WHERE "Id" = art_record."Id";
                
                INSERT INTO "ComprobantesDetalle" (
                    "IdComprobante", "IdArticulo", "Descripcion", "Cantidad", "PrecioUnitario", "PrecioUnitarioSinIva",
                    "AlicuotaIva", "MontoIva", "PorcentajeDescuento", "MontoDescuento", "SubTotal"
                ) VALUES (
                    comp_id, art_record."Id", art_record."Descripcion", v_cant, art_record."PrecioVenta", 
                    art_record."PrecioVenta" / (1 + (art_record."AlicuotaIva" / 100)),
                    art_record."AlicuotaIva", art_record."PrecioVenta" - (art_record."PrecioVenta" / (1 + (art_record."AlicuotaIva" / 100))),
                    0, 0, art_record."PrecioVenta" * v_cant
                );
            END LOOP;
            
            UPDATE "Comprobantes" SET "Total" = v_total, "SubTotal" = v_total WHERE "Id" = comp_id;
        END LOOP;
    END $$;
  `;
  
  console.log('Ejecutando script de simulación (puede tardar unos segundos)...');
  await client.query(sql);
  
  console.log('¡Listo! Se simularon stock mínimos/máximos y 500 comprobantes de ventas.');
  await client.end();
}

run().catch(err => {
  console.error('Error al simular:', err);
  process.exit(1);
});
