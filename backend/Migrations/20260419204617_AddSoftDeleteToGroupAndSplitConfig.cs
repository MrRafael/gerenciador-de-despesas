using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToGroupAndSplitConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "GroupSplitConfig",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Group",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "GroupSplitConfig");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Group");
        }
    }
}
