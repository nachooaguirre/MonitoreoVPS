using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class v3_PerfilesPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PuedeVerReportes",
                table: "Perfiles",
                newName: "PuedeVerCostos");

            migrationBuilder.RenameColumn(
                name: "PuedeVerPrecios",
                table: "Perfiles",
                newName: "PuedeModificarStock");

            migrationBuilder.RenameColumn(
                name: "PuedeGestionarStock",
                table: "Perfiles",
                newName: "PuedeCerrarCaja");

            migrationBuilder.RenameColumn(
                name: "PuedeGestionarCompras",
                table: "Perfiles",
                newName: "PuedeCambiarPrecios");

            migrationBuilder.RenameColumn(
                name: "PuedeGestionarClientes",
                table: "Perfiles",
                newName: "PuedeAnularVentas");

            migrationBuilder.RenameColumn(
                name: "PuedeAnular",
                table: "Perfiles",
                newName: "AccesoUsuarios");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "Usuarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Perfiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaximoDescuento",
                table: "Perfiles",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<bool>(
                name: "AccesoArticulos",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoCaja",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoClientes",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoCompras",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoConfiguracion",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoCtaCte",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoProveedores",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoReportes",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoStock",
                table: "Perfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Perfiles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AccesoArticulos", "AccesoCaja", "AccesoClientes", "AccesoCompras", "AccesoConfiguracion", "AccesoCtaCte", "AccesoProveedores", "AccesoReportes", "AccesoStock" },
                values: new object[] { true, true, true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Perfiles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AccesoArticulos", "AccesoCaja", "AccesoClientes", "AccesoCompras", "AccesoConfiguracion", "AccesoCtaCte", "AccesoProveedores", "AccesoReportes", "AccesoStock", "PuedeModificarStock" },
                values: new object[] { false, true, true, false, false, false, false, false, false, false });

            migrationBuilder.InsertData(
                table: "Perfiles",
                columns: new[] { "Id", "AccesoArticulos", "AccesoCaja", "AccesoClientes", "AccesoCompras", "AccesoConfiguracion", "AccesoCtaCte", "AccesoProveedores", "AccesoReportes", "AccesoStock", "AccesoUsuarios", "EsAdministrador", "MaximoDescuento", "Nombre", "PuedeAbrirCaja", "PuedeAnularVentas", "PuedeCambiarPrecios", "PuedeCerrarCaja", "PuedeHacerDescuentos", "PuedeModificarStock", "PuedeVender", "PuedeVerCostos" },
                values: new object[,]
                {
                    { 3, true, true, false, true, false, false, false, false, true, false, false, 0m, "Repositor", true, false, false, false, false, true, true, true },
                    { 4, true, true, true, true, false, true, true, true, true, false, false, 30m, "Supervisor", true, true, false, true, true, true, true, true }
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "PasswordHash", "Telefono" },
                values: new object[] { null, "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Perfiles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Perfiles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AccesoArticulos",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoCaja",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoClientes",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoCompras",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoConfiguracion",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoCtaCte",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoProveedores",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoReportes",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AccesoStock",
                table: "Perfiles");

            migrationBuilder.RenameColumn(
                name: "PuedeVerCostos",
                table: "Perfiles",
                newName: "PuedeVerReportes");

            migrationBuilder.RenameColumn(
                name: "PuedeModificarStock",
                table: "Perfiles",
                newName: "PuedeVerPrecios");

            migrationBuilder.RenameColumn(
                name: "PuedeCerrarCaja",
                table: "Perfiles",
                newName: "PuedeGestionarStock");

            migrationBuilder.RenameColumn(
                name: "PuedeCambiarPrecios",
                table: "Perfiles",
                newName: "PuedeGestionarCompras");

            migrationBuilder.RenameColumn(
                name: "PuedeAnularVentas",
                table: "Perfiles",
                newName: "PuedeGestionarClientes");

            migrationBuilder.RenameColumn(
                name: "AccesoUsuarios",
                table: "Perfiles",
                newName: "PuedeAnular");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "Usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Perfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaximoDescuento",
                table: "Perfiles",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.UpdateData(
                table: "Perfiles",
                keyColumn: "Id",
                keyValue: 2,
                column: "PuedeVerPrecios",
                value: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "admin");
        }
    }
}
