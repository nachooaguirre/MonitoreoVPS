using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromocionesSchemaParaMdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AplicaSobre",
                table: "PromocionesParametrosAccion",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrefiereMenorValor",
                table: "PromocionesParametrosAccion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Repeticiones",
                table: "PromocionesParametrosAccion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoValor",
                table: "PromocionesParametrosAccion",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Valor",
                table: "PromocionesParametrosAccion",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Excluye",
                table: "PromocionesCondiciones",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoValor",
                table: "PromocionesCondiciones",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDesde",
                table: "PromocionesCondiciones",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorHasta",
                table: "PromocionesCondiciones",
                type: "numeric(18,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AplicaSobre",
                table: "PromocionesParametrosAccion");

            migrationBuilder.DropColumn(
                name: "PrefiereMenorValor",
                table: "PromocionesParametrosAccion");

            migrationBuilder.DropColumn(
                name: "Repeticiones",
                table: "PromocionesParametrosAccion");

            migrationBuilder.DropColumn(
                name: "TipoValor",
                table: "PromocionesParametrosAccion");

            migrationBuilder.DropColumn(
                name: "Valor",
                table: "PromocionesParametrosAccion");

            migrationBuilder.DropColumn(
                name: "Excluye",
                table: "PromocionesCondiciones");

            migrationBuilder.DropColumn(
                name: "TipoValor",
                table: "PromocionesCondiciones");

            migrationBuilder.DropColumn(
                name: "ValorDesde",
                table: "PromocionesCondiciones");

            migrationBuilder.DropColumn(
                name: "ValorHasta",
                table: "PromocionesCondiciones");
        }
    }
}
