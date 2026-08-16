using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAfipFieldsToCompra : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CuitCorredor",
                table: "Compras",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DespachoImportacion",
                table: "Compras",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTipoComprobante",
                table: "Compras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ImporteExento",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImporteNoGravado",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImpuestosInternos",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IvaComision",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NombreCorredor",
                table: "Compras",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercepcionIIBB",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercepcionIva",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercepcionMunicipal",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercepcionNacional",
                table: "Compras",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PuntoVentaProveedor",
                table: "Compras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_IdTipoComprobante",
                table: "Compras",
                column: "IdTipoComprobante");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_TiposComprobante_IdTipoComprobante",
                table: "Compras",
                column: "IdTipoComprobante",
                principalTable: "TiposComprobante",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_TiposComprobante_IdTipoComprobante",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_IdTipoComprobante",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "CuitCorredor",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "DespachoImportacion",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "IdTipoComprobante",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ImporteExento",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ImporteNoGravado",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ImpuestosInternos",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "IvaComision",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "NombreCorredor",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PercepcionIIBB",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PercepcionIva",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PercepcionMunicipal",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PercepcionNacional",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PuntoVentaProveedor",
                table: "Compras");
        }
    }
}
