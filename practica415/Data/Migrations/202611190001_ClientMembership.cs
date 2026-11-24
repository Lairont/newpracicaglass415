using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace practica415.Data.Migrations;

[DbContext(typeof(WarehouseDbContext))]
[Migration("202611190001_ClientMembership")]
public sealed class ClientMembershipMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "FullName", table: "warehouse_users", type: "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Phone", table: "warehouse_users", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "MembershipPlan", table: "warehouse_users", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "MembershipStart", table: "warehouse_users", type: "date", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "MembershipEnd", table: "warehouse_users", type: "date", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("FullName", "warehouse_users");
        migrationBuilder.DropColumn("Phone", "warehouse_users");
        migrationBuilder.DropColumn("MembershipPlan", "warehouse_users");
        migrationBuilder.DropColumn("MembershipStart", "warehouse_users");
        migrationBuilder.DropColumn("MembershipEnd", "warehouse_users");
    }
}
