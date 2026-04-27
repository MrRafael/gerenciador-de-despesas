using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class ExpenseService(FinanceContext db, IMonthCloseService monthClose) : IExpenseService
    {
        public async Task<ServiceResult<List<ExpenseReturnDto>>> GetByUserIdAsync(string userId, string contextUserId)
        {
            if (userId != contextUserId)
                return ServiceResult<List<ExpenseReturnDto>>.Fail(ServiceError.Unauthorized);

            var expenses = await db.Expenses.Include(x => x.Group).Include(x => x.Category).Where(x => x.UserId == userId).ToListAsync();
            return ServiceResult<List<ExpenseReturnDto>>.Ok(expenses.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<List<ExpenseReturnDto>>> GetByDateRangeAsync(string userId, string contextUserId, DateOnly startDate, DateOnly endDate)
        {
            if (userId != contextUserId)
                return ServiceResult<List<ExpenseReturnDto>>.Fail(ServiceError.Unauthorized);

            var expenses = await db.Expenses
                .Include(x => x.Group)
                .Include(x => x.Category)
                .Where(x => x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
                .ToListAsync();

            return ServiceResult<List<ExpenseReturnDto>>.Ok(expenses.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<ExpenseReturnDto>> GetByIdAsync(int id, string contextUserId)
        {
            var expense = await db.Expenses.Include(x => x.Group).Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
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
            await db.Entry(expense).Reference(x => x.Category).LoadAsync();
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

            if (dto.GroupId.HasValue && await monthClose.IsClosedMonthAsync(dto.GroupId.Value, dto.Date.Month, dto.Date.Year))
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.Conflict);

            db.Expenses.Add(expense);
            await db.SaveChangesAsync();

            if (dto.GroupSplitConfigId.HasValue)
            {
                db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig
                {
                    ExpenseId = expense.Id,
                    GroupSplitConfigId = dto.GroupSplitConfigId.Value
                });
                await db.SaveChangesAsync();
            }

            await db.Entry(expense).Reference(x => x.Category).LoadAsync();
            return ServiceResult<ExpenseReturnDto>.Ok(ToDto(expense));
        }

        public async Task<ServiceResult<List<ExpenseReturnDto>>> CreateBulkAsync(BulkExpenseToSaveDto bulk, string contextUserId)
        {
            if (bulk.Expenses.FirstOrDefault()?.UserId != contextUserId)
                return ServiceResult<List<ExpenseReturnDto>>.Fail(ServiceError.Unauthorized);

            db.Expenses.AddRange(bulk.Expenses);
            await db.SaveChangesAsync();
            foreach(var exp in bulk.Expenses)
            {
                await db.Entry(exp).Reference(x => x.Category).LoadAsync();
            }
            return ServiceResult<List<ExpenseReturnDto>>.Ok(bulk.Expenses.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<ExpenseReturnDto>> UpdateGroupAsync(int expenseId, UpdateExpenseGroupDto dto, string contextUserId)
        {
            var expense = await db.Expenses.Include(x => x.Group).Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == expenseId);
            if (expense == null || expense.UserId != contextUserId)
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.NotFound);

            if (expense.GroupId.HasValue && await monthClose.IsClosedMonthAsync(expense.GroupId.Value, expense.Date.Month, expense.Date.Year))
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.Conflict);

            if (dto.GroupId.HasValue && await monthClose.IsClosedMonthAsync(dto.GroupId.Value, expense.Date.Month, expense.Date.Year))
                return ServiceResult<ExpenseReturnDto>.Fail(ServiceError.Conflict);

            expense.GroupId = dto.GroupId;

            var existingSplit = await db.ExpenseSplitConfigs.FindAsync(expenseId);
            if (dto.GroupId == null)
            {
                if (existingSplit != null)
                    db.ExpenseSplitConfigs.Remove(existingSplit);
            }
            else if (dto.GroupSplitConfigId.HasValue)
            {
                if (existingSplit == null)
                    db.ExpenseSplitConfigs.Add(new ExpenseSplitConfig { ExpenseId = expenseId, GroupSplitConfigId = dto.GroupSplitConfigId.Value });
                else
                    existingSplit.GroupSplitConfigId = dto.GroupSplitConfigId.Value;
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

            if (expense.GroupId.HasValue && await monthClose.IsClosedMonthAsync(expense.GroupId.Value, expense.Date.Month, expense.Date.Year))
                return ServiceResult.Fail(ServiceError.Conflict);

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
            GroupName = x.Group?.Name,
            CategoryName = x.Category?.Name
        };
    }
}
