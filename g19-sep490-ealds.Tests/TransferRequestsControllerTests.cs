using g19_sep490_ealds.Server.Controllers;
using g19_sep490_ealds.Server.DTOs.Transfers;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace g19_sep490_ealds.Tests;

public class TransferRequestsControllerTests
{
    private readonly Mock<ITransferRequestService> _mockService;
    private readonly TransferRequestsController _controller;

    public TransferRequestsControllerTests()
    {
        _mockService = new Mock<ITransferRequestService>();
        _controller = new TransferRequestsController(_mockService.Object);
    }

    private void SetUserContext(int userId, bool isAccountant = false)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var roles = new List<string>();
        if (isAccountant) roles.Add("ACCOUNTANT");

        var identity = new ClaimsIdentity(claims, "TestAuth");
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetUserWithoutClaim()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    private static TransferRequestDTO MakeCreateDto(
        int assetInstanceId = 1,
        int fromLocationId = 1,
        int toLocationId = 2,
        DateTime? transferDate = null,
        int requestTypeId = 3)
    {
        return new TransferRequestDTO
        {
            AssetInstanceId = assetInstanceId,
            RequestTypeId = requestTypeId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            TransferDate = transferDate,
            ExecuteBy = 1,
            CreatedBy = 1,
            Description = "Test transfer"
        };
    }

    private static UpdateTransferDraftBody MakeUpdateDraftBody(string? draftFormJson = null)
    {
        return new UpdateTransferDraftBody { DraftFormJson = draftFormJson ?? "{}" };
    }

    private static TransferHandoverConfirmBody MakeConfirmBody(string? note = null)
    {
        return new TransferHandoverConfirmBody { Note = note };
    }

    #region GetList Tests

    [Fact]
    public async Task GetList_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.GetList();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetList_WithValidUser_ReturnsOkWithList()
    {
        SetUserContext(1, isAccountant: false);
        var items = new List<TransferRequestListItemDTO>
        {
            new TransferRequestListItemDTO
            {
                RecordId = 1,
                AssetRequestId = 1,
                Code = "TR001",
                TransferDate = DateTime.UtcNow,
                AssetCode = "ASSET001",
                AssetName = "Test Asset",
                FromDepartment = "IT",
                ToDepartment = "HR",
                FromDepartmentId = 1,
                ToDepartmentId = 2,
                CreatedBy = 1,
                Quantity = 1,
                Status = 1,
                StatusName = "Đang xử lý"
            }
        };
        _mockService.Setup(s => s.GetListAsync(1, false)).ReturnsAsync(items);

        var result = await _controller.GetList();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsAssignableFrom<IEnumerable<TransferRequestListItemDTO>>(okResult.Value);
        Assert.Single(returnedItems);
    }

    [Fact]
    public async Task GetList_AccountantRole_CallsServiceWithIsAccountantTrue()
    {
        SetUserContext(1, isAccountant: true);
        _mockService.Setup(s => s.GetListAsync(1, true)).ReturnsAsync(new List<TransferRequestListItemDTO>());

        var result = await _controller.GetList();

        Assert.IsType<OkObjectResult>(result.Result);
        _mockService.Verify(s => s.GetListAsync(1, true), Times.Once);
    }

