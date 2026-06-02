using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPorSucursalYTransferencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdSucursal",
                table: "Inventarios",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ArticulosStockPorSucursal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    IdSucursal = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticulosStockPorSucursal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticulosStockPorSucursal_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticulosStockPorSucursal_Sucursales_IdSucursal",
                        column: x => x.IdSucursal,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferenciasInternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NroTransferencia = table.Column<int>(type: "integer", nullable: false),
                    IdSucursalOrigen = table.Column<int>(type: "integer", nullable: false),
                    IdSucursalDestino = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferenciasInternas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_Sucursales_IdSucursalDestino",
                        column: x => x.IdSucursalDestino,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternas_Sucursales_IdSucursalOrigen",
                        column: x => x.IdSucursalOrigen,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferenciasInternasDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdTransferencia = table.Column<int>(type: "integer", nullable: false),
                    IdArticulo = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferenciasInternasDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternasDetalle_Articulos_IdArticulo",
                        column: x => x.IdArticulo,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciasInternasDetalle_TransferenciasInternas_IdTran~",
                        column: x => x.IdTransferencia,
                        principalTable: "TransferenciasInternas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Sucursales",
                columns: new[] { "Id", "Activo", "Direccion", "EsCentral", "Nombre" },
                values: new object[] { 2, true, null, false, "Local comercial (ejemplo)" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventarios_IdSucursal",
                table: "Inventarios",
                column: "IdSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosStockPorSucursal_IdArticulo_IdSucursal",
                table: "ArticulosStockPorSucursal",
                columns: new[] { "IdArticulo", "IdSucursal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosStockPorSucursal_IdSucursal",
                table: "ArticulosStockPorSucursal",
                column: "IdSucursal");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_IdSucursalDestino",
                table: "TransferenciasInternas",
                column: "IdSucursalDestino");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_IdSucursalOrigen",
                table: "TransferenciasInternas",
                column: "IdSucursalOrigen");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternas_NroTransferencia",
                table: "TransferenciasInternas",
                column: "NroTransferencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternasDetalle_IdArticulo",
                table: "TransferenciasInternasDetalle",
                column: "IdArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciasInternasDetalle_IdTransferencia",
                table: "TransferenciasInternasDetalle",
                column: "IdTransferencia");

            // Stock inicial: sucursal 1 = suma de StockActual + StockDeposito del artículo (modelo previo monolítico)
            migrationBuilder.Sql("""
                INSERT INTO "ArticulosStockPorSucursal" ("IdArticulo", "IdSucursal", "Cantidad")
                SELECT a."Id", 1, COALESCE(a."StockActual", 0) + COALESCE(a."StockDeposito", 0)
                FROM "Articulos" a
                WHERE NOT EXISTS (
                    SELECT 1 FROM "ArticulosStockPorSucursal" s WHERE s."IdArticulo" = a."Id" AND s."IdSucursal" = 1
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "Articulos" a SET
                  "StockActual" = COALESCE((SELECT SUM(s."Cantidad") FROM "ArticulosStockPorSucursal" s WHERE s."IdArticulo" = a."Id"), 0),
                  "StockDeposito" = COALESCE((
                    SELECT SUM(s."Cantidad") FROM "ArticulosStockPorSucursal" s
                    INNER JOIN "Sucursales" su ON su."Id" = s."IdSucursal" AND su."EsCentral" = true
                    WHERE s."IdArticulo" = a."Id"), 0);
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventarios_Sucursales_IdSucursal",
                table: "Inventarios",
                column: "IdSucursal",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventarios_Sucursales_IdSucursal",
                table: "Inventarios");

            migrationBuilder.DropTable(
                name: "ArticulosStockPorSucursal");

            migrationBuilder.DropTable(
                name: "TransferenciasInternasDetalle");

            migrationBuilder.DropTable(
                name: "TransferenciasInternas");

            migrationBuilder.DropIndex(
                name: "IX_Inventarios_IdSucursal",
                table: "Inventarios");

            migrationBuilder.DeleteData(
                table: "Sucursales",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "IdSucursal",
                table: "Inventarios");
        }
    }
}
