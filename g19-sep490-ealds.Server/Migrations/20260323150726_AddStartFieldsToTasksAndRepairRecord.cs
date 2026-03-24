using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace g19_sep490_ealds.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddStartFieldsToTasksAndRepairRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetCategory",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetCat__19093A0BBD9AABBB", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "DepreciationPolicy",
                columns: table => new
                {
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    UsefullLifeMonths = table.Column<int>(type: "int", nullable: false),
                    SalvageValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Deprecia__2E1339A4DBD75EB5", x => x.PolicyId);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RefId = table.Column<int>(type: "int", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    IsSend = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__20CF2E12625C8C38", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    SupplierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Supplier__4BE666B4C484C0AA", x => x.SupplierId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ResetPasswordToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResetPasswordTokenExpiryTime = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__User__1788CC4CD9659AAB", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseAsset",
                columns: table => new
                {
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Warehous__2608AFF9C170F644", x => x.WarehouseId);
                });

            migrationBuilder.CreateTable(
                name: "Workflow",
                columns: table => new
                {
                    WorkflowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Workflow__5704A66A5F7ECD21", x => x.WorkflowId);
                });

            migrationBuilder.CreateTable(
                name: "AssetType",
                columns: table => new
                {
                    AssetTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetTyp__FD33C2C25C7997D4", x => x.AssetTypeId);
                    table.ForeignKey(
                        name: "FK__AssetType__Categ__5629CD9C",
                        column: x => x.CategoryId,
                        principalTable: "AssetCategory",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Departme__B2079BED77FCD9A8", x => x.DepartmentId);
                    table.ForeignKey(
                        name: "FK__Departmen__Updat__4222D4EF",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Role__8AFACE1ABA6BB0F6", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK__Role__UpdatedBy__3B75D760",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "RequestType",
                columns: table => new
                {
                    RequestTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RequestT__4D328B839803B9F3", x => x.RequestTypeId);
                    table.ForeignKey(
                        name: "FK__RequestTy__Workf__5165187F",
                        column: x => x.WorkflowId,
                        principalTable: "Workflow",
                        principalColumn: "WorkflowId");
                });

            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AssetTypeId = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WarrantyEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    InUseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Asset__43492352E67F3D08", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK__Asset__AssetType__5BE2A6F2",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetType",
                        principalColumn: "AssetTypeId");
                    table.ForeignKey(
                        name: "FK__Asset__CreatedBy__5DCAEF64",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Asset__Warehouse__5CD6CB2B",
                        column: x => x.WarehouseId,
                        principalTable: "WarehouseAsset",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTemplate",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrequencyType = table.Column<int>(type: "int", nullable: false),
                    RepeatIntervalValue = table.Column<int>(type: "int", nullable: false),
                    RepeatIntervalUnit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__F87ADD279992E41B", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK__Maintenan__IsAct__1EA48E88",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetType",
                        principalColumn: "AssetTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Dob = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Employee__7AD04F119EA8424A", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK__Employee__Create__47DBAE45",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Employee__Depart__46E78A0C",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK__Employee__UserId__45F365D3",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "InventorySession",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false),
                    AssetTypeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__C9F49290BE3C907A", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK__Inventory__Asset__43D61337",
                        column: x => x.AssetCategoryId,
                        principalTable: "AssetCategory",
                        principalColumn: "CategoryId");
                    table.ForeignKey(
                        name: "FK__Inventory__Asset__44CA3770",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetType",
                        principalColumn: "AssetTypeId");
                    table.ForeignKey(
                        name: "FK__Inventory__Creat__42E1EEFE",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK__Inventory__Creat__45BE5BA9",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK__UserRole__RoleId__3E52440B",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK__UserRole__UserId__3D5E1FD2",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStep",
                columns: table => new
                {
                    StepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsFinalStep = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Workflow__24343357E49AFA53", x => x.StepId);
                    table.ForeignKey(
                        name: "FK__WorkflowS__RoleI__4E88ABD4",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK__WorkflowS__Workf__4D94879B",
                        column: x => x.WorkflowId,
                        principalTable: "Workflow",
                        principalColumn: "WorkflowId");
                });

            migrationBuilder.CreateTable(
                name: "AssetCapitalization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    CapitalizedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CapitalizedBy = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCapitalization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetCapitalization_Asset_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetCapitalization_User_CapitalizedBy",
                        column: x => x.CapitalizedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "AssetLifeCycle",
                columns: table => new
                {
                    AuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    RelatedEntityType = table.Column<int>(type: "int", nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    ActorRoleId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetLif__A17F2398D8AAD078", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK__AssetLife__Actor__75A278F5",
                        column: x => x.ActorUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__AssetLife__Actor__76969D2E",
                        column: x => x.ActorRoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK__AssetLife__Occur__74AE54BC",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                });

            migrationBuilder.CreateTable(
                name: "AssetLocation",
                columns: table => new
                {
                    LocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetLoc__E7FEA497FCEA8382", x => x.LocationId);
                    table.ForeignKey(
                        name: "FK__AssetLoca__Asset__6FE99F9F",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__AssetLoca__Depar__70DDC3D8",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                });

            migrationBuilder.CreateTable(
                name: "AssetRequest",
                columns: table => new
                {
                    AssetRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestTypeId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    StepId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetReq__0CA9D3840B64CC53", x => x.AssetRequestId);
                    table.ForeignKey(
                        name: "FK__AssetRequ__Asset__6383C8BA",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__AssetRequ__Reque__628FA481",
                        column: x => x.RequestTypeId,
                        principalTable: "RequestType",
                        principalColumn: "RequestTypeId");
                    table.ForeignKey(
                        name: "FK__AssetRequ__UserI__619B8048",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DrepreciationRecord",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    DepreciationAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Drepreci__FBDF78E9C90EFE9C", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK__Dreprecia__Asset__0E6E26BF",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__Dreprecia__Polic__0F624AF8",
                        column: x => x.PolicyId,
                        principalTable: "DepreciationPolicy",
                        principalColumn: "PolicyId");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedule",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    ScheduleType = table.Column<int>(type: "int", nullable: false),
                    IntervalMonths = table.Column<int>(type: "int", nullable: true),
                    IntervalHours = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    NextDueDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreateBy = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__9C8A5B496EFEDE90", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK__Maintenan__Asset__236943A5",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__Maintenan__Creat__245D67DE",
                        column: x => x.CreateBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Maintenan__Templ__22751F6C",
                        column: x => x.TemplateId,
                        principalTable: "MaintenanceTemplate",
                        principalColumn: "TemplateId");
                });

            migrationBuilder.CreateTable(
                name: "AssetUsage",
                columns: table => new
                {
                    UsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetUsa__29B197202990F957", x => x.UsageId);
                    table.ForeignKey(
                        name: "FK__AssetUsag__Asset__6C190EBB",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__AssetUsag__Emplo__6D0D32F4",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTask",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__7C6949B117E656E9", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK__Inventory__Asset__498EEC8D",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__Inventory__Assig__4B7734FF",
                        column: x => x.AssignedUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Inventory__Depar__4A8310C6",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK__Inventory__Sessi__489AC854",
                        column: x => x.SessionId,
                        principalTable: "InventorySession",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateTable(
                name: "Approval",
                columns: table => new
                {
                    ApprovalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ApprovedUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedRoleId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Approval__328477F426B63B6B", x => x.ApprovalId);
                    table.ForeignKey(
                        name: "FK__Approval__Approv__6754599E",
                        column: x => x.ApprovedUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Approval__Approv__68487DD7",
                        column: x => x.ApprovedRoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK__Approval__AssetR__66603565",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                    table.ForeignKey(
                        name: "FK__Approval__StepId__693CA210",
                        column: x => x.StepId,
                        principalTable: "WorkflowStep",
                        principalColumn: "StepId");
                });

            migrationBuilder.CreateTable(
                name: "AssetRequestRecord",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionByUserId = table.Column<int>(type: "int", nullable: false),
                    ActionRoleId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AssetReq__FBDF78E9FDA92C09", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK__AssetRequ__Actio__1AD3FDA4",
                        column: x => x.ActionByUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__AssetRequ__Actio__1BC821DD",
                        column: x => x.ActionRoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK__AssetRequ__Asset__19DFD96B",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                });

            migrationBuilder.CreateTable(
                name: "DiposalRecord",
                columns: table => new
                {
                    DiposalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    DiposalMethod = table.Column<int>(type: "int", nullable: false),
                    DiposalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiposalDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DiposalR__EF94B94CAAD0B945", x => x.DiposalId);
                    table.ForeignKey(
                        name: "FK__DiposalRe__Asset__123EB7A3",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__DiposalRe__Asset__1332DBDC",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                    table.ForeignKey(
                        name: "FK__DiposalRe__Execu__14270015",
                        column: x => x.ExecutedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Procurement",
                columns: table => new
                {
                    ProcurementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    ContractNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContractDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Procurem__95B451EC99F6191D", x => x.ProcurementId);
                    table.ForeignKey(
                        name: "FK__Procureme__Asset__7F2BE32F",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                    table.ForeignKey(
                        name: "FK__Procureme__Creat__00200768",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Procureme__Suppl__7E37BEF6",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "SupplierId");
                });

            migrationBuilder.CreateTable(
                name: "RepairTask",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RepairDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpectedCompletionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RepairProgressStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RepairTa__7C6949B11CA3EB4B", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK__RepairTas__Asset__3A4CA8FD",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__RepairTas__Asset__3B40CD36",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                });

            migrationBuilder.CreateTable(
                name: "TransferRecord",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: false),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    FromUserId = table.Column<int>(type: "int", nullable: true),
                    ToUserId = table.Column<int>(type: "int", nullable: true),
                    TransferDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExecuteBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Transfer__FBDF78E93D1B7772", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK__TransferR__Asset__31B762FC",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__TransferR__Asset__32AB8735",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                    table.ForeignKey(
                        name: "FK__TransferR__Execu__37703C52",
                        column: x => x.ExecuteBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__TransferR__FromL__339FAB6E",
                        column: x => x.FromLocationId,
                        principalTable: "AssetLocation",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK__TransferR__FromU__3587F3E0",
                        column: x => x.FromUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__TransferR__ToLoc__3493CFA7",
                        column: x => x.ToLocationId,
                        principalTable: "AssetLocation",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK__TransferR__ToUse__367C1819",
                        column: x => x.ToUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "MaintenaceTask",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    AssetRequestId = table.Column<int>(type: "int", nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    AssignTo = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CreateBy = table.Column<int>(type: "int", nullable: false),
                    PerformerUserId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpectedCompletionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    MaintenanceContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__7C6949B1AA925102", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK__Maintenac__Asset__2A164134",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequest",
                        principalColumn: "AssetRequestId");
                    table.ForeignKey(
                        name: "FK__Maintenac__Assig__2B0A656D",
                        column: x => x.AssignTo,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Maintenac__Creat__282DF8C2",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "AssetId");
                    table.ForeignKey(
                        name: "FK__Maintenac__Creat__2BFE89A6",
                        column: x => x.CreateBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Maintenac__Sched__29221CFB",
                        column: x => x.ScheduleId,
                        principalTable: "MaintenanceSchedule",
                        principalColumn: "ScheduleId");
                });

            migrationBuilder.CreateTable(
                name: "InventoryDiscrepancy",
                columns: table => new
                {
                    DiscrepancyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    DiscrepancyType = table.Column<int>(type: "int", nullable: false),
                    BookValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BookLocationId = table.Column<int>(type: "int", nullable: false),
                    BookUserId = table.Column<int>(type: "int", nullable: true),
                    BookCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualLocationId = table.Column<int>(type: "int", nullable: false),
                    ActualUserId = table.Column<int>(type: "int", nullable: true),
                    ActualCondition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__7462A89253901749", x => x.DiscrepancyId);
                    table.ForeignKey(
                        name: "FK__Inventory__Actua__55F4C372",
                        column: x => x.ActualLocationId,
                        principalTable: "AssetLocation",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK__Inventory__Actua__57DD0BE4",
                        column: x => x.ActualUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Inventory__BookL__55009F39",
                        column: x => x.BookLocationId,
                        principalTable: "AssetLocation",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK__Inventory__BookU__56E8E7AB",
                        column: x => x.BookUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Inventory__TaskI__540C7B00",
                        column: x => x.TaskId,
                        principalTable: "InventoryTask",
                        principalColumn: "TaskId");
                });

            migrationBuilder.CreateTable(
                name: "InventoryRecord",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    ActualLocationId = table.Column<int>(type: "int", nullable: false),
                    ActualUserId = table.Column<int>(type: "int", nullable: true),
                    ActualCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFound = table.Column<bool>(type: "bit", nullable: true),
                    CheckedBy = table.Column<int>(type: "int", nullable: false),
                    CheckedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DateCheckCompleted = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__FBDF78E9EA25DCF8", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK__Inventory__Actua__4F47C5E3",
                        column: x => x.ActualLocationId,
                        principalTable: "AssetLocation",
                        principalColumn: "LocationId");
                    table.ForeignKey(
                        name: "FK__Inventory__Actua__503BEA1C",
                        column: x => x.ActualUserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Inventory__Check__51300E55",
                        column: x => x.CheckedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK__Inventory__DateC__4E53A1AA",
                        column: x => x.TaskId,
                        principalTable: "InventoryTask",
                        principalColumn: "TaskId");
                });

            migrationBuilder.CreateTable(
                name: "AcceptanceRecord",
                columns: table => new
                {
                    AcceptanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcurementId = table.Column<int>(type: "int", nullable: false),
                    AcceptanceDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TrialStartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcceptedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Acceptan__747806F6B4C75151", x => x.AcceptanceId);
                    table.ForeignKey(
                        name: "FK__Acceptanc__Accep__06CD04F7",
                        column: x => x.ProcurementId,
                        principalTable: "Procurement",
                        principalColumn: "ProcurementId");
                    table.ForeignKey(
                        name: "FK__Acceptanc__Accep__07C12930",
                        column: x => x.AcceptedBy,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcurementId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Document__1ABEEF0FA851738F", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK__Document__Procur__03F0984C",
                        column: x => x.ProcurementId,
                        principalTable: "Procurement",
                        principalColumn: "ProcurementId");
                });

            migrationBuilder.CreateTable(
                name: "RepairRecord",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RepairDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DamageDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DamageCondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RepairRe__FBDF78E9DBAA0F0C", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK__RepairRec__Suppl__3F115E1A",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "SupplierId");
                    table.ForeignKey(
                        name: "FK__RepairRec__TaskI__3E1D39E1",
                        column: x => x.TaskId,
                        principalTable: "RepairTask",
                        principalColumn: "TaskId");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecord",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    ExecutionDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WorkPerformed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionBefore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionAfter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicalNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__FBDF78E957EB0F1D", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK__Maintenan__Techn__2EDAF651",
                        column: x => x.TaskId,
                        principalTable: "MaintenaceTask",
                        principalColumn: "TaskId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceRecord_AcceptedBy",
                table: "AcceptanceRecord",
                column: "AcceptedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceRecord_ProcurementId",
                table: "AcceptanceRecord",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_Approval_ApprovedRoleId",
                table: "Approval",
                column: "ApprovedRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Approval_ApprovedUserId",
                table: "Approval",
                column: "ApprovedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Approval_AssetRequestId",
                table: "Approval",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Approval_StepId",
                table: "Approval",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_AssetTypeId",
                table: "Asset",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_CreatedBy",
                table: "Asset",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_WarehouseId",
                table: "Asset",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ__Asset__A25C5AA73764417A",
                table: "Asset",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Asset__A25C5AA75E51F3B6",
                table: "Asset",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetCapitalization_AssetId",
                table: "AssetCapitalization",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCapitalization_CapitalizedBy",
                table: "AssetCapitalization",
                column: "CapitalizedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifeCycle_ActorRoleId",
                table: "AssetLifeCycle",
                column: "ActorRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifeCycle_ActorUserId",
                table: "AssetLifeCycle",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifeCycle_AssetId",
                table: "AssetLifeCycle",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLocation_AssetId",
                table: "AssetLocation",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLocation_DepartmentId",
                table: "AssetLocation",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequest_AssetId",
                table: "AssetRequest",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequest_RequestTypeId",
                table: "AssetRequest",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequest_UserId",
                table: "AssetRequest",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequestRecord_ActionByUserId",
                table: "AssetRequestRecord",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequestRecord_ActionRoleId",
                table: "AssetRequestRecord",
                column: "ActionRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequestRecord_AssetRequestId",
                table: "AssetRequestRecord",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetType_CategoryId",
                table: "AssetType",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetUsage_AssetId",
                table: "AssetUsage",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetUsage_EmployeeId",
                table: "AssetUsage",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedBy",
                table: "Department",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiposalRecord_AssetId",
                table: "DiposalRecord",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_DiposalRecord_AssetRequestId",
                table: "DiposalRecord",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DiposalRecord_ExecutedBy",
                table: "DiposalRecord",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Document_ProcurementId",
                table: "Document",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_DrepreciationRecord_AssetId",
                table: "DrepreciationRecord",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_DrepreciationRecord_PolicyId",
                table: "DrepreciationRecord",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_CreatedBy",
                table: "Employee",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DepartmentId",
                table: "Employee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserId",
                table: "Employee",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDiscrepancy_ActualLocationId",
                table: "InventoryDiscrepancy",
                column: "ActualLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDiscrepancy_ActualUserId",
                table: "InventoryDiscrepancy",
                column: "ActualUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDiscrepancy_BookLocationId",
                table: "InventoryDiscrepancy",
                column: "BookLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDiscrepancy_BookUserId",
                table: "InventoryDiscrepancy",
                column: "BookUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDiscrepancy_TaskId",
                table: "InventoryDiscrepancy",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRecord_ActualLocationId",
                table: "InventoryRecord",
                column: "ActualLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRecord_ActualUserId",
                table: "InventoryRecord",
                column: "ActualUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRecord_CheckedBy",
                table: "InventoryRecord",
                column: "CheckedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRecord_TaskId",
                table: "InventoryRecord",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySession_AssetCategoryId",
                table: "InventorySession",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySession_AssetTypeId",
                table: "InventorySession",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySession_CreatedBy",
                table: "InventorySession",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySession_DepartmentId",
                table: "InventorySession",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UQ__Inventor__A25C5AA78FAD898D",
                table: "InventorySession",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Inventor__A25C5AA7BFABD97D",
                table: "InventorySession",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTask_AssetId",
                table: "InventoryTask",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTask_AssignedUserId",
                table: "InventoryTask",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTask_DepartmentId",
                table: "InventoryTask",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTask_SessionId",
                table: "InventoryTask",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenaceTask_AssetId",
                table: "MaintenaceTask",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenaceTask_AssetRequestId",
                table: "MaintenaceTask",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenaceTask_AssignTo",
                table: "MaintenaceTask",
                column: "AssignTo");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenaceTask_CreateBy",
                table: "MaintenaceTask",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenaceTask_ScheduleId",
                table: "MaintenaceTask",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecord_TaskId",
                table: "MaintenanceRecord",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedule_AssetId",
                table: "MaintenanceSchedule",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedule_CreateBy",
                table: "MaintenanceSchedule",
                column: "CreateBy");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedule_TemplateId",
                table: "MaintenanceSchedule",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTemplate_AssetTypeId",
                table: "MaintenanceTemplate",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurement_AssetRequestId",
                table: "Procurement",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurement_CreatedBy",
                table: "Procurement",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Procurement_SupplierId",
                table: "Procurement",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRecord_SupplierId",
                table: "RepairRecord",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRecord_TaskId",
                table: "RepairRecord",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTask_AssetId",
                table: "RepairTask",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTask_AssetRequestId",
                table: "RepairTask",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestType_WorkflowId",
                table: "RequestType",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_CreatedBy",
                table: "Role",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ__Supplier__A25C5AA73A268A53",
                table: "Supplier",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Supplier__A25C5AA7B0056715",
                table: "Supplier",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_AssetId",
                table: "TransferRecord",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_AssetRequestId",
                table: "TransferRecord",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_ExecuteBy",
                table: "TransferRecord",
                column: "ExecuteBy");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_FromLocationId",
                table: "TransferRecord",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_FromUserId",
                table: "TransferRecord",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_ToLocationId",
                table: "TransferRecord",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_ToUserId",
                table: "TransferRecord",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "UQ__User__A9D10534A0864F67",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__User__A9D10534EBCFA9A1",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStep_RoleId",
                table: "WorkflowStep",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStep_WorkflowId",
                table: "WorkflowStep",
                column: "WorkflowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptanceRecord");

            migrationBuilder.DropTable(
                name: "Approval");

            migrationBuilder.DropTable(
                name: "AssetCapitalization");

            migrationBuilder.DropTable(
                name: "AssetLifeCycle");

            migrationBuilder.DropTable(
                name: "AssetRequestRecord");

            migrationBuilder.DropTable(
                name: "AssetUsage");

            migrationBuilder.DropTable(
                name: "DiposalRecord");

            migrationBuilder.DropTable(
                name: "Document");

            migrationBuilder.DropTable(
                name: "DrepreciationRecord");

            migrationBuilder.DropTable(
                name: "InventoryDiscrepancy");

            migrationBuilder.DropTable(
                name: "InventoryRecord");

            migrationBuilder.DropTable(
                name: "MaintenanceRecord");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "RepairRecord");

            migrationBuilder.DropTable(
                name: "TransferRecord");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "WorkflowStep");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Procurement");

            migrationBuilder.DropTable(
                name: "DepreciationPolicy");

            migrationBuilder.DropTable(
                name: "InventoryTask");

            migrationBuilder.DropTable(
                name: "MaintenaceTask");

            migrationBuilder.DropTable(
                name: "RepairTask");

            migrationBuilder.DropTable(
                name: "AssetLocation");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "InventorySession");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedule");

            migrationBuilder.DropTable(
                name: "AssetRequest");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "MaintenanceTemplate");

            migrationBuilder.DropTable(
                name: "Asset");

            migrationBuilder.DropTable(
                name: "RequestType");

            migrationBuilder.DropTable(
                name: "AssetType");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "WarehouseAsset");

            migrationBuilder.DropTable(
                name: "Workflow");

            migrationBuilder.DropTable(
                name: "AssetCategory");
        }
    }
}
