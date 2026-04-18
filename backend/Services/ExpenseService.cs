using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class ExpenseService(FinanceContext db) : IExpenseService
    {
        public async Task<ServiceResult<List<ExpenseReturnDto>>> GetByUserIdAsync(string userId, string contextUserId)
        {
            if (userId != contextUserId)
                return ServiceResult<List<ExpenseReturnDto>>.Fail(ServiceError.Unauthorized);

            var expenses = await db.Expenses.Include(x => x.Group).Where(x => x.UserId == userId).ToListAsync();
            return ServiceResult<List<ExpenseReturnDto>>.Ok(expenses.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<List<ExpenseReturnDto>>> GetByDateRangeAsync(string userId, string contextUserId, DateOnly startDate, DateOnly endDate)
        {
            if (userId != contextUserId)
                return ServiceResult<List<ExpenseReturnDto>>.Fail(ServiceError.Unauthorized);

            var expenses = await db.Expenses
                .Include(x => x.Group)
                .Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
                .ToListAsync();

            return ServiceResult<List<ExpenseReturnDto>>.Ok(expenses.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<ExpenseReturnDto>> GetByIdAsync(int id, string contextUserId)
        {
            var expense = await db.Expenses.Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == id);
            if (expense == null || expense.UserId != contextUserId)
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.NotFound);

            return ServiceResult<ExpenseReturnDto>.Ok(ToDto(expense));
        }

        public async Task<ServiceResult<ExpenseReturnDto>> CreateAsync(Expense expense, string contextUserId)
        {
            if (expense.UserId != contextUserId)
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.Unauthorized);

            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            return ServiceResult<ExpenseReturnDto>.Ok(ToDto(expense));
        }

        public async Task<ServiceResult<ExpenseReturnDto>> CreateWithSplitAsync(CreateExpenseDto dto, string contextUserId)
        {
            if (dto.UserId != contextUserId)
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.Unauthorized);

            var expense = new Expense
            {
                Description = dto.Description,
                Value = dto.Value,
                Date = dto.Date,
                UserId = dto.UserId,
                CategoryId = dto.CategoryId,
                GroupId = dto.GroupId
            };

            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            if (dto.SplitType.HasValue)
            {
                db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig
                {
                    ExpenseId = expense.Id,
                    SplitType = dto.SplitType.Value
                });
                await db.SaveChangesAsync();
            }

            return ServiceResult<ExpenseReturnDto>.Ok(ToDto(expense));
        }

        public async Task<ServiceResult<List<ExpenseReturnDto>>> CreateBulkAsync(BulkExpenseToSaveDto bulk, string contextUserId)
        {
            if (bulk.Expenses.FirstOrDefault()?.UserId != contextUserId)
                return ServiceResult<List<ExpenseReturnDto>>.Fail(ServiceError.Unauthorized);

            db.Expenses.AddRange(bulk.Expenses);
            await db.SaveChangesAsync();
            return ServiceResult<List<ExpenseReturnDto>>.Ok(bulk.Expenses.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<ExpenseReturnDto>> UpdateGroupAsync(int expenseId, UpdateExpenseGroupDto dto, string contextUserId)
        {
            var expense = await db.Expenses.Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == expenseId);
            if (expense == null || expense.UserId != contextUserId)
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.NotFound);

            expense.GroupId = dto.GroupId;

            var existingSplit = await db.ExpenseSplitConfigs.FindAsync(expenseId);
            if (dto.GroupId == null)
            {
                if (existingSplit != null)
                    db.ExpenseSplitConfigs.Remove(existingSplit);
            }
            else if (dto.SplitType.HasValue)
            {
                if (existingSplit == null)
                    db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { ExpenseId = expenseId, SplitType = dto.SplitType.Value });
                else
                    existingSplit.SplitType = dto.SplitType.Value;
            }

            await db.SaveChangesAsync();
            await db.Entry(expense).Reference(x => x.Group).LoadAsync();
            return ServiceResult<ExpenseReturnDto>.Ok(ToDto(expense));
        }

        public async Task<ServiceResult> DeleteAsync(int expenseId, string contextUserId)
        {
            var expense = await db.Expenses.FindAsync(expenseId);
            if (expense == null || expense.UserId != contextUserId)
                return ServiceResult.Fail(ServiceError.Unauthorized);

            db.Expenses.Remove(expense);
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private static ExpenseReturnDto ToDto(Expense x) => new()
        {
            Id = x.Id,
            Description = x.Description,
            Date = x.Date,
            Value = x.Value,
            CategoryId = x.CategoryId,
            GroupId = x.GroupId,
            GroupName = x.Group?.Name
        };
    }
}
