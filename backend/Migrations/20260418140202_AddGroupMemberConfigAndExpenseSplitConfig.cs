using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMemberConfigAndExpenseSplitConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expense_ExpenseCategory_CategoryId1",
                table: "Expense");

            migrationBuilder.DropForeignKey(
                name: "FK_Expense_User_UserId1",
                table: "Expense");

            migrationBuilder.DropIndex(
                name: "IX_Expense_CategoryId1",
                table: "Expense");

            migrationBuilder.DropIndex(
                name: "IX_Expense_UserId1",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "split_type",
                table: "Expense");

            migrationBuilder.CreateTable(
                name: "ExpenseSplitConfig",
                columns: table => new
                {
                    expense_id = table.Column<int>(type: "integer", nullable: false),
                    split_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expense_split_config_pk", x => x.expense_id);
                    table.ForeignKey(
                        name: "fk_expense_split_config",
                        column: x => x.expense_id,
                        principalTable: "Expense",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMemberConfig",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false),
                    salary = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_member_config_pk", x => new { x.group_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_group_member_config",
                        columns: x => new { x.group_id, x.user_id },
                        principalTable: "GroupMember",
                        principalColumns: new[] { "group_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseSplitConfig");

            migrationBuilder.DropTable(
                name: "GroupMemberConfig");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "Expense",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Expense",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "split_type",
                table: "Expense",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expense_CategoryId1",
                table: "Expense",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_UserId1",
                table: "Expense",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_ExpenseCategory_CategoryId1",
                table: "Expense",
                column: "CategoryId1",
                principalTable: "ExpenseCategory",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_User_UserId1",
                table: "Expense",
                column: "UserId1",
                principalTable: "User",
                principalColumn: "id");
        }
    }
}
