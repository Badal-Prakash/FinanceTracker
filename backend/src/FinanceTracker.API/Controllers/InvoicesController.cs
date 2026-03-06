using FinanceTracker.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : BaseController
    {
        [HttpGet("stats")]
        public async Task<ActionResult<InvoiceStatsDto>> GetStats()
            => Ok(await Mediator.Send(new GetInvoiceStatsQuery()));

        [HttpGet]
        public async Task<ActionResult<PaginatedList<InvoiceListDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? clientName = null,
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null)
            => Ok(await Mediator.Send(
                new GetInvoicesListQuery(page, pageSize, status, clientName, fromDate, toDate)));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<InvoiceDetailDto>> GetById(Guid id)
            => Ok(await Mediator.Send(new GetInvoiceByIdQuery(id)));

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateInvoiceCommand command)
        {
            var id = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceCommand command)
        {
            // InvoiceId comes from the route, not the body — rebuild the command with it
            var cmd = command with { InvoiceId = id };
            await Mediator.Send(cmd);
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
        public async Task<IActionResult> Delete(Guid id)
        {
            await Mediator.Send(new DeleteInvoiceCommand(id));
            return NoContent();
        }
    }
}