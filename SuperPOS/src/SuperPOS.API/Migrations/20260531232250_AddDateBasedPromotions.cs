using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDateBasedPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonificacionesFecha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Detalle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Aplicado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonificacionesFecha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonificacionesFecha_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonificacionesFecha_IdArticulo",
                table: "BonificacionesFecha",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_BonificacionesFecha_IdArticulo_FechaDesde_FechaHasta",
                table: "BonificacionesFecha",
                columns: new[] { "IdArticulo", "FechaDesde", "FechaHasta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonificacionesFecha");
        }
    }
}
