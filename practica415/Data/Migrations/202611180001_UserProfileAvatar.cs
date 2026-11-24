using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace practica415.Data.Migrations;

[DbContext(typeof(WarehouseDbContext))]
[Migration("202611180001_UserProfileAvatar")]
public sealed class UserProfileAvatarMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "AvatarPath", table: "warehouse_users", type: "character varying(300)", maxLength: 300, nullable: true);
        migrationBuilder.Sql("UPDATE warehouse_users SET \"Role\" = 'User' WHERE \"Role\" = 'WarehouseOperator';");
        migrationBuilder.Sql("UPDATE warehouse_users SET \"Role\" = 'Manager' WHERE \"Role\" = 'WarehouseManager';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE warehouse_users SET \"Role\" = 'WarehouseOperator' WHERE \"Role\" = 'User';");
        migrationBuilder.Sql("UPDATE warehouse_users SET \"Role\" = 'WarehouseManager' WHERE \"Role\" = 'Manager';");
        migrationBuilder.DropColumn(name: "AvatarPath", table: "warehouse_users");
    }
}
