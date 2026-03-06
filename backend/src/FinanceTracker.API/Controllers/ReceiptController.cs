using FinanceTracker.Application.Receipts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
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
}
