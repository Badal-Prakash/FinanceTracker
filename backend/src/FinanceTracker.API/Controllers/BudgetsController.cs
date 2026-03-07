using FinanceTracker.Application.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetsController : BaseController
    {
        // GET /api/budgets/summary?month=3&year=2026
        [HttpGet("summary")]
        public async Task<ActionResult<BudgetSummaryDto>> GetSummary(
            [FromQuery] int? month, [FromQuery] int? year)
            => Ok(await Mediator.Send(new GetBudgetSummaryQuery(month, year)));

        // GET /api/budgets/trend?months=6
        [HttpGet("trend")]
        public async Task<ActionResult<List<BudgetTrendDto>>> GetTrend(
            [FromQuery] int months = 6)
            => Ok(await Mediator.Send(new GetBudgetTrendQuery(months)));

        // POST /api/budgets  (upsert — creates or updates for the given month/category)
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin,Manager")]
        public async Task<ActionResult<Guid>> Set([FromBody] SetBudgetCommand command)
            => Ok(await Mediator.Send(command));

        // POST /api/budgets/copy
        [HttpPost("copy")]
        [Authorize(Roles = "Admin,SuperAdmin,Manager")]
        public async Task<ActionResult<int>> Copy([FromBody] CopyBudgetsCommand command)
            => Ok(await Mediator.Send(command));

        // DELETE /api/budgets/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,Manager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteBudgetCommand(id));
            return NoContent();
        }
    }
}