using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptIdToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoodsReceiptId",
                table: "Document",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierInvoiceId",
                table: "Document",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Document_GoodsReceiptId",
                table: "Document",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_SupplierInvoiceId",
                table: "Document",
                column: "SupplierInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Document_GoodsReceipt_GoodsReceiptId",
                table: "Document",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipt",
                principalColumn: "GoodsReceiptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Document_SupplierInvoice_SupplierInvoiceId",
                table: "Document",
                column: "SupplierInvoiceId",
                principalTable: "SupplierInvoice",
                principalColumn: "SupplierInvoiceId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Document_GoodsReceipt_GoodsReceiptId",
                table: "Document");

            migrationBuilder.DropForeignKey(
                name: "FK_Document_SupplierInvoice_SupplierInvoiceId",
                table: "Document");

            migrationBuilder.DropIndex(
                name: "IX_Document_GoodsReceiptId",
                table: "Document");

            migrationBuilder.DropIndex(
                name: "IX_Document_SupplierInvoiceId",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptId",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceId",
                table: "Document");
        }
    }
}
