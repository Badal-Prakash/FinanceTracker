using FinanceTracker.Application.Auth.Commands;
using FinanceTracker.Application.Categories;
using FinanceTracker.Application.Dashboard;
using FinanceTracker.Application.Expenses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected ISender Mediator => HttpContext.RequestServices.GetRequiredService<ISender>();
}

// ─── Auth Controller ──────────────────────────────────────────────────────────
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

// ─── Expenses Controller ──────────────────────────────────────────────────────
[Authorize]
public class ExpensesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<PaginatedList<ExpenseListDto>>> GetList(
        [FromQuery] GetExpensesListQuery query)
        => Ok(await Mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseDto>> GetById(Guid id)
        => Ok(await Mediator.Send(new GetExpenseByIdQuery(id)));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateExpenseCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        await Mediator.Send(new SubmitExpenseCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,Admin,SuperAdmin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await Mediator.Send(new ApproveExpenseCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager,Admin,SuperAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectExpenseBody body)
    {
        await Mediator.Send(new RejectExpenseCommand(id, body.Reason));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteExpenseCommand(id));
        return NoContent();
    }
}

// Body record for reject (decouples route id from command)
public record RejectExpenseBody(string Reason);

// ─── Categories Controller ────────────────────────────────────────────────────
[Authorize]
public class CategoriesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
        => Ok(await Mediator.Send(new GetCategoriesQuery()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
        => Ok(await Mediator.Send(new GetCategoryByIdQuery(id)));

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<Guid>> Create(CreateCategoryCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryCommand command)
    {
        await Mediator.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}

// ─── Dashboard Controller ─────────────────────────────────────────────────────
[Authorize]
public class DashboardController : BaseController
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
        => Ok(await Mediator.Send(new GetDashboardStatsQuery()));
}