using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddChequeras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdChequera",
                table: "Cheques",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConsultaSQL = table.Column<string>(type: "text", nullable: false),
                    DiasSemanaAlerta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chequeras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCuentaTesoreria = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Desde = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Hasta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SiguienteNumero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chequeras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chequeras_CuentasTesoreria_IdCuentaTesoreria",
                        column: x => x.IdCuentaTesoreria,
                        principalTable: "CuentasTesoreria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mailing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechaCreado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Destino = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Asunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cuerpo = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<char>(type: "character(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mailing", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertasEnviadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdAlerta = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Md5Registros = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaHoraCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Log = table.Column<string>(type: "text", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasEnviadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasEnviadas_Alertas_IdAlerta",
                        column: x => x.IdAlerta,
                        principalTable: "Alertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertasEnviadas_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertasUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdAlerta = table.Column<int>(type: "integer", nullable: false),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasUsuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasUsuarios_Alertas_IdAlerta",
                        column: x => x.IdAlerta,
                        principalTable: "Alertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertasUsuarios_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cheques_IdChequera",
                table: "Cheques",
                column: "IdChequera");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasEnviadas_IdAlerta",
                table: "AlertasEnviadas",
                column: "IdAlerta");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasEnviadas_IdUsuario",
                table: "AlertasEnviadas",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasUsuarios_IdAlerta_IdUsuario",
                table: "AlertasUsuarios",
                columns: new[] { "IdAlerta", "IdUsuario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertasUsuarios_IdUsuario",
                table: "AlertasUsuarios",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Chequeras_IdCuentaTesoreria",
                table: "Chequeras",
                column: "IdCuentaTesoreria");

            migrationBuilder.AddForeignKey(
                name: "FK_Cheques_Chequeras_IdChequera",
                table: "Cheques",
                column: "IdChequera",
                principalTable: "Chequeras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cheques_Chequeras_IdChequera",
                table: "Cheques");

            migrationBuilder.DropTable(
                name: "AlertasEnviadas");

            migrationBuilder.DropTable(
                name: "AlertasUsuarios");

            migrationBuilder.DropTable(
                name: "Chequeras");

            migrationBuilder.DropTable(
                name: "Mailing");

            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.DropIndex(
                name: "IX_Cheques_IdChequera",
                table: "Cheques");

            migrationBuilder.DropColumn(
                name: "IdChequera",
                table: "Cheques");
        }
    }
}
