using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Dtos;
using FinanceTracker.Application.Expenses;
using FinanceTracker.Application.Expenses.Commands.ApproveExpense;
using FinanceTracker.Application.Expenses.Commands.CreateExpense;
using FinanceTracker.Application.Expenses.Commands.DeleteExpense;
using FinanceTracker.Application.Expenses.Commands.RejectExpense;
using FinanceTracker.Application.Expenses.Commands.SubmitExpense;
using FinanceTracker.Application.Expenses.Queries.GetExpenseById;
using FinanceTracker.Application.Expenses.Queries.GetExpensesList;
using FinanceTracker.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

}
