using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personal.FinanceTracker.Finance.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialFinanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finances");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "finances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "finances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "finances",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_categories_user_id",
                schema: "finances",
                table: "categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_categories_user_name",
                schema: "finances",
                table: "categories",
                columns: new[] { "user_id", "name" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_date",
                schema: "finances",
                table: "transactions",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_category",
                schema: "finances",
                table: "transactions",
                columns: new[] { "user_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user_id",
                schema: "finances",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_category_id",
                schema: "finances",
                table: "transactions",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions",
                schema: "finances");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "finances");
        }
    }
}
