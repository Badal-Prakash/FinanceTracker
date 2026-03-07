using FinanceTracker.Application.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : BaseController
    {
        // GET /api/auditlogs?entityName=Expense&action=Updated&page=1&pageSize=30
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<AuditLogPageDto>> GetList(
            [FromQuery] string? entityName = null,
            [FromQuery] Guid? entityId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
            => Ok(await Mediator.Send(
                new GetAuditLogsQuery(entityName, entityId, userId, action, from, to, page, pageSize)));

        // GET /api/auditlogs/{entityName}/{entityId}  — full history for one record
        [HttpGet("{entityName}/{entityId:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,Manager")]
        public async Task<ActionResult<List<AuditLogDto>>> GetEntityHistory(
            string entityName, Guid entityId)
            => Ok(await Mediator.Send(new GetEntityAuditHistoryQuery(entityName, entityId)));
    }
}