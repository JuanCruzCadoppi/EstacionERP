using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EstacionERP.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoDocumento = table.Column<int>(type: "integer", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreFantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CondicionIva = table.Column<int>(type: "integer", nullable: false),
                    Domicilio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Localidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TieneCuentaCorriente = table.Column<bool>(type: "boolean", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "unidades_negocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidades_negocio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Patente = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehiculos_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CodigoFabricante = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Marca = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Rubro = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnidadNegocioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    UnidadMedida = table.Column<int>(type: "integer", nullable: false),
                    AlicuotaIva = table.Column<int>(type: "integer", nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ControlaStock = table.Column<bool>(type: "boolean", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_productos_unidades_negocio_UnidadNegocioId",
                        column: x => x.UnidadNegocioId,
                        principalTable: "unidades_negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "puntos_venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    UnidadNegocioId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puntos_venta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_puntos_venta_unidades_negocio_UnidadNegocioId",
                        column: x => x.UnidadNegocioId,
                        principalTable: "unidades_negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "clientes",
                columns: new[] { "Id", "Activo", "CondicionIva", "CreadoEn", "Domicilio", "Email", "LimiteCredito", "Localidad", "ModificadoEn", "NombreFantasia", "NumeroDocumento", "Observaciones", "RazonSocial", "Telefono", "TieneCuentaCorriente", "TipoDocumento" },
                values: new object[] { 1, true, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0m, null, null, null, "0", null, "CONSUMIDOR FINAL", null, false, 99 });

            migrationBuilder.InsertData(
                table: "unidades_negocio",
                columns: new[] { "Id", "Activa", "Codigo", "CreadoEn", "ModificadoEn", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "PLAYA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Playa / Combustibles" },
                    { 2, true, "REPUESTOS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Repuestos" },
                    { 3, true, "LAVADERO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Lavadero" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_RazonSocial",
                table: "clientes",
                column: "RazonSocial");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_TipoDocumento_NumeroDocumento",
                table: "clientes",
                columns: new[] { "TipoDocumento", "NumeroDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_CodigoBarras",
                table: "productos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_productos_Descripcion",
                table: "productos",
                column: "Descripcion");

            migrationBuilder.CreateIndex(
                name: "IX_productos_UnidadNegocioId_Codigo",
                table: "productos",
                columns: new[] { "UnidadNegocioId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_puntos_venta_Numero",
                table: "puntos_venta",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_puntos_venta_UnidadNegocioId",
                table: "puntos_venta",
                column: "UnidadNegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_unidades_negocio_Codigo",
                table: "unidades_negocio",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_ClienteId",
                table: "vehiculos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_Patente",
                table: "vehiculos",
                column: "Patente",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "puntos_venta");

            migrationBuilder.DropTable(
                name: "vehiculos");

            migrationBuilder.DropTable(
                name: "unidades_negocio");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
