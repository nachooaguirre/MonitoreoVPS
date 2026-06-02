\encoding UTF8
SELECT COUNT(*) FROM "Departamentos" WHERE "Nombre"='Almacén';
SELECT COUNT(*) FROM "Departamentos" WHERE "Nombre" LIKE 'Alm%';
SELECT COUNT(*) FROM "Departamentos" WHERE "Id"=1;
INSERT INTO "Familias" ("Nombre","IdDepartamento","Activo") SELECT 'TEST', "Id", true FROM "Departamentos" WHERE "Id"=1;
SELECT COUNT(*) FROM "Familias";
DELETE FROM "Familias";
