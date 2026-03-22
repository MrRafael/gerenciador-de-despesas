using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyFinBackend.Migrations
{
    /// <inheritdoc />
    public partial class start : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying", nullable: false),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    photo = table.Column<string>(type: "character varying", nullable: true),
                    email = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expense_category_pk", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_category_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Group",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_pk", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_owner",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expense",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying", nullable: false),
                    value = table.Column<float>(type: "numeric", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    UserId1 = table.Column<string>(type: "character varying", nullable: true),
                    CategoryId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expense_pk", x => x.id);
                    table.ForeignKey(
                        name: "FK_Expense_ExpenseCategory_CategoryId1",
                        column: x => x.CategoryId1,
                        principalTable: "ExpenseCategory",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Expense_User_UserId1",
                        column: x => x.UserId1,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_expense_category",
                        column: x => x.category_id,
                        principalTable: "ExpenseCategory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expense_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMember",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_member_pk", x => new { x.group_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_member_group",
                        column: x => x.group_id,
                        principalTable: "Group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_member_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expense_category_id",
                table: "Expense",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_CategoryId1",
                table: "Expense",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_user_id",
                table: "Expense",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_UserId1",
                table: "Expense",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_user_id",
                table: "ExpenseCategory",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Group_user_id",
                table: "Group",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMember_user_id",
                table: "GroupMember",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expense");

            migrationBuilder.DropTable(
                name: "GroupMember");

            migrationBuilder.DropTable(
                name: "ExpenseCategory");

            migrationBuilder.DropTable(
                name: "Group");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
