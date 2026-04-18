using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class ExpenseCategoryController(IExpenseCategoryService categoryService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseCategoryReturnDto>> GetExpenseCategory(int id)
        {
            var result = await categoryService.GetByIdAsync(id, GetUserId());
            return result.IsSuccess ? Ok(result.Data) : NotFound();
        }

        [HttpGet("/api/Users/{userId}/[controller]")]
        public async Task<ActionResult<List<ExpenseCategoryReturnDto>>> GetExpenseCategoryByUserId(string userId)
        {
            var result = await categoryService.GetByUserIdAsync(userId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => BadRequest(),
                ServiceError.NotFound => NotFound(),
                _ => Ok(result.Data)
            };
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseCategoryReturnDto>> PostExpenseCategory(ExpenseCategory expenseCategory)
        {
            var result = await categoryService.CreateAsync(expenseCategory, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => BadRequest(),
                ServiceError.Conflict => Conflict(),
                _ => CreatedAtAction("GetExpenseCategory", new { id = result.Data!.Id }, result.Data)
            };
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
