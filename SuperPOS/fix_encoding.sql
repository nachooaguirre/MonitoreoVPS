SET client_encoding TO 'UTF8';

UPDATE "Articulos" SET "Descripcion" = 'Yerba Mate Taragüi 500g',   "DescripcionCorta" = 'Yerba Taragüi' WHERE "CodigoInterno" = 'A008';
UPDATE "Articulos" SET "Descripcion" = 'Café La Virginia x250g',     "DescripcionCorta" = 'Café Virginia'  WHERE "CodigoInterno" = 'A009';
UPDATE "Articulos" SET "Descripcion" = 'Fideos Spaghetti Matarazzo 500g', "DescripcionCorta" = 'Fideos Matat 500' WHERE "CodigoInterno" = 'A003';
UPDATE "Articulos" SET "Descripcion" = 'Harina 000 Pureza 1kg',      "DescripcionCorta" = 'Harina 000 1kg'  WHERE "CodigoInterno" = 'A004';
UPDATE "Articulos" SET "Descripcion" = 'Azúcar Ledesma 1kg',         "DescripcionCorta" = 'Azúcar 1kg'     WHERE "CodigoInterno" = 'A005';
UPDATE "Articulos" SET "Descripcion" = 'Aceite Girasol Natura 900ml',"DescripcionCorta" = 'Aceite Natura'   WHERE "CodigoInterno" = 'A001';
UPDATE "Articulos" SET "Descripcion" = 'Arroz Largo Fino Gallo 1kg', "DescripcionCorta" = 'Arroz Gallo 1kg' WHERE "CodigoInterno" = 'A002';
UPDATE "Articulos" SET "Descripcion" = 'Leche La Serenísima Entera 1L', "DescripcionCorta" = 'Leche Seren.'  WHERE "CodigoInterno" = 'L001';
UPDATE "Articulos" SET "Descripcion" = 'Queso Cremoso La Serenísima x400g', "DescripcionCorta" = 'Queso Cremos.' WHERE "CodigoInterno" = 'L003';
UPDATE "Articulos" SET "Descripcion" = 'Lavandina Pato Concentrada 1L', "DescripcionCorta" = 'Lavandina Pato' WHERE "CodigoInterno" = 'LI01';
UPDATE "Articulos" SET "Descripcion" = 'Detergente Magistral 500ml', "DescripcionCorta" = 'Deterg. 500ml'  WHERE "CodigoInterno" = 'LI02';
UPDATE "Articulos" SET "Descripcion" = 'Galletitas Oreo Original 117g', "DescripcionCorta" = 'Galletas Oreo' WHERE "CodigoInterno" = 'G001';
UPDATE "Articulos" SET "Descripcion" = 'Chocolate Milka Leche 100g', "DescripcionCorta" = 'Milka 100g'     WHERE "CodigoInterno" = 'K001';
UPDATE "Articulos" SET "Descripcion" = 'Caramelos Halls Menta x10',  "DescripcionCorta" = 'Halls Menta'    WHERE "CodigoInterno" = 'K002';
UPDATE "Articulos" SET "Descripcion" = 'Shampoo Head & Shoulders 400ml', "DescripcionCorta" = 'H&S 400ml'  WHERE "CodigoInterno" = 'P001';
UPDATE "Articulos" SET "Descripcion" = 'Pasta Dental Colgate Triple 90g', "DescripcionCorta" = 'Colgate 90g' WHERE "CodigoInterno" = 'P002';
UPDATE "Articulos" SET "Descripcion" = 'Desodorante Rexona Men 150ml', "DescripcionCorta" = 'Rexona Men'   WHERE "CodigoInterno" = 'P003';
UPDATE "Articulos" SET "Descripcion" = 'Skip Polvo Concentrado 1kg', "DescripcionCorta" = 'Skip Polvo 1kg' WHERE "CodigoInterno" = 'LI03';
UPDATE "Articulos" SET "Descripcion" = 'Cepita Naranja 1L',          "DescripcionCorta" = 'Cepita Naranja' WHERE "CodigoInterno" = 'B005';
UPDATE "Articulos" SET "Descripcion" = 'Papas Fritas Pringles 124g', "DescripcionCorta" = 'Pringles 124g'  WHERE "CodigoInterno" = 'K004';
UPDATE "Articulos" SET "Descripcion" = 'Chizitos Arcor 55g',         "DescripcionCorta" = 'Chizitos 55g'   WHERE "CodigoInterno" = 'K003';

-- Verificar
SELECT "CodigoInterno", "Descripcion" FROM "Articulos" ORDER BY "CodigoInterno" LIMIT 15;
