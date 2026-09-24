using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EstacionERP.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Facturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comprobantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnidadNegocioId = table.Column<int>(type: "integer", nullable: false),
                    PuntoVentaId = table.Column<int>(type: "integer", nullable: false),
                    PuntoVentaNumero = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: true),
                    NumeroIntentado = table.Column<long>(type: "bigint", nullable: true),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Concepto = table.Column<int>(type: "integer", nullable: false),
                    FechaServicioDesde = table.Column<DateTime>(type: "date", nullable: true),
                    FechaServicioHasta = table.Column<DateTime>(type: "date", nullable: true),
                    FechaVencimientoPago = table.Column<DateTime>(type: "date", nullable: true),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    ReceptorTipoDocumento = table.Column<int>(type: "integer", nullable: false),
                    ReceptorNumeroDocumento = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    ReceptorRazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ReceptorCondicionIva = table.Column<int>(type: "integer", nullable: false),
                    ReceptorDomicilio = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ImporteNeto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteIva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteNoGravado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteExento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteTributos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Cae = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    CaeVencimiento = table.Column<DateTime>(type: "date", nullable: true),
                    Entorno = table.Column<int>(type: "integer", nullable: false),
                    MensajesArca = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    ComprobanteAsociadoId = table.Column<int>(type: "integer", nullable: true),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comprobantes_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_comprobantes_ComprobanteAsociadoId",
                        column: x => x.ComprobanteAsociadoId,
                        principalTable: "comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_puntos_venta_PuntoVentaId",
                        column: x => x.PuntoVentaId,
                        principalTable: "puntos_venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_unidades_negocio_UnidadNegocioId",
                        column: x => x.UnidadNegocioId,
                        principalTable: "unidades_negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_fiscal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Cuit = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreFantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CondicionIva = table.Column<int>(type: "integer", nullable: false),
                    Domicilio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Localidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IngresosBrutos = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    InicioActividades = table.Column<DateTime>(type: "date", nullable: true),
                    Entorno = table.Column<int>(type: "integer", nullable: false),
                    AliasCertificado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ClavePrivadaPem = table.Column<string>(type: "text", nullable: true),
                    ClavePendientePem = table.Column<string>(type: "text", nullable: true),
                    SolicitudCertificadoPem = table.Column<string>(type: "text", nullable: true),
                    CertificadoPem = table.Column<string>(type: "text", nullable: true),
                    CertificadoVence = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_fiscal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tickets_acceso_arca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Servicio = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Entorno = table.Column<int>(type: "integer", nullable: false),
                    Cuit = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Sign = table.Column<string>(type: "text", nullable: false),
                    Expira = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets_acceso_arca", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComprobanteId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: true),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AlicuotaIva = table.Column<int>(type: "integer", nullable: false),
                    EsServicio = table.Column<bool>(type: "boolean", nullable: false),
                    ImporteNeto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteIva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comprobantes_items_comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_comprobantes_items_productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comprobantes_iva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComprobanteId = table.Column<int>(type: "integer", nullable: false),
                    AlicuotaIva = table.Column<int>(type: "integer", nullable: false),
                    BaseImponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes_iva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comprobantes_iva_comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_ClienteId",
                table: "comprobantes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_ComprobanteAsociadoId",
                table: "comprobantes",
                column: "ComprobanteAsociadoId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_Entorno_PuntoVentaNumero_Tipo_Numero",
                table: "comprobantes",
                columns: new[] { "Entorno", "PuntoVentaNumero", "Tipo", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_Estado",
                table: "comprobantes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_Fecha",
                table: "comprobantes",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_PuntoVentaId",
                table: "comprobantes",
                column: "PuntoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_UnidadNegocioId",
                table: "comprobantes",
                column: "UnidadNegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_items_ComprobanteId",
                table: "comprobantes_items",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_items_ProductoId",
                table: "comprobantes_items",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_iva_ComprobanteId",
                table: "comprobantes_iva",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_acceso_arca_Servicio_Entorno_Cuit",
                table: "tickets_acceso_arca",
                columns: new[] { "Servicio", "Entorno", "Cuit" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comprobantes_items");

            migrationBuilder.DropTable(
                name: "comprobantes_iva");

            migrationBuilder.DropTable(
                name: "configuracion_fiscal");

            migrationBuilder.DropTable(
                name: "tickets_acceso_arca");

            migrationBuilder.DropTable(
                name: "comprobantes");
        }
    }
}
