using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupIdAndSplitTypeToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "group_id",
                table: "Expense",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "split_type",
                table: "Expense",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expense_group_id",
                table: "Expense",
                column: "group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expense_group",
                table: "Expense",
                column: "group_id",
                principalTable: "Group",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expense_group",
                table: "Expense");

            migrationBuilder.DropIndex(
                name: "IX_Expense_group_id",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "Expense");

            migrationBuilder.DropColumn(
                name: "split_type",
                table: "Expense");
        }
    }
}
