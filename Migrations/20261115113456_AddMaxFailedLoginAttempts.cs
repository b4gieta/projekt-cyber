using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projekt_cyber.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxFailedLoginAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxFailedLoginAttempts",
                table: "PasswordPolicySettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxFailedLoginAttempts",
                table: "PasswordPolicySettings");
        }
    }
}
