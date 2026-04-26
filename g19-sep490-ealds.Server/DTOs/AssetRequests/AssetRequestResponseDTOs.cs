namespace g19_sep490_ealds.Server.DTOs.AssetRequests;

// ── GetById (purchase-scoped detail) ─────────────────────────────────────────

public class AssetRequestDetailDTO
{
    public int AssetRequestId { get; set; }
    public int? AssetId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ProposedData { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public int UserId { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public string? CreatorDepartmentName { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public string? AccountantComment { get; set; }
    public string? DirectorComment { get; set; }
    public List<AssetRequestApprovalItemDTO> Approvals { get; set; } = new();
}

public class AssetRequestApprovalItemDTO
{
    public int ApprovalId { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? Comment { get; set; }
    public string? RoleCode { get; set; }
}

// ── GetDetails (generic full detail) ─────────────────────────────────────────

public class AssetRequestFullDetailDTO
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ProposedData { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ApproveDate { get; set; }
    public int? StepId { get; set; }
    public int? RequestTypeId { get; set; }
    public AssetRequestUserDTO? User { get; set; }
    public AssetRequestTypeRefDTO? RequestType { get; set; }
    public AssetRequestAssetRefDTO? Asset { get; set; }
    public List<AssetRequestFullApprovalDTO> Approvals { get; set; } = new();
    public List<AssetRequestRecordDTO> Records { get; set; } = new();
    public List<AssetRequestMaintenanceTaskDTO> MaintenanceTasks { get; set; } = new();
    public List<AssetRequestRepairTaskDTO> RepairTasks { get; set; } = new();
    public List<AssetRequestTransferRecordDTO> TransferRecords { get; set; } = new();
    public List<AssetRequestProcurementRefDTO> Procurements { get; set; } = new();
}

public class AssetRequestUserDTO
{
    public int UserId { get; set; }
    public string? Email { get; set; }
}

public class AssetRequestTypeRefDTO
{
    public int RequestTypeId { get; set; }
    public int? WorkflowId { get; set; }
}

public class AssetRequestAssetRefDTO
{
    public int AssetId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? Quantity { get; set; }
}

public class AssetRequestFullApprovalDTO
{
    public int ApprovalId { get; set; }
    public DateTime? DecisionDate { get; set; }
    public int? ApprovedUserId { get; set; }
    public int? ApprovedRoleId { get; set; }
    public int? StepId { get; set; }
    public int? Decision { get; set; }
    public string? Comment { get; set; }
    public string? RoleCode { get; set; }
    public string? RoleName { get; set; }
}

public class AssetRequestRecordDTO
{
    public int RecordId { get; set; }
    public int FromStatus { get; set; }
    public int ToStatus { get; set; }
    public int Action { get; set; }
    public int ActionByUserId { get; set; }
    public int ActionRoleId { get; set; }
    public string? Comment { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class AssetRequestMaintenanceTaskDTO
{
    public int TaskId { get; set; }
    public DateTime? PlannedDate { get; set; }
    public int Status { get; set; }
    public int? AssignTo { get; set; }
    public int? AssetInstanceId { get; set; }
    public string? InstanceCode { get; set; }
    public string? AssetName { get; set; }
}

public class AssetRequestRepairTaskDTO
{
    public int TaskId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? DamageCondition { get; set; }
    public int Status { get; set; }
    public DateTime? RepairDate { get; set; }
    public int? AssetInstanceId { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? InstanceCode { get; set; }
    public string? AssetName { get; set; }
}

public class AssetRequestTransferRecordDTO
{
    public int TransferId { get; set; }
    public int? AssetRequestId { get; set; }
    public int? AssetInstanceId { get; set; }
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    public DateTime? TransferDate { get; set; }
    public string? InstanceCode { get; set; }
    public string? AssetName { get; set; }
}

public class AssetRequestProcurementRefDTO
{
    public int ProcurementId { get; set; }
}

// ── List (generic paged list) ─────────────────────────────────────────────────

public class AssetRequestPagedResultDTO
{
    public List<AssetRequestListResultItemDTO> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AssetRequestListResultItemDTO
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public int? AssetId { get; set; }
    public int? AssetInstanceId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetInstanceCode { get; set; }
    public string? AssetName { get; set; }
    public int? AssetQuantity { get; set; }
    public string? CurrentDepartmentName { get; set; }
    public string? CurrentLocation { get; set; }
    public int? RequestTypeId { get; set; }
}