    [Fact]
    public async Task GetList_EmptyList_ReturnsOkWithEmptyCollection()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.GetListAsync(1, false)).ReturnsAsync(new List<TransferRequestListItemDTO>());

        var result = await _controller.GetList();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsAssignableFrom<IEnumerable<TransferRequestListItemDTO>>(okResult.Value);
        Assert.Empty(returnedItems);
    }

    [Fact]
    public async Task GetList_ServiceThrowsKeyNotFound_ReturnsNotFound()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.GetListAsync(1, false)).ThrowsAsync(new KeyNotFoundException("Not found"));

        var result = await _controller.GetList();

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetList_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.GetListAsync(1, false)).ThrowsAsync(new ArgumentException("Invalid param"));

        var result = await _controller.GetList();

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region GetHandoverRecords Tests

    [Fact]
    public async Task GetHandoverRecords_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.GetHandoverRecords(1);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetHandoverRecords_WithValidUser_ReturnsOkWithRecords()
    {
        SetUserContext(1);
        var records = new List<TransferHandoverRecordItemDto>
        {
            new TransferHandoverRecordItemDto
            {
                TransferHandoverRecordId = 1,
                Side = "Sender",
                ActionByUserId = 1,
                ActionByUserName = "Test User",
                OccurredAt = DateTime.UtcNow,
                Details = new TransferHandoverDetailsDto
                {
                    Side = "Sender",
                    ProtocolCode = "PROTO001",
                    AssetRequestId = 1,
                    FromDepartment = "IT",
                    ToDepartment = "HR",
                    InstanceCode = "INS001",
                    AssetCode = "ASSET001",
                    AssetName = "Test Asset",
                    Summary = "Test handover"
                }
            }
        };
        _mockService.Setup(s => s.GetHandoverRecordsAsync(1, false, 1)).ReturnsAsync(records);

        var result = await _controller.GetHandoverRecords(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRecords = Assert.IsAssignableFrom<IEnumerable<TransferHandoverRecordItemDto>>(okResult.Value);
        Assert.Single(returnedRecords);
    }

    [Fact]
    public async Task GetHandoverRecords_NonExistentId_ReturnsNotFound()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.GetHandoverRecordsAsync(1, false, 999)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.GetHandoverRecords(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHandoverRecords_AccountantRole_CallsServiceWithIsAccountantTrue()
    {
        SetUserContext(1, isAccountant: true);
        _mockService.Setup(s => s.GetHandoverRecordsAsync(1, true, 1)).ReturnsAsync(new List<TransferHandoverRecordItemDto>());

        var result = await _controller.GetHandoverRecords(1);

        Assert.IsType<OkObjectResult>(result.Result);
        _mockService.Verify(s => s.GetHandoverRecordsAsync(1, true, 1), Times.Once);
    }

    [Fact]
    public async Task GetHandoverRecords_EmptyList_ReturnsOkWithEmptyCollection()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.GetHandoverRecordsAsync(1, false, 1)).ReturnsAsync(new List<TransferHandoverRecordItemDto>());

        var result = await _controller.GetHandoverRecords(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRecords = Assert.IsAssignableFrom<IEnumerable<TransferHandoverRecordItemDto>>(okResult.Value);
        Assert.Empty(returnedRecords);
    }

    [Fact]
    public async Task GetHandoverRecords_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.GetHandoverRecordsAsync(1, false, 1)).ThrowsAsync(new ArgumentException("Invalid"));

        var result = await _controller.GetHandoverRecords(1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Test case 1 (Normal): TransferDate = today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_ValidDataToday_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case 2 (Normal): TransferDate = today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_ValidData_ReturnsOkRegardlessOfIsSenderConfirmed()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Test case 3 (Abnormal): IsSenderConfirmed not in DTO (always false).
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_IsSenderConfirmedNotInDto_AlwaysFalse()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 4 (Abnormal): IsSenderConfirmed must be false during creation.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_IsSenderConfirmedIgnored_AlwaysFalse()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 5 (Normal): TransferDate <= today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_PastTransferDate_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow.AddDays(-1), fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 6 (Normal): TransferDate >= today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK
    /// </summary>
    [Fact]
    public async Task Create_FutureTransferDate_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow.AddDays(1), fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 7 (Abnormal): FromLocationId = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_FromLocationIdZero_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 0, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Source department does not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 8 (Abnormal): FromLocationId = -1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_FromLocationIdNegative_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: -1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Source department does not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 9 (Abnormal): FromLocationId = ToLocationId.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_FromLocationIdEqualsToLocationId_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 2, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Source and destination cannot be the same."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 10 (Abnormal): ToLocationId = 0.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_ToLocationIdZero_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 1, toLocationId: 0);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Destination department does not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 11 (Abnormal): ToLocationId = -1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_ToLocationIdNegative_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 1, toLocationId: -1);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Destination department does not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Test case 12 (Abnormal): TransferDate = today, FromLocationId = 1, ToLocationId = 2.
    /// Expected output: 200 OK (IsSenderConfirmed is not validated during creation)
    /// </summary>
    [Fact]
    public async Task Create_IsSenderConfirmedNotValidated_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: DateTime.UtcNow, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Test case 13 (Abnormal): FromLocationId = -1 and ToLocationId = -1.
    /// Expected output: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_BothLocationIdsInvalid_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: -1, toLocationId: -1);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Source and destination departments do not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Create Additional Edge Cases

    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);

        var result = await _controller.CreateTransferRequest(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_AssetInstanceIdZero_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(assetInstanceId: 0, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Asset instance ID must be greater than 0."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_AssetInstanceIdNegative_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(assetInstanceId: -1, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Asset instance ID must be greater than 0."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_NonExistentAssetInstanceId_ReturnsNotFound()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(assetInstanceId: 999, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new KeyNotFoundException("Asset instance not found."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_AssetNotInUse_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(assetInstanceId: 2, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Asset is not in InUse status."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithoutUserAuthentication_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();
        var dto = MakeCreateDto(fromLocationId: 1, toLocationId: 2);

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Create_NonExistentFromLocationId_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 999, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Source department does not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_NonExistentToLocationId_ReturnsBadRequest()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 1, toLocationId: 999);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ThrowsAsync(new ArgumentException("Destination department does not exist."));

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_NullTransferDate_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var dto = MakeCreateDto(transferDate: null, fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(new CreateTransferResultDTO { AssetRequestId = 10, RecordId = 5, IncompleteDraft = false });

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_UnauthorizedUser_ReturnsUnauthorized()
    {
        SetUserContext(99, isAccountant: true);
        var dto = MakeCreateDto(fromLocationId: 1, toLocationId: 2);
        _mockService.Setup(s => s.CreateAsync(99, dto)).ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.CreateTransferRequest(dto);

        Assert.IsType<ForbidResult>(result);
    }

    #endregion

    #region UpdateDraft Tests

    [Fact]
    public async Task UpdateDraft_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.UpdateIncompleteTransferDraft(1, MakeUpdateDraftBody());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateDraft_ValidData_ReturnsOk()
    {
        SetUserContext(1);
        var body = MakeUpdateDraftBody("{\"key\":\"value\"}");
        _mockService.Setup(s => s.UpdateDraftAsync(1, 1, body)).ReturnsAsync(1);

        var result = await _controller.UpdateIncompleteTransferDraft(1, body);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateDraft_NullBody_ReturnsBadRequest()
    {
        SetUserContext(1);

        var result = await _controller.UpdateIncompleteTransferDraft(1, null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDraft_NonExistentAssetRequestId_ReturnsNotFound()
    {
        SetUserContext(1);
        var body = MakeUpdateDraftBody();
        _mockService.Setup(s => s.UpdateDraftAsync(1, 999, body)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.UpdateIncompleteTransferDraft(999, body);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDraft_UnauthorizedUser_ReturnsForbid()
    {
        SetUserContext(2);
        var body = MakeUpdateDraftBody();
        _mockService.Setup(s => s.UpdateDraftAsync(2, 1, body)).ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.UpdateIncompleteTransferDraft(1, body);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateDraft_ServiceValidationError_ReturnsBadRequest()
    {
        SetUserContext(1);
        var body = MakeUpdateDraftBody();
        _mockService.Setup(s => s.UpdateDraftAsync(1, 1, body)).ThrowsAsync(new ArgumentException("Invalid draft data."));

        var result = await _controller.UpdateIncompleteTransferDraft(1, body);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateDraft_ZeroAssetRequestId_ReturnsBadRequest()
    {
        SetUserContext(1);
        var body = MakeUpdateDraftBody();
        _mockService.Setup(s => s.UpdateDraftAsync(1, 0, body)).ThrowsAsync(new ArgumentException("Asset request ID must be greater than 0."));

        var result = await _controller.UpdateIncompleteTransferDraft(0, body);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_TransferIdZero_ReturnsBadRequest()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 0)).ThrowsAsync(new ArgumentException("Asset request ID must be greater than 0."));

        var result = await _controller.DeleteTransferRequest(0);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ValidTransferId_ReturnsNoContent()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 1)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistentTransferId_ReturnsNotFound()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 999)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DeleteTransferRequest(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_NegativeTransferId_ReturnsNotFound()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, -1)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.DeleteTransferRequest(-1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region Delete Additional Edge Cases

    [Fact]
    public async Task Delete_ByDifferentUser_ReturnsForbidden()
    {
        SetUserContext(2);
        _mockService.Setup(s => s.DeleteAsync(2, 1)).ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_AlreadyApproved_ReturnsBadRequest()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 1)).ThrowsAsync(new ArgumentException("Cannot delete an approved transfer request."));

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_DraftStatus_ReturnsNoContent()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 1)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_SubmittedStatus_ReturnsNoContent()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 1)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_PendingApprovalStatus_ReturnsBadRequest()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.DeleteAsync(1, 1)).ThrowsAsync(new ArgumentException("Cannot delete a transfer request that is pending approval."));

        var result = await _controller.DeleteTransferRequest(1);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region ConfirmSend Tests

    [Fact]
    public async Task ConfirmSend_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.ConfirmSend(1, MakeConfirmBody());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ConfirmSend_ValidId_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var body = MakeConfirmBody("Sent successfully");
        _mockService.Setup(s => s.ConfirmSendAsync(1, true, 1, body)).ReturnsAsync(true);

        var result = await _controller.ConfirmSend(1, body);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ConfirmSend_NonExistentId_ReturnsNotFound()
    {
        SetUserContext(1);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmSendAsync(1, false, 999, body)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.ConfirmSend(999, body);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmSend_UnauthorizedUser_ReturnsForbid()
    {
        SetUserContext(2);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmSendAsync(2, false, 1, body)).ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.ConfirmSend(1, body);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ConfirmSend_ServiceValidationError_ReturnsBadRequest()
    {
        SetUserContext(1);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmSendAsync(1, false, 1, body)).ThrowsAsync(new ArgumentException("Cannot confirm send at this stage."));

        var result = await _controller.ConfirmSend(1, body);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmSend_ZeroId_ReturnsBadRequest()
    {
        SetUserContext(1);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmSendAsync(1, false, 0, body)).ThrowsAsync(new ArgumentException("Invalid transfer ID."));

        var result = await _controller.ConfirmSend(0, body);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region ConfirmReceive Tests

    [Fact]
    public async Task ConfirmReceive_WithoutUserClaim_ReturnsUnauthorized()
    {
        SetUserWithoutClaim();

        var result = await _controller.ConfirmReceive(1, MakeConfirmBody());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ConfirmReceive_ValidId_ReturnsOk()
    {
        SetUserContext(1, isAccountant: true);
        var body = MakeConfirmBody("Received successfully");
        _mockService.Setup(s => s.ConfirmReceiveAsync(1, true, 1, body)).ReturnsAsync(true);

        var result = await _controller.ConfirmReceive(1, body);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ConfirmReceive_NonExistentId_ReturnsNotFound()
    {
        SetUserContext(1);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmReceiveAsync(1, false, 999, body)).ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.ConfirmReceive(999, body);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmReceive_UnauthorizedUser_ReturnsForbid()
    {
        SetUserContext(2);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmReceiveAsync(2, false, 1, body)).ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.ConfirmReceive(1, body);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ConfirmReceive_ServiceValidationError_ReturnsBadRequest()
    {
        SetUserContext(1);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmReceiveAsync(1, false, 1, body)).ThrowsAsync(new ArgumentException("Cannot confirm receive at this stage."));

        var result = await _controller.ConfirmReceive(1, body);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmReceive_ZeroId_ReturnsBadRequest()
    {
        SetUserContext(1);
        var body = MakeConfirmBody();
        _mockService.Setup(s => s.ConfirmReceiveAsync(1, false, 0, body)).ThrowsAsync(new ArgumentException("Invalid transfer ID."));

        var result = await _controller.ConfirmReceive(0, body);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmReceive_NullBody_ReturnsOk()
    {
        SetUserContext(1);
        _mockService.Setup(s => s.ConfirmReceiveAsync(1, false, 1, null)).ReturnsAsync(false);

        var result = await _controller.ConfirmReceive(1, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
