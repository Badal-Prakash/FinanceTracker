using FinanceTracker.Application.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    // ─── Team Controller ──────────────────────────────────────────────────────────
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TeamController : BaseController
    {
        // GET /api/team/stats
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<TeamStatsDto>> GetStats()
            => Ok(await Mediator.Send(new GetTeamStatsQuery()));

        // GET /api/team?search=john&role=Employee&includeInactive=false
        [HttpGet]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<ActionResult<List<TeamMemberDto>>> GetList(
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool includeInactive = false)
            => Ok(await Mediator.Send(
                new GetTeamMembersQuery(search, role, includeInactive)));

        // GET /api/team/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<ActionResult<TeamMemberDto>> GetById(Guid id)
            => Ok(await Mediator.Send(new GetTeamMemberByIdQuery(id)));

        // POST /api/team/invite
        [HttpPost("invite")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Guid>> Invite(
            [FromBody] InviteTeamMemberCommand command)
        {
            var id = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        // PUT /api/team/{id}/role
        [HttpPut("{id:guid}/role")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ChangeRole(
            Guid id, [FromBody] ChangeRoleBody body)
        {
            await Mediator.Send(new ChangeRoleCommand(id, body.Role));
            return NoContent();
        }

        // POST /api/team/{id}/deactivate
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await Mediator.Send(new DeactivateTeamMemberCommand(id));
            return NoContent();
        }

        // POST /api/team/{id}/reactivate
        [HttpPost("{id:guid}/reactivate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            await Mediator.Send(new ReactivateTeamMemberCommand(id));
            return NoContent();
        }

        // POST /api/team/{id}/reset-password
        [HttpPost("{id:guid}/reset-password")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ResetPassword(
            Guid id, [FromBody] ResetPasswordBody body)
        {
            await Mediator.Send(new ResetPasswordCommand(id, body.NewPassword));
            return NoContent();
        }
    }

    public record ChangeRoleBody(string Role);
    public record ResetPasswordBody(string NewPassword);

}