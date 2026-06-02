using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTablasLegacyYPromociones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticulosDatosAdicionales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Campo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Dato = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticulosDatosAdicionales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticulosDatosAdicionales_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonedasPlanes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanNro = table.Column<int>(type: "integer", nullable: false),
                    IdMedioPago = table.Column<int>(type: "integer", nullable: false),
                    Detalle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Recargo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Acumulador = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonedasPlanes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonedasPlanes_MediosPago_IdMedioPago",
                        column: x => x.IdMedioPago,
                        principalTable: "MediosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Promociones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoPromocion = table.Column<int>(type: "integer", nullable: false),
                    TipoAccion = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraInicio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HoraFin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiasSemana = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sucursales = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promociones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromocionesCondiciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdPromocion = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Item = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionesCondiciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocionesCondiciones_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromocionesCondiciones_Promociones_IdPromocion",
                        column: x => x.IdPromocion,
                        principalTable: "Promociones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromocionesParametrosAccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdPromocion = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    Item = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionesParametrosAccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocionesParametrosAccion_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromocionesParametrosAccion_Promociones_IdPromocion",
                        column: x => x.IdPromocion,
                        principalTable: "Promociones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosDatosAdicionales_IdArticulo_Campo",
                table: "ArticulosDatosAdicionales",
                columns: new[] { "IdArticulo", "Campo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonedasPlanes_IdMedioPago",
                table: "MonedasPlanes",
                column: "IdMedioPago");

            migrationBuilder.CreateIndex(
                name: "IX_Promociones_FechaDesde_FechaHasta_Activa",
                table: "Promociones",
                columns: new[] { "FechaDesde", "FechaHasta", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesCondiciones_IdArticulo",
                table: "PromocionesCondiciones",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesCondiciones_IdPromocion",
                table: "PromocionesCondiciones",
                column: "IdPromocion");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesParametrosAccion_IdArticulo",
                table: "PromocionesParametrosAccion",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesParametrosAccion_IdPromocion",
                table: "PromocionesParametrosAccion",
                column: "IdPromocion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticulosDatosAdicionales");

            migrationBuilder.DropTable(
                name: "MonedasPlanes");

            migrationBuilder.DropTable(
                name: "PromocionesCondiciones");

            migrationBuilder.DropTable(
                name: "PromocionesParametrosAccion");

            migrationBuilder.DropTable(
                name: "Promociones");
        }
    }
}
