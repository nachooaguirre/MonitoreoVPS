using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMediosYTarjetasFaltantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert condicional: una base restaurada desde un backup real de producción puede
            // ya tener filas con estos mismos Id en MediosPago (numeración heredada del sistema
            // legacy) — un InsertData con Id fijo chocaría contra la primary key y abortaría el
            // arranque de la API (Migrate() falla => contenedor en restart loop).
            migrationBuilder.Sql("""
                INSERT INTO "MediosPago" ("Id", "Activo", "CodigoAfip", "Nombre", "RequiereReferencia", "Tipo")
                VALUES
                    (8, true, null, 'Giro', true, 9),
                    (9, true, null, 'Ticket', true, 10),
                    (10, true, null, 'Otros', false, 11)
                ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.InsertData(
                table: "TarjetasMarca",
                columns: new[] { "Id", "Activo", "Codigo", "EsCredito", "Nombre", "PorcentajeRecargo" },
                values: new object[,]
                {
                    { 10, true, "maestro", false, "Maestro", 0m },
                    { 11, true, "otra-cred", true, "Otra (Crédito)", 0m },
                    { 12, true, "otra-deb", false, "Otra (Débito)", 0m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "TarjetasMarca",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "TarjetasMarca",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "TarjetasMarca",
                keyColumn: "Id",
                keyValue: 12);
        }
    }
}
