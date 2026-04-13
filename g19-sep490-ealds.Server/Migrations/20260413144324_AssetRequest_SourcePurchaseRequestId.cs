using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class AssetRequest_SourcePurchaseRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourcePurchaseRequestId",
                table: "AssetRequest",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequest_SourcePurchaseRequestId",
                table: "AssetRequest",
                column: "SourcePurchaseRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetRequest_AssetRequest_SourcePurchaseRequestId",
                table: "AssetRequest",
                column: "SourcePurchaseRequestId",
                principalTable: "AssetRequest",
                principalColumn: "AssetRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetRequest_AssetRequest_SourcePurchaseRequestId",
                table: "AssetRequest");

            migrationBuilder.DropIndex(
                name: "IX_AssetRequest_SourcePurchaseRequestId",
                table: "AssetRequest");

            migrationBuilder.DropColumn(
                name: "SourcePurchaseRequestId",
                table: "AssetRequest");
        }
    }
}
