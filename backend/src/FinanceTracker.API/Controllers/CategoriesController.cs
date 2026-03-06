using FinanceTracker.Application.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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


}
