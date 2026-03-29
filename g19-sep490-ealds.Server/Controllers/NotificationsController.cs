using System.Security.Claims;
using g19_sep490_ealds.Server.DTOs.Notifications;
using g19_sep490_ealds.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public NotificationsController(EaldsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Current user's notifications (inventory and other modules), newest first.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationListItemDto>>> GetMine(
        [FromQuery] int take = 200)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        take = Math.Clamp(take, 1, 500);

        var list = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentDate)
            .Take(take)
            .Select(n => new NotificationListItemDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Content = n.Content,
                RefId = n.RefId,
                SentDate = n.SentDate,
                IsSend = n.IsSend
            })
            .ToListAsync();

        return Ok(list);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId) && userId > 0;
    }
}
