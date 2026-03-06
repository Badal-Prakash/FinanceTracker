using FinanceTracker.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : BaseController
    {
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
            => Ok(await Mediator.Send(new GetDashboardStatsQuery()));
    }
}
