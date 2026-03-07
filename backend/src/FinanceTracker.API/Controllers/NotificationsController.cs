using FinanceTracker.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : BaseController
    {
        // GET /api/notifications/summary  — unread count + last 10 (for bell dropdown)
        [HttpGet("summary")]
        public async Task<ActionResult<NotificationSummaryDto>> GetSummary()
            => Ok(await Mediator.Send(new GetNotificationSummaryQuery()));

        // GET /api/notifications?unreadOnly=false&page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetList(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => Ok(await Mediator.Send(
                new GetNotificationsQuery(unreadOnly, page, pageSize)));

        // POST /api/notifications/{id}/read
        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await Mediator.Send(new MarkNotificationReadCommand(id));
            return NoContent();
        }

        // POST /api/notifications/read-all
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            await Mediator.Send(new MarkAllNotificationsReadCommand());
            return NoContent();
        }

        // DELETE /api/notifications/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteNotificationCommand(id));
            return NoContent();
        }
    }
}






















