using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupSplitConfigAndMonthClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "split_type",
                table: "ExpenseSplitConfig",
                newName: "group_split_config_id");

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "ExpenseSplitShare",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "GroupMonthClose",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_month_close_pk", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_month_close_group",
                        column: x => x.group_id,
                        principalTable: "Group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupSplitConfig",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    split_type = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_split_config_pk", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_split_config_group",
                        column: x => x.group_id,
                        principalTable: "Group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMonthCloseConfirmation",
                columns: table => new
                {
                    group_month_close_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_month_close_confirmation_pk", x => new { x.group_month_close_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_month_close_confirmation_close",
                        column: x => x.group_month_close_id,
                        principalTable: "GroupMonthClose",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_month_close_confirmation_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupSplitConfigShare",
                columns: table => new
                {
                    group_split_config_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_split_config_share_pk", x => new { x.group_split_config_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_split_config_share_config",
                        column: x => x.group_split_config_id,
                        principalTable: "GroupSplitConfig",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_split_config_share_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSplitConfig_group_split_config_id",
                table: "ExpenseSplitConfig",
                column: "group_split_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMonthClose_group_id",
                table: "GroupMonthClose",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMonthCloseConfirmation_user_id",
                table: "GroupMonthCloseConfirmation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupSplitConfig_group_id",
                table: "GroupSplitConfig",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupSplitConfigShare_user_id",
                table: "GroupSplitConfigShare",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expense_split_config_group_split",
                table: "ExpenseSplitConfig",
                column: "group_split_config_id",
                principalTable: "GroupSplitConfig",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expense_split_config_group_split",
                table: "ExpenseSplitConfig");

            migrationBuilder.DropTable(
                name: "GroupMonthCloseConfirmation");

            migrationBuilder.DropTable(
                name: "GroupSplitConfigShare");

            migrationBuilder.DropTable(
                name: "GroupMonthClose");

            migrationBuilder.DropTable(
                name: "GroupSplitConfig");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseSplitConfig_group_split_config_id",
                table: "ExpenseSplitConfig");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "ExpenseSplitShare");

            migrationBuilder.RenameColumn(
                name: "group_split_config_id",
                table: "ExpenseSplitConfig",
                newName: "split_type");
        }
    }
}
