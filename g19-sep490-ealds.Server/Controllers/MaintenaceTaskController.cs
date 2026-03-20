using g19_sep490_ealds.Server.DTO.RequestDTO.AssetMaintenance.MaintenanceTask;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace g19_sep490_ealds.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MaintenaceTaskController : ControllerBase
{
    private readonly IMaintenanceTaskService _service;

    public MaintenaceTaskController(IMaintenanceTaskService service)
    {
        _service = service;
    }

    [HttpPost("complete/{taskId}")]
    public async Task<IActionResult> CompleteTask(int taskId, [FromBody] CompleteTaskDTO dto)
    {
        try
        {
            //authen thay vào nhé
            var userId = 1; // GetUserId();
            var roleId = 1;//GetRoleId()

            await _service.CompleteTaskAsync(taskId, roleId, userId, dto);

            return Ok("Task completed");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.ToString());
        }
    }

    [HttpPost("start/{taskId}")]
    public async Task<IActionResult> StartTask(int taskId)
    {
        try
        {
            //authen thay vào nhé
            var userId = 1; // GetUserId();
            var roleId = 1;//GetRoleId()
            await _service.StartTaskAsync(taskId, userId, roleId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.ToString());
        }
    }
}