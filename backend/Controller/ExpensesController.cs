using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class ExpensesController(IExpenseService expenseService) : ControllerBase
    {
        [HttpGet("/api/users/{userId}/[controller]")]
        public async Task<ActionResult<List<ExpenseReturnDto>>> GetExpensesByUserId(string userId)
        {
            var result = await expenseService.GetByUserIdAsync(userId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("/api/users/{userId}/[controller]/by-range")]
        public async Task<ActionResult<List<ExpenseReturnDto>>> GetExpensesByDateRange(string userId, DateOnly startDate, DateOnly endDate)
        {
            var result = await expenseService.GetByDateRangeAsync(userId, GetUserId(), startDate, endDate);
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseReturnDto>> GetExpense(int id)
        {
            var result = await expenseService.GetByIdAsync(id, GetUserId());
            return result.IsSuccess ? Ok(result.Data) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseReturnDto>> PostExpense(CreateExpenseDto dto)
        {
            var result = await expenseService.CreateWithSplitAsync(dto, GetUserId());
            if (!result.IsSuccess) return BadRequest();
            return CreatedAtAction(nameof(GetExpense), new { id = result.Data!.Id }, result.Data);
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<List<ExpenseReturnDto>>> PostBulkExpense(BulkExpenseToSaveDto bulk)
        {
            var result = await expenseService.CreateBulkAsync(bulk, GetUserId());
            if (!result.IsSuccess) return BadRequest();
            return StatusCode(StatusCodes.Status201Created, result.Data);
        }

        [HttpDelete("{expenseId}")]
        public async Task<ActionResult> DeleteExpense(int expenseId)
        {
            var result = await expenseService.DeleteAsync(expenseId, GetUserId());
            return result.IsSuccess ? NoContent() : Forbid();
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
