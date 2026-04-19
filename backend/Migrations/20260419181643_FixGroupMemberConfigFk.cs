using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixGroupMemberConfigFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_member_config",
                table: "GroupMemberConfig");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberConfig_user_id",
                table: "GroupMemberConfig",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_group_member_config_group",
                table: "GroupMemberConfig",
                column: "group_id",
                principalTable: "Group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_member_config_user",
                table: "GroupMemberConfig",
                column: "user_id",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_member_config_group",
                table: "GroupMemberConfig");

            migrationBuilder.DropForeignKey(
                name: "fk_group_member_config_user",
                table: "GroupMemberConfig");

            migrationBuilder.DropIndex(
                name: "IX_GroupMemberConfig_user_id",
                table: "GroupMemberConfig");

            migrationBuilder.AddForeignKey(
                name: "fk_group_member_config",
                table: "GroupMemberConfig",
                columns: new[] { "group_id", "user_id" },
                principalTable: "GroupMember",
                principalColumns: new[] { "group_id", "user_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
