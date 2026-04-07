using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class GoodsReceiptsAndPoReceivedQty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedQuantity",
                table: "ProcurementLine",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "GoodsReceiptLineId",
                table: "AssetInstance",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GoodsReceipt",
                columns: table => new
                {
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcurementId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipt", x => x.GoodsReceiptId);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Procurement",
                        column: x => x.ProcurementId,
                        principalTable: "Procurement",
                        principalColumn: "ProcurementId");
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLine",
                columns: table => new
                {
                    GoodsReceiptLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false),
                    ProcurementLineId = table.Column<int>(type: "int", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLine", x => x.GoodsReceiptLineId);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_Asset",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_GoodsReceipt",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipt",
                        principalColumn: "GoodsReceiptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_ProcurementLine",
                        column: x => x.ProcurementLineId,
                        principalTable: "ProcurementLine",
                        principalColumn: "LineId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetInstance_GoodsReceiptLineId",
                table: "AssetInstance",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_CreatedBy",
                table: "GoodsReceipt",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_ProcurementId",
                table: "GoodsReceipt",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_AssetId",
                table: "GoodsReceiptLine",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_GoodsReceiptId",
                table: "GoodsReceiptLine",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_ProcurementLineId",
                table: "GoodsReceiptLine",
                column: "ProcurementLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetInstance_GoodsReceiptLine",
                table: "AssetInstance",
                column: "GoodsReceiptLineId",
                principalTable: "GoodsReceiptLine",
                principalColumn: "GoodsReceiptLineId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetInstance_GoodsReceiptLine",
                table: "AssetInstance");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLine");

            migrationBuilder.DropTable(
                name: "GoodsReceipt");

            migrationBuilder.DropIndex(
                name: "IX_AssetInstance_GoodsReceiptLineId",
                table: "AssetInstance");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "ProcurementLine");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptLineId",
                table: "AssetInstance");
        }
    }
}
