using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personal.FinanceTracker.Reporting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlySummariesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reports");

            migrationBuilder.CreateTable(
                name: "monthly_summaries",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    total_income = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_expenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_summaries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_monthly_summaries_user_id",
                schema: "reports",
                table: "monthly_summaries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_monthly_summaries_user_period",
                schema: "reports",
                table: "monthly_summaries",
                columns: new[] { "user_id", "year", "month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monthly_summaries",
                schema: "reports");
        }
    }
}
