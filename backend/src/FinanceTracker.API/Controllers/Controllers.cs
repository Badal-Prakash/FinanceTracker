using FinanceTracker.Application.Auth.Commands;
using FinanceTracker.Application.Budgets;
using FinanceTracker.Application.Categories;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Dashboard;
using FinanceTracker.Application.Expenses;
using FinanceTracker.Application.Invoices;
using FinanceTracker.Application.Reports;
using FinanceTracker.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Receipts;

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

// ─── Invoices Controller ──────────────────────────────────────────────────────
[Authorize]
public class InvoicesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<PaginatedInvoiceList>> GetList(
        [FromQuery] GetInvoicesListQuery query)
        => Ok(await Mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
        => Ok(await Mediator.Send(new GetInvoiceByIdQuery(id)));

    [HttpGet("stats")]
    public async Task<ActionResult<InvoiceStatsDto>> GetStats()
        => Ok(await Mediator.Send(new GetInvoiceStatsQuery()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateInvoiceCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateInvoiceCommand command)
    {
        await Mediator.Send(command with { Id = id });
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id)
    {
        await Mediator.Send(new SendInvoiceCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id)
    {
        await Mediator.Send(new MarkInvoicePaidCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await Mediator.Send(new CancelInvoiceCommand(id));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager,Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteInvoiceCommand(id));
        return NoContent();
    }
}


// Add this controller to Controllers.cs
// Also add: using FinanceTracker.Application.Budgets;

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

    // POST /api/budgets  (upsert: create or update)
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<Guid>> Set([FromBody] SetBudgetCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(id);
    }

    // POST /api/budgets/copy
    [HttpPost("copy")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<ActionResult<int>> Copy([FromBody] CopyBudgetsCommand command)
    {
        var count = await Mediator.Send(command);
        return Ok(count);
    }

    // DELETE /api/budgets/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteBudgetCommand(id));
        return NoContent();
    }
}


// Add to Controllers.cs
// Also add: using FinanceTracker.Application.Users;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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

public record ChangeRoleBody(string Role);


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : BaseController
{
    [HttpGet("expenses")]
    public async Task<ActionResult<ExpenseReportDto>> GetExpenseReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? userId)
    {
        var filters = new ReportFilters(fromDate, toDate, status, categoryId, userId);
        return Ok(await Mediator.Send(new GetExpenseReportQuery(filters)));
    }

    // GET /api/reports/expenses/csv?fromDate=...
    [HttpGet("expenses/csv")]
    public async Task<IActionResult> ExpensesCsv(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? userId)
    {
        var filters = new ReportFilters(fromDate, toDate, status, categoryId, userId);
        var bytes = await Mediator.Send(new ExportExpensesCsvQuery(filters));
        var fileName = $"expenses_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    // GET /api/reports/budget/csv?month=3&year=2026
    [HttpGet("budget/csv")]
    public async Task<IActionResult> BudgetCsv(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var bytes = await Mediator.Send(new ExportBudgetCsvQuery(month, year));
        var fileName = $"budget_{year}_{month:D2}.csv";
        return File(bytes, "text/csv", fileName);
    }
}

// Add to Controllers.cs
// Also add: using FinanceTracker.Application.Receipts;

[Authorize]
[ApiController]
[Route("api/expenses/{expenseId:guid}/receipt")]
public class ReceiptController : BaseController
{
    // POST /api/expenses/{expenseId}/receipt
    // Content-Type: multipart/form-data
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]  // 10 MB
    public async Task<ActionResult<ReceiptUploadResultDto>> Upload(
        Guid expenseId,
        IFormFile file)
    {
        var result = await Mediator.Send(
            new UploadReceiptCommand(expenseId, file));
        return Ok(result);
    }

    // DELETE /api/expenses/{expenseId}/receipt
    [HttpDelete]
    public async Task<IActionResult> Remove(Guid expenseId)
    {
        await Mediator.Send(new RemoveReceiptCommand(expenseId));
        return NoContent();
    }
}