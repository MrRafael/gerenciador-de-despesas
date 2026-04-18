using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseSplitShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseSplitShare",
                columns: table => new
                {
                    expense_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expense_split_share_pk", x => new { x.expense_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_split_share_expense",
                        column: x => x.expense_id,
                        principalTable: "Expense",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_split_share_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSplitShare_user_id",
                table: "ExpenseSplitShare",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseSplitShare");
        }
    }
}
