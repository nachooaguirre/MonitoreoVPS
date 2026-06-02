const fs = require('fs');
const path = require('path');

const inputPath = path.join(__dirname, '..', 'estadistico de ventas 1-3 al 26-4.xls');
const outputPath = path.join(__dirname, '..', 'seed_real.sql');

console.log('Leyendo archivo...', inputPath);
const rawData = fs.readFileSync(inputPath, 'latin1');
const lines = rawData.split('\n').map(l => l.trim()).filter(l => l.length > 0);

// asumiendo linea 0 es título, linea 1 son cabeceras, o podría estar en la línea 2.
let startRow = 0;
while (startRow < lines.length && !lines[startRow].startsWith('Codigo\tEAN')) {
    startRow++;
}
if (startRow >= lines.length) startRow = 1;

// Map
const rubros = new Map(); // name -> id
const familias = new Map(); // name -> {id, idDepto}
const marcas = new Map(); // name -> id
const proveedores = new Map(); // name -> id
const articulos = [];

let deptoIdSeq = 1;
let familiaIdSeq = 1;
let marcaIdSeq = 1;
let provIdSeq = 1;

for (let i = startRow + 1; i < lines.length; i++) {
    const cols = lines[i].split('\t').map(c => c.trim());
    if (cols.length < 25) continue;
    
    // Extracción de datos
    const codigo = cols[0];
    let ean = cols[1];
    const descripcion = cols[2];
    const uxb = parseInt(cols[3], 10) || 1;
    const margen = parseFloat(cols[4].replace(',','.')) || 30; // Mrgn T.
    const stkSuc = parseFloat(cols[12].replace(',','.')) || 0; // Stk. Suc.
    const marcaStr = cols[18];
    const costo = parseFloat(cols[20].replace(',','.')) || 0; // Costo U.Act
    const venta = parseFloat(cols[22].replace(',','.')) || 0; // Venta U.Act
    const provStr = cols[24]; // e.g. "[00347] DEBI PLAST S.A."
    const familiaStr = cols[25]; 
    const rubroStr = cols[26];
    
    if (!ean) ean = codigo;
    if (!ean) continue;
    
    const getCleanName = (str) => {
        if (!str) return "VARIOS";
        return str.replace(/^\[\d+\]\s*/, '').replace(/'/g, "''").trim() || "VARIOS";
    };
    
    const rubroName = getCleanName(rubroStr);
    const familiaName = getCleanName(familiaStr);
    const marcaName = getCleanName(marcaStr);
    const provName = getCleanName(provStr);
    
    if (!rubros.has(rubroName)) rubros.set(rubroName, deptoIdSeq++);
    const idDepto = rubros.get(rubroName);
    
    const famKey = `${idDepto}-${familiaName}`;
    if (!familias.has(famKey)) familias.set(famKey, { id: familiaIdSeq++, idDepto });
    const idFam = familias.get(famKey).id;
    
    if (!marcas.has(marcaName)) marcas.set(marcaName, marcaIdSeq++);
    const idMarca = marcas.get(marcaName);
    
    if (!proveedores.has(provName)) proveedores.set(provName, provIdSeq++);
    const idProv = proveedores.get(provName);
    
    articulos.push({
        codigo, ean, descripcion: descripcion.replace(/'/g, "''"), uxb, margen, stkSuc,
        costo, venta, idDepto, idFam, idMarca, idProv
    });
}

let sql = `-- ================================================================
-- SuperPOS - Script de carga real
-- Generado automáticamente desde excel
-- ================================================================

TRUNCATE TABLE "ComprobantesDetalle" CASCADE;
TRUNCATE TABLE "ComprobantesPago" CASCADE;
TRUNCATE TABLE "Comprobantes" CASCADE;
TRUNCATE TABLE "ArticulosCodigoBarras" CASCADE;
TRUNCATE TABLE "Articulos" CASCADE;
TRUNCATE TABLE "Familias" CASCADE;
TRUNCATE TABLE "Departamentos" CASCADE;
TRUNCATE TABLE "Marcas" CASCADE;
TRUNCATE TABLE "Proveedores" CASCADE;

ALTER SEQUENCE "Departamentos_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Familias_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Marcas_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Proveedores_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "Articulos_Id_seq" RESTART WITH 1;

`;

sql += `INSERT INTO "Departamentos" ("Id", "Nombre","Activo") VALUES\n`;
const deptoVals = [];
for (const [name, id] of rubros.entries()) { deptoVals.push(`(${id}, '${name}', true)`); }
sql += deptoVals.join(',\n') + `;\n\n`;

sql += `INSERT INTO "Familias" ("Id", "Nombre","IdDepartamento","Activo") VALUES\n`;
const famVals = [];
for (const [name, data] of familias.entries()) {
    const famName = name.split('-').slice(1).join('-');
    famVals.push(`(${data.id}, '${famName}', ${data.idDepto}, true)`);
}
sql += famVals.join(',\n') + `;\n\n`;

if (marcas.size > 0) {
    sql += `INSERT INTO "Marcas" ("Id", "Nombre","Activo") VALUES\n`;
    const marcaVals = [];
    for (const [name, id] of marcas.entries()) { marcaVals.push(`(${id}, '${name}', true)`); }
    sql += marcaVals.join(',\n') + `;\n\n`;
}

if (proveedores.size > 0) {
    sql += `INSERT INTO "Proveedores" ("Id", "RazonSocial","Cuit","CondicionIva","Telefono","Email","DiasEntrega","DiasVencimientoPago","SaldoCtaCte","Activo","FechaAlta") VALUES\n`;
    const provVals = [];
    for (const [name, id] of proveedores.entries()) {
        provVals.push(`(${id}, '${name}', '00-00000000-0', 1, '', '', 0, 0, 0, true, NOW())`);
    }
    sql += provVals.join(',\n') + `;\n\n`;
}

const artVals = [];
for (const art of articulos) {
    let alicuota = 21;
    let esPesable = (art.uxb !== 1) ? 'true' : 'false';
    artVals.push(`('${art.ean}', '${art.codigo}', '', '${art.descripcion}', '', ${art.idDepto}, ${art.idFam}, ${art.idMarca}, ${art.idProv}, ${art.costo}, 0, 0, 0, 0, 0, 0, 0, ${alicuota}, 0, ${art.margen}, ${art.venta}, ${art.uxb}, 1, ${art.stkSuc}, 0, 100, 0, true, true, false, false, false, NOW(), ${esPesable}, 0, 0)`);
}

const chunkSize = 500;
for (let i = 0; i < artVals.length; i += chunkSize) {
    const chunk = artVals.slice(i, i + chunkSize);
    sql += `INSERT INTO "Articulos" ("CodigoBarras","CodigoInterno","CodigoProveedor","Descripcion","DescripcionCorta","IdDepartamento","IdFamilia","IdMarca","IdProveedor","PrecioCosto","PrecioOferta","Bonificacion1","Bonificacion2","Bonificacion3","Bonificacion4","Bonificacion5","Recargo1","AlicuotaIva","ImpuestoInterno","MargenGanancia","PrecioVenta","UnidadesPorBulto","CajasPorBulto","StockActual","StockMinimo","StockMaximo","StockDeposito","AplicaIva","Activo","RequiereNroSerie","RequiereNroLote","RequiereFechaVencimiento","FechaAlta","EsPesable","BanderaEAN","CantidadVendida") VALUES\n`;
    sql += chunk.join(',\n') + `;\n\n`;
}

// Actualizar secuencias
sql += `
SELECT setval('"Departamentos_Id_seq"', COALESCE((SELECT MAX("Id")+1 FROM "Departamentos"), 1), false);
SELECT setval('"Familias_Id_seq"', COALESCE((SELECT MAX("Id")+1 FROM "Familias"), 1), false);
SELECT setval('"Marcas_Id_seq"', COALESCE((SELECT MAX("Id")+1 FROM "Marcas"), 1), false);
SELECT setval('"Proveedores_Id_seq"', COALESCE((SELECT MAX("Id")+1 FROM "Proveedores"), 1), false);
SELECT setval('"Articulos_Id_seq"', COALESCE((SELECT MAX("Id")+1 FROM "Articulos"), 1), false);
`;

fs.writeFileSync(outputPath, sql, 'utf8');
console.log('Script generado exitosamente: seed_real.sql con ' + articulos.length + ' artículos.');
