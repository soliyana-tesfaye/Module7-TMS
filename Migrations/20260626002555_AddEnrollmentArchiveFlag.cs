using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentArchiveFlag : Migration
    {
        /// <inheritdoc />
        /// Up() adds a new boolean column named IsArchived to the Enrollments table and gives existing rows a default value of false.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        /// Down() reverses the migration by removing the IsArchived column from the Enrollments table.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Enrollments");
        }
    }
}
