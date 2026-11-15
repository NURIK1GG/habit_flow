using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeeklyGoal",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_WeeklyGoal",
                table: "AspNetUsers",
                sql: "WeeklyGoal BETWEEN 1 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_WeeklyGoal",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WeeklyGoal",
                table: "AspNetUsers");
        }
    }
}
