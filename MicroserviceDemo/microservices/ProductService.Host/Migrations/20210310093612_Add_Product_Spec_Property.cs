using Microsoft.EntityFrameworkCore.Migrations;

namespace ProductService.Host.Migrations
{
    public partial class Add_Product_Spec_Property : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Spec",
                table: "PmProducts",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Spec",
                table: "PmProducts");
        }
    }
}
