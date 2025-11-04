using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaFacturacionSRI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "NVARCHAR(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "NVARCHAR(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "NVARCHAR(1000)", maxLength: 1000, nullable: true),
                    Precio = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    TipoIVA = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UnidadMedida = table.Column<string>(type: "NVARCHAR(20)", maxLength: 20, nullable: false, defaultValue: "Unidad"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Codigo",
                table: "Productos",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Productos");
        }
    }
}
