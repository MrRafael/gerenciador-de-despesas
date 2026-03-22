using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinBackend.Auth;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class ExpenseCategoryController : ControllerBase
    {
        private readonly FinanceContext _dbContext;

        public ExpenseCategoryController(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }
        

        // GET: api/ExpenseCategory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseCategoryReturnDto>> GetExpenseCategory(int id)
        {
            ExpenseCategory expenseCategory = await _dbContext.ExpenseCategories.FindAsync(id);
            string contextUserId = GetUserIdFromContext();

            if (expenseCategory == null || expenseCategory.UserId != contextUserId)
            {
                return NotFound();
            }

            return new ExpenseCategoryReturnDto { Id = expenseCategory.Id, Name = expenseCategory.Name };
        }

        // GET: api/Users/5/ExpenseCategory
        [HttpGet("/api/Users/{userId}/[controller]")]
        public async Task<ActionResult<List<ExpenseCategoryReturnDto>>> GetExpenseCategoryByUserId(string userId)
        {
            string contextUserId = GetUserIdFromContext();

            if(userId != contextUserId)
            {
                return BadRequest();
            }

            List<ExpenseCategory> expenseCategories = await _dbContext.ExpenseCategories.Where(x=> x.UserId == userId).ToListAsync();
            if (expenseCategories == null || expenseCategories.Count == 0)
            {
                return NotFound();
            }

            return expenseCategories.Select(x => new ExpenseCategoryReturnDto { Id = x.Id, Name = x.Name }).ToList();
        }

        // POST: api/ExpenseCategory
        [HttpPost]
        public async Task<ActionResult<ExpenseCategoryReturnDto>> PostExpenseCategory(ExpenseCategory expenseCategory)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != expenseCategory.UserId)
            {
                return BadRequest();
            }

            if (CategoryExists(expenseCategory))
            {
                return Conflict();
            }
            _dbContext.ExpenseCategories.Add(expenseCategory);

            await _dbContext.SaveChangesAsync();

            return CreatedAtAction("GetExpenseCategory", new { id = expenseCategory.Id }, new ExpenseCategoryReturnDto {Id = expenseCategory.Id, Name = expenseCategory.Name });
        }

        private bool CategoryExists(ExpenseCategory expenseCategory)
        {
            return _dbContext.ExpenseCategories.Any(e => e.Name == expenseCategory.Name && e.UserId == expenseCategory.UserId);
        }

        private string GetUserIdFromContext()
        {
            var mainClaims = User;
            var userId = mainClaims.FindFirstValue("sub");

            return userId;
        }
    }
}
