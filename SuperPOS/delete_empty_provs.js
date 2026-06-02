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
  // Delete old providers if they are not referenced
  try {
    const res = await client.query('DELETE FROM "Proveedores" WHERE "Id" NOT IN (SELECT DISTINCT "IdProveedor" FROM "Articulos" WHERE "IdProveedor" IS NOT NULL)');
    console.log("Deleted old providers:", res.rowCount);
  } catch(e) {
    console.log("Could not delete providers (might be referenced by other tables). Updating them to INACTIVO instead...");
    await client.query('UPDATE "Proveedores" SET "Activo" = false WHERE "Id" NOT IN (SELECT DISTINCT "IdProveedor" FROM "Articulos" WHERE "IdProveedor" IS NOT NULL)');
  }
  
  const resCoca = await client.query('UPDATE "Articulos" SET "IdProveedor" = 1 WHERE "Descripcion" ILIKE \'%coca%\' OR "Descripcion" ILIKE \'%sprite%\' OR "Descripcion" ILIKE \'%fanta%\'');
  console.log("Updated Coca-Cola items to Prov 1:", resCoca.rowCount);
  
  const resPepsi = await client.query('UPDATE "Articulos" SET "IdProveedor" = 2 WHERE "Descripcion" ILIKE \'%pepsi%\' OR "Descripcion" ILIKE \'%7up%\'');
  console.log("Updated Pepsi items to Prov 2:", resPepsi.rowCount);
  
  await client.end();
}
main().catch(console.error);
