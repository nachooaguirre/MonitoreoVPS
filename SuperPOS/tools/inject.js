const { Client } = require('pg');
const fs = require('fs');
const path = require('path');

const client = new Client({
  host: 'localhost',
  database: 'superpos',
  user: 'postgres',
  password: 'Chupamela10',
  port: 5432,
});

async function run() {
  console.log('Conectando a PostgreSQL...');
  await client.connect();
  console.log('Conectado. Leyendo seed_real.sql...');
  
  const sqlPath = path.join(__dirname, '..', 'seed_real.sql');
  const sql = fs.readFileSync(sqlPath, 'utf8');
  
  console.log('Inyectando datos en la base de datos (esto puede tardar unos segundos)...');
  await client.query(sql);
  
  console.log('¡Inyección completada con éxito!');
  await client.end();
}

run().catch(err => {
  console.error('Error al inyectar:', err);
  process.exit(1);
});
