using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptidCare.Claims.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimRejectionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RejectionCode",
                table: "Claims",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionCode",
                table: "ClaimRuleEvaluations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionCode",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "RejectionCode",
                table: "ClaimRuleEvaluations");
        }
    }
}
