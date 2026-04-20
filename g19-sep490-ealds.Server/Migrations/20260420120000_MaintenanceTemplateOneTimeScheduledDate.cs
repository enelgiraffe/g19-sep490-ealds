using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class MaintenanceTemplateOneTimeScheduledDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OneTimeScheduledDate",
                table: "MaintenanceTemplate",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OneTimeScheduledDate",
                table: "MaintenanceTemplate");
        }
    }
}
