using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UsersController : BaseController
    {
        // GET /api/users?search=john&role=Manager&isActive=true
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<List<UserListDto>>> GetList(
            [FromQuery] GetUsersListQuery query)
            => Ok(await Mediator.Send(query));

        // GET /api/users/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<UserDetailDto>> GetById(Guid id)
            => Ok(await Mediator.Send(new GetUserByIdQuery(id)));

        // GET /api/users/me
        [HttpGet("me")]
        public async Task<ActionResult<UserDetailDto>> GetMe()
            => Ok(await Mediator.Send(new GetUserByIdQuery(
                HttpContext.RequestServices
                    .GetRequiredService<ICurrentUserService>().UserId)));

        // POST /api/users/invite
        [HttpPost("invite")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Guid>> Invite([FromBody] InviteUserCommand command)
        {
            var id = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        // PUT /api/users/{id}/role
        [HttpPut("{id:guid}/role")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ChangeRole(
            Guid id, [FromBody] ChangeRoleBody body)
        {
            await Mediator.Send(new ChangeUserRoleCommand(id, body.Role));
            return NoContent();
        }

        // POST /api/users/{id}/deactivate
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await Mediator.Send(new DeactivateUserCommand(id));
            return NoContent();
        }

        // POST /api/users/{id}/reactivate
        [HttpPost("{id:guid}/reactivate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            await Mediator.Send(new ReactivateUserCommand(id));
            return NoContent();
        }

        // PUT /api/users/me/profile
        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }

        // PUT /api/users/me/password
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }
    }

}

public record ChangeRoleBody(string Role);