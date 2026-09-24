using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstacionERP.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ImpresionComprobantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormatoImpresion",
                table: "puntos_venta",
                type: "integer",
                nullable: false,
                defaultValue: 1);   // A4 para los puntos de venta ya cargados

            migrationBuilder.AddColumn<string>(
                name: "Impresora",
                table: "puntos_venta",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ImprimirAlEmitir",
                table: "puntos_venta",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormatoImpresion",
                table: "puntos_venta");

            migrationBuilder.DropColumn(
                name: "Impresora",
                table: "puntos_venta");

            migrationBuilder.DropColumn(
                name: "ImprimirAlEmitir",
                table: "puntos_venta");
        }
    }
}
