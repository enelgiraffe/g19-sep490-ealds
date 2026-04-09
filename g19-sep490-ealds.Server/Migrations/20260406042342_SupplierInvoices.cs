using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class SupplierInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierInvoice",
                columns: table => new
                {
                    SupplierInvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcurementId = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoice", x => x.SupplierInvoiceId);
                    table.ForeignKey(
                        name: "FK_SupplierInvoice_GoodsReceipt",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipt",
                        principalColumn: "GoodsReceiptId");
                    table.ForeignKey(
                        name: "FK_SupplierInvoice_Procurement",
                        column: x => x.ProcurementId,
                        principalTable: "Procurement",
                        principalColumn: "ProcurementId");
                    table.ForeignKey(
                        name: "FK_SupplierInvoice_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceLine",
                columns: table => new
                {
                    SupplierInvoiceLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProcurementLineId = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoiceLine", x => x.SupplierInvoiceLineId);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLine_GoodsReceiptLine",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLine",
                        principalColumn: "GoodsReceiptLineId");
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLine_ProcurementLine",
                        column: x => x.ProcurementLineId,
                        principalTable: "ProcurementLine",
                        principalColumn: "LineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLine_SupplierInvoice",
                        column: x => x.SupplierInvoiceId,
                        principalTable: "SupplierInvoice",
                        principalColumn: "SupplierInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoice_CreatedBy",
                table: "SupplierInvoice",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoice_GoodsReceiptId",
                table: "SupplierInvoice",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoice_InvoiceNumber",
                table: "SupplierInvoice",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoice_ProcurementId",
                table: "SupplierInvoice",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLine_GoodsReceiptLineId",
                table: "SupplierInvoiceLine",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLine_ProcurementLineId",
                table: "SupplierInvoiceLine",
                column: "ProcurementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLine_SupplierInvoiceId",
                table: "SupplierInvoiceLine",
                column: "SupplierInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierInvoiceLine");

            migrationBuilder.DropTable(
                name: "SupplierInvoice");
        }
    }
}
