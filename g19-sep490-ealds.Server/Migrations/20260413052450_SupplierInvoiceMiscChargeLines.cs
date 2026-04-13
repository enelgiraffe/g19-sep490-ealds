using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class SupplierInvoiceMiscChargeLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProcurementLineId",
                table: "SupplierInvoiceLine",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ChargeDescription",
                table: "SupplierInvoiceLine",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeDescription",
                table: "SupplierInvoiceLine");

            migrationBuilder.AlterColumn<int>(
                name: "ProcurementLineId",
                table: "SupplierInvoiceLine",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
