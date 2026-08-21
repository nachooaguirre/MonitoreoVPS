using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCtaDni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert condicional: ver nota en la migración AddMediosYTarjetasFaltantes sobre por qué
            // un InsertData con Id fijo puede chocar contra datos heredados de un backup real.
            migrationBuilder.Sql("""
                INSERT INTO "MediosPago" ("Id", "Activo", "CodigoAfip", "Nombre", "RequiereReferencia", "Tipo")
                VALUES (11, true, null, 'Cuenta DNI', true, 12)
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
