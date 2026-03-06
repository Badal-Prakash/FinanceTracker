using FinanceTracker.Application.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : BaseController
    {
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterTenantCommand command)
            => Ok(await Mediator.Send(command));

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginCommand command)
            => Ok(await Mediator.Send(command));

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenCommand command)
            => Ok(await Mediator.Send(command));
    }
}
