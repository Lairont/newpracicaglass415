using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace practica415.Data.Migrations;

[DbContext(typeof(WarehouseDbContext))]
[Migration("202611170001_FitStockWarehouse")]
public sealed class FitStockWarehouseMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "products",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Sku = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                Unit = table.Column<string>(type: "text", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                MinimumStock = table.Column<int>(type: "integer", nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                Zone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Condition = table.Column<int>(type: "integer", nullable: false),
                PurchaseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                WarrantyUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_products", x => x.Id));

        migrationBuilder.CreateTable(
            name: "stock_movements",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                ProductName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Sku = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                Unit = table.Column<string>(type: "text", nullable: false),
                Zone = table.Column<string>(type: "text", nullable: false),
                Note = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_stock_movements", x => x.Id));

        migrationBuilder.CreateTable(
            name: "warehouse_orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Number = table.Column<string>(type: "text", nullable: false),
                Customer = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                ProductName = table.Column<string>(type: "text", nullable: false),
                Sku = table.Column<string>(type: "text", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_warehouse_orders", x => x.Id));

        migrationBuilder.CreateTable(
            name: "warehouse_users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Role = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_warehouse_users", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_products_Sku", table: "products", column: "Sku", unique: true);
        migrationBuilder.CreateIndex(name: "IX_stock_movements_CreatedAt", table: "stock_movements", column: "CreatedAt");
        migrationBuilder.CreateIndex(name: "IX_warehouse_orders_Number", table: "warehouse_orders", column: "Number", unique: true);
        migrationBuilder.CreateIndex(name: "IX_warehouse_users_Email", table: "warehouse_users", column: "Email", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "stock_movements");
        migrationBuilder.DropTable(name: "warehouse_orders");
        migrationBuilder.DropTable(name: "warehouse_users");
        migrationBuilder.DropTable(name: "products");
    }
}
