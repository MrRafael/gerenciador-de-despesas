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
    public class ExpensesController : ControllerBase
    {
        private readonly FinanceContext _dbContext;

        public ExpensesController(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Users/5/Expenses
        [HttpGet("/api/Users/{userId}/[controller]")]
        public async Task<ActionResult<List<ExpenseReturnDto>>> GetExpensesByUserId(string userId)
        {
            string contextUserId = GetUserIdFromContext();

            if (userId != contextUserId)
            {
                return BadRequest();
            }

            List<Expense> expense = await _dbContext.Expenses.Where(x => x.UserId == userId).ToListAsync();
            if (expense == null || expense.Count == 0)
            {
                return NotFound();
            }

            return expense.Select(x => new ExpenseReturnDto
            {
                Id = x.Id,
                Description = x.Description,
                Date = x.Date,
                Value = x.Value,
                CategoryId = x.CategoryId
            }).ToList();
        }

        [HttpGet("/api/Users/{userId}/[controller]/by-range")]
        public async Task<ActionResult<List<ExpenseReturnDto>>> GetExpensesByDateRange(string userId, DateOnly startDate, DateOnly endDate)
        {
            string contextUserId = GetUserIdFromContext();

            if (userId != contextUserId)
            {
                return BadRequest();
            }

            List<Expense> expense = await _dbContext.Expenses.Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate).ToListAsync();
            if (expense == null || expense.Count == 0)
            {
                return NotFound();
            }

            return expense.Select(x => new ExpenseReturnDto
            {
                Id = x.Id,
                Description = x.Description,
                Date = x.Date,
                Value = x.Value,
                CategoryId = x.CategoryId
            }).ToList();
        }

        // GET: api/Expenses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseReturnDto>> GetExpense(int id)
        {
            Expense expense = await _dbContext.Expenses.FindAsync(id);
            string contextUserId = GetUserIdFromContext();

            if (expense == null || expense.UserId != contextUserId)
            {
                return NotFound();
            }

            return new ExpenseReturnDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Date = expense.Date,
                Value = expense.Value,
                CategoryId = expense.CategoryId
            };
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseReturnDto>> PostExpense(Expense expense)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != expense.UserId)
            {
                return BadRequest();
            }

            _dbContext.Expenses.Add(expense);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction("GetExpense", new { id = expense.Id }, new ExpenseReturnDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Date = expense.Date,
                Value = expense.Value,
                CategoryId = expense.CategoryId
            });
        }

        [HttpPost("PostBulkExpense")]
        public async Task<ActionResult<List<ExpenseReturnDto>>> PostBulkExpense(BulkExpenseToSaveDto bulkexpenses)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != bulkexpenses.Expenses.FirstOrDefault()?.UserId)
            {
                return BadRequest();
            }

            _dbContext.Expenses.AddRange(bulkexpenses.Expenses);
            await _dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created, bulkexpenses.Expenses.Select(x => new ExpenseReturnDto
            {
                CategoryId = x.CategoryId,
                Date = x.Date,
                Description = x.Description,
                Id = x.Id,
                Value = x.Value
            }));
        }

        [HttpDelete("{expenseId}")]
        public async Task<ActionResult> deleteExpense(int expenseId)
        {
            var expense = await _dbContext.Expenses.FindAsync(expenseId);
            var contextUserId = GetUserIdFromContext();
            if (expense == null || contextUserId != expense.UserId)
            {
                return BadRequest();
            }

            _dbContext.Expenses.Remove(expense);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }

        private string GetUserIdFromContext()
        {
            var mainClaims = User;
            var userId = mainClaims.FindFirstValue("sub");

            return userId;
        }
    }
}
