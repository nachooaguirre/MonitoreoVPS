using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class DesactivarMediosPagoLegacySinNombre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Placeholders creados por el importador legacy (ver ImportadorLegacyService) para códigos
            // de moneda/plan que no tenían medio de pago mapeado. Nunca se les asignó nombre real y el
            // usuario confirmó que esos planes ya no se usan — se desactivan (no se borran, para no
            // romper la FK de ventas/comprobantes históricos que sí los referencian).
            migrationBuilder.Sql("""
                UPDATE "MediosPago" SET "Activo" = false WHERE "Nombre" ~ '^Medio Pago [0-9]+$';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
