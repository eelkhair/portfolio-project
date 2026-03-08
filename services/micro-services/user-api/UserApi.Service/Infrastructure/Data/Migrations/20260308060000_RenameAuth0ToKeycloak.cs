using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuth0ToKeycloak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Auth0UserId",
                schema: "Users",
                table: "Users",
                newName: "KeycloakUserId");

            migrationBuilder.RenameColumn(
                name: "Auth0OrganizationId",
                schema: "Users",
                table: "Companies",
                newName: "KeycloakGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_Auth0OrganizationId",
                schema: "Users",
                table: "Companies",
                newName: "IX_Companies_KeycloakGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KeycloakUserId",
                schema: "Users",
                table: "Users",
                newName: "Auth0UserId");

            migrationBuilder.RenameColumn(
                name: "KeycloakGroupId",
                schema: "Users",
                table: "Companies",
                newName: "Auth0OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_KeycloakGroupId",
                schema: "Users",
                table: "Companies",
                newName: "IX_Companies_Auth0OrganizationId");
        }
    }
}
