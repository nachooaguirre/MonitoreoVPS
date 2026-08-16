using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobanteAfipLogAndComisionProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Comision",
                table: "Comprobantes",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IdProveedor",
                table: "Comprobantes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComprobantesAfipLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdComprobante = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resultado = table.Column<char>(type: "character(1)", nullable: false),
                    Detalle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestXml = table.Column<string>(type: "text", nullable: true),
                    ResponseXml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobantesAfipLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobantesAfipLog_Comprobantes_IdComprobante",
                        column: x => x.IdComprobante,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdProveedor",
                table: "Comprobantes",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesAfipLog_IdComprobante",
                table: "ComprobantesAfipLog",
                column: "IdComprobante");

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_Proveedores_IdProveedor",
                table: "Comprobantes",
                column: "IdProveedor",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_Proveedores_IdProveedor",
                table: "Comprobantes");

            migrationBuilder.DropTable(
                name: "ComprobantesAfipLog");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_IdProveedor",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "Comision",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "IdProveedor",
                table: "Comprobantes");
        }
    }
}
