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
  const res = await client.query('SELECT "Id", "RazonSocial" FROM "Proveedores"');
  console.log("Proveedores:", res.rows);
  const res2 = await client.query('SELECT "Id", "Descripcion", "IdProveedor" FROM "Articulos" WHERE "Descripcion" ILIKE \'%Coca%\' OR "Descripcion" ILIKE \'%Pepsi%\' LIMIT 5');
  console.log("Articulos:", res2.rows);
  
  // Remove old providers (1 and 2 if they are empty? No, old providers are 1 and 2, but wait!
  // In `update_db.js`, I UPDATED provider 1 and 2!
  // UPDATE "Proveedores" SET "RazonSocial" = 'Distribuidora Coca-Cola' WHERE "Id" = 1;
  // UPDATE "Proveedores" SET "RazonSocial" = 'Distribuidora Pepsico' WHERE "Id" = 2;
  
  // Wait, the user said "elimina los proveedores anteriores porque no tienen ningun producto asignado".
  // Which providers are they talking about? Let's check which ones have 0 products.
  
  const res3 = await client.query('SELECT p."Id", p."RazonSocial", COUNT(a."Id") as "ProdCount" FROM "Proveedores" p LEFT JOIN "Articulos" a ON p."Id" = a."IdProveedor" GROUP BY p."Id", p."RazonSocial" ORDER BY "ProdCount" ASC');
  console.log("Prod Counts:", res3.rows);

  await client.end();
}
main().catch(console.error);
