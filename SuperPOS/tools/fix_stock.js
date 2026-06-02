const { Client } = require('pg');

const client = new Client({
  host: 'localhost',
  database: 'superpos',
  user: 'postgres',
  password: 'Chupamela10',
  port: 5432,
});

async function run() {
  console.log('Conectando a PostgreSQL para corregir stock...');
  await client.connect();
  
  // Actualizar StockActual a un valor aleatorio entre 1 y 100 para los negativos
  const sql = `
    UPDATE "Articulos" 
    SET "StockActual" = floor(random() * 100 + 1)::numeric 
    WHERE "StockActual" < 0;
  `;
  
  console.log('Ejecutando script de corrección...');
  const res = await client.query(sql);
  
  console.log(`¡Listo! Se corrigió el stock de ${res.rowCount} artículos que estaban en negativo.`);
  await client.end();
}

run().catch(err => {
  console.error('Error al corregir el stock:', err);
  process.exit(1);
});
