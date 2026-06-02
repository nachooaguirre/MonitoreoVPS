using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class UnificarForeignKeysArticuloYVencimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Departamentos_DepartamentoId",
                table: "Articulos");

            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Familias_FamiliaId",
                table: "Articulos");

            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Marcas_MarcaId",
                table: "Articulos");

            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Proveedores_ProveedorId",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_DepartamentoId",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_FamiliaId",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_MarcaId",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_ProveedorId",
                table: "Articulos");

            // Sincronizar claves: la app usaba Id*; EF tenía sombras *Id. Copiar el valor de la sombra si Id* quedó en 0.
            migrationBuilder.Sql("""
                UPDATE "Articulos" SET "IdDepartamento" = "DepartamentoId" WHERE "DepartamentoId" IS NOT NULL AND "IdDepartamento" = 0;
                UPDATE "Articulos" SET "IdFamilia" = "FamiliaId" WHERE "FamiliaId" IS NOT NULL AND "IdFamilia" = 0;
                UPDATE "Articulos" SET "IdMarca" = "MarcaId" WHERE "MarcaId" IS NOT NULL AND "IdMarca" = 0;
                UPDATE "Articulos" SET "IdProveedor" = "ProveedorId" WHERE "ProveedorId" IS NOT NULL AND "IdProveedor" = 0;
                """);

            migrationBuilder.DropColumn(
                name: "DepartamentoId",
                table: "Articulos");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "Articulos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Articulos");

            migrationBuilder.DropColumn(
                name: "ProveedorId",
                table: "Articulos");

            migrationBuilder.AddColumn<DateTime>(
                name: "VencimientoReferencia",
                table: "Articulos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdDepartamento",
                table: "Articulos",
                column: "IdDepartamento");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdFamilia",
                table: "Articulos",
                column: "IdFamilia");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdMarca",
                table: "Articulos",
                column: "IdMarca");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdProveedor",
                table: "Articulos",
                column: "IdProveedor");

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Departamentos_IdDepartamento",
                table: "Articulos",
                column: "IdDepartamento",
                principalTable: "Departamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Familias_IdFamilia",
                table: "Articulos",
                column: "IdFamilia",
                principalTable: "Familias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Marcas_IdMarca",
                table: "Articulos",
                column: "IdMarca",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Proveedores_IdProveedor",
                table: "Articulos",
                column: "IdProveedor",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Departamentos_IdDepartamento",
                table: "Articulos");

            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Familias_IdFamilia",
                table: "Articulos");

            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Marcas_IdMarca",
                table: "Articulos");

            migrationBuilder.DropForeignKey(
                name: "FK_Articulos_Proveedores_IdProveedor",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_IdDepartamento",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_IdFamilia",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_IdMarca",
                table: "Articulos");

            migrationBuilder.DropIndex(
                name: "IX_Articulos_IdProveedor",
                table: "Articulos");

            migrationBuilder.DropColumn(
                name: "VencimientoReferencia",
                table: "Articulos");

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoId",
                table: "Articulos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamiliaId",
                table: "Articulos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "Articulos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProveedorId",
                table: "Articulos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_DepartamentoId",
                table: "Articulos",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_FamiliaId",
                table: "Articulos",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_MarcaId",
                table: "Articulos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_ProveedorId",
                table: "Articulos",
                column: "ProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Departamentos_DepartamentoId",
                table: "Articulos",
                column: "DepartamentoId",
                principalTable: "Departamentos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Familias_FamiliaId",
                table: "Articulos",
                column: "FamiliaId",
                principalTable: "Familias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Marcas_MarcaId",
                table: "Articulos",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Articulos_Proveedores_ProveedorId",
                table: "Articulos",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id");
        }
    }
}
