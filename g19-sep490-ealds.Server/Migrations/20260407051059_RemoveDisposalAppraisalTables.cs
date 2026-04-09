using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDisposalAppraisalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisposalExecution_DisposalAppraisal_AppraisalId",
                table: "DisposalExecution");

            migrationBuilder.DropTable(
                name: "DisposalAppraisalMemberDecision");

            migrationBuilder.DropTable(
                name: "DisposalAppraisalReport");

            migrationBuilder.DropTable(
                name: "DisposalAppraisalMember");

            migrationBuilder.DropTable(
                name: "DisposalAppraisal");

            migrationBuilder.DropIndex(
                name: "IX_DisposalExecution_AppraisalId",
                table: "DisposalExecution");

            migrationBuilder.DropColumn(
                name: "AppraisalId",
                table: "DisposalExecution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppraisalId",
                table: "DisposalExecution",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisposalAppraisal",
                columns: table => new
                {
                    AppraisalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    MeetingDepartmentId = table.Column<int>(type: "int", nullable: true),
                    ReporterUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    MeetingLocation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisposalAppraisal", x => x.AppraisalId);
                    table.ForeignKey(
                        name: "FK_DisposalAppraisal_AssetRequest_AssetRequestId",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisal_Department_MeetingDepartmentId",
                        column: x => x.MeetingDepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisal_User_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisal_User_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisal_User_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DisposalAppraisalMember",
                columns: table => new
                {
                    AppraisalMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddedBy = table.Column<int>(type: "int", nullable: false),
                    AppraisalId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsReporter = table.Column<bool>(type: "bit", nullable: false),
                    MemberRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisposalAppraisalMember", x => x.AppraisalMemberId);
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalMember_DisposalAppraisal_AppraisalId",
                        column: x => x.AppraisalId,
                        principalTable: "DisposalAppraisal",
                        principalColumn: "AppraisalId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalMember_User_AddedBy",
                        column: x => x.AddedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalMember_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DisposalAppraisalReport",
                columns: table => new
                {
                    AppraisalReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppraisalId = table.Column<int>(type: "int", nullable: false),
                    DirectorReviewedBy = table.Column<int>(type: "int", nullable: true),
                    SubmittedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    AppraisalMethod = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AppraisalOutcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraisedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AppraisedValueInWords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttachmentUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DirectorComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DirectorDecision = table.Column<int>(type: "int", nullable: true),
                    DirectorReviewedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    MarketReferenceValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinutesNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisposalAppraisalReport", x => x.AppraisalReportId);
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalReport_DisposalAppraisal_AppraisalId",
                        column: x => x.AppraisalId,
                        principalTable: "DisposalAppraisal",
                        principalColumn: "AppraisalId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalReport_User_DirectorReviewedBy",
                        column: x => x.DirectorReviewedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalReport_User_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalReport_User_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DisposalAppraisalMemberDecision",
                columns: table => new
                {
                    AppraisalMemberDecisionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppraisalId = table.Column<int>(type: "int", nullable: false),
                    AppraisalMemberId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisposalAppraisalMemberDecision", x => x.AppraisalMemberDecisionId);
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalMemberDecision_DisposalAppraisalMember_AppraisalMemberId",
                        column: x => x.AppraisalMemberId,
                        principalTable: "DisposalAppraisalMember",
                        principalColumn: "AppraisalMemberId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalMemberDecision_DisposalAppraisal_AppraisalId",
                        column: x => x.AppraisalId,
                        principalTable: "DisposalAppraisal",
                        principalColumn: "AppraisalId");
                    table.ForeignKey(
                        name: "FK_DisposalAppraisalMemberDecision_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisposalExecution_AppraisalId",
                table: "DisposalExecution",
                column: "AppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisal_AssetRequestId",
                table: "DisposalAppraisal",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisal_CreatedBy",
                table: "DisposalAppraisal",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisal_MeetingDepartmentId",
                table: "DisposalAppraisal",
                column: "MeetingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisal_ReporterUserId",
                table: "DisposalAppraisal",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisal_UpdatedBy",
                table: "DisposalAppraisal",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalMember_AddedBy",
                table: "DisposalAppraisalMember",
                column: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalMember_AppraisalId",
                table: "DisposalAppraisalMember",
                column: "AppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalMember_UserId",
                table: "DisposalAppraisalMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalMemberDecision_AppraisalId",
                table: "DisposalAppraisalMemberDecision",
                column: "AppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalMemberDecision_AppraisalMemberId",
                table: "DisposalAppraisalMemberDecision",
                column: "AppraisalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalMemberDecision_UserId",
                table: "DisposalAppraisalMemberDecision",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalReport_AppraisalId",
                table: "DisposalAppraisalReport",
                column: "AppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalReport_DirectorReviewedBy",
                table: "DisposalAppraisalReport",
                column: "DirectorReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalReport_SubmittedBy",
                table: "DisposalAppraisalReport",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalAppraisalReport_UpdatedBy",
                table: "DisposalAppraisalReport",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_DisposalExecution_DisposalAppraisal_AppraisalId",
                table: "DisposalExecution",
                column: "AppraisalId",
                principalTable: "DisposalAppraisal",
                principalColumn: "AppraisalId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
