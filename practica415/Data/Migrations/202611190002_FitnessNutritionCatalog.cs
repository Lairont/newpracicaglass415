using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace practica415.Data.Migrations;

[DbContext(typeof(WarehouseDbContext))]
[Migration("202611190002_FitnessNutritionCatalog")]
public sealed class FitnessNutritionCatalogMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE products SET \"Name\" = 'Сывороточный протеин, демо', \"Sku\" = 'DEMO-WHEY-001', \"Category\" = 'Протеин', \"Unit\" = 'банка', \"UnitPrice\" = 620, \"Notes\" = 'Демонстрационный товар спортивного питания.' WHERE \"Sku\" = 'DEMO-YOGA-001';");
        migrationBuilder.Sql("UPDATE products SET \"Name\" = 'Креатин моногидрат, демо', \"Sku\" = 'DEMO-CREATINE-001', \"Category\" = 'Креатин', \"Unit\" = 'банка', \"UnitPrice\" = 360, \"Notes\" = 'Демонстрационный товар спортивного питания.' WHERE \"Sku\" = 'DEMO-DUMBBELL-001';");
        migrationBuilder.Sql("UPDATE products SET \"Name\" = 'Протеиновый батончик, демо', \"Sku\" = 'DEMO-BAR-001', \"Category\" = 'Батончики', \"Unit\" = 'шт', \"UnitPrice\" = 45, \"Notes\" = 'Демонстрационный товар спортивного питания.' WHERE \"Sku\" = 'DEMO-BAND-001';");
        migrationBuilder.Sql("UPDATE stock_movements SET \"ProductName\" = 'Сывороточный протеин, демо', \"Sku\" = 'DEMO-WHEY-001' WHERE \"Sku\" = 'DEMO-YOGA-001';");
        migrationBuilder.Sql("UPDATE stock_movements SET \"ProductName\" = 'Креатин моногидрат, демо', \"Sku\" = 'DEMO-CREATINE-001' WHERE \"Sku\" = 'DEMO-DUMBBELL-001';");
        migrationBuilder.Sql("UPDATE stock_movements SET \"ProductName\" = 'Протеиновый батончик, демо', \"Sku\" = 'DEMO-BAR-001' WHERE \"Sku\" = 'DEMO-BAND-001';");
    }
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
