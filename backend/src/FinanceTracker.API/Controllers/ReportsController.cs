using FinanceTracker.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
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

}
