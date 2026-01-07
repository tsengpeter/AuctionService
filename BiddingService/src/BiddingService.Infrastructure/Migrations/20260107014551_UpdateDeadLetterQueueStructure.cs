using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiddingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeadLetterQueueStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_BidderId",
                table: "Bids");

            migrationBuilder.AlterColumn<string>(
                name: "BidderId",
                table: "Bids",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Bids_BidderIdHash",
                table: "Bids",
                column: "BidderIdHash");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_BidderIdHash_BidAt",
                table: "Bids",
                columns: new[] { "BidderIdHash", "BidAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_BidderIdHash",
                table: "Bids");

            migrationBuilder.DropIndex(
                name: "IX_Bids_BidderIdHash_BidAt",
                table: "Bids");

            migrationBuilder.AlterColumn<string>(
                name: "BidderId",
                table: "Bids",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Bids_BidderId",
                table: "Bids",
                column: "BidderId");
        }
    }
}
