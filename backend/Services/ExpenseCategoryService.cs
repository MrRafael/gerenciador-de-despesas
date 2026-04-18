using Microsoft.EntityFrameworkCore;
using MyFinBackend.Database;
using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public class ExpenseCategoryService(FinanceContext db) : IExpenseCategoryService
    {
        public async Task<ServiceResult<ExpenseCategoryReturnDto>> GetByIdAsync(int id, string contextUserId)
        {
            var category = await db.ExpenseCategories.FindAsync(id);
            if (category == null || category.UserId != contextUserId)
                return ServiceResult<ExpenseCategoryReturnDto>.Fail(ServiceError.NotFound);

            return ServiceResult<ExpenseCategoryReturnDto>.Ok(new ExpenseCategoryReturnDto { Id = category.Id, Name = category.Name });
        }

        public async Task<ServiceResult<List<ExpenseCategoryReturnDto>>> GetByUserIdAsync(string userId, string contextUserId)
        {
            if (userId != contextUserId)
                return ServiceResult<List<ExpenseCategoryReturnDto>>.Fail(ServiceError.Unauthorized);

            var categories = await db.ExpenseCategories.Where(x => x.UserId == userId).ToListAsync();
            if (categories.Count == 0)
                return ServiceResult<List<ExpenseCategoryReturnDto>>.Fail(ServiceError.NotFound);

            return ServiceResult<List<ExpenseCategoryReturnDto>>.Ok(
                categories.Select(x => new ExpenseCategoryReturnDto { Id = x.Id, Name = x.Name }).ToList());
        }

        public async Task<ServiceResult<ExpenseCategoryReturnDto>> CreateAsync(ExpenseCategory category, string contextUserId)
        {
            if (category.UserId != contextUserId)
                return ServiceResult<ExpenseCategoryReturnDto>.Fail(ServiceError.Unauthorized);

            var exists = await db.ExpenseCategories
                .AnyAsync(e => e.Name == category.Name && e.UserId == category.UserId);

            if (exists)
                return ServiceResult<ExpenseCategoryReturnDto>.Fail(ServiceError.Conflict);

            db.ExpenseCategories.Add(category);
            await db.SaveChangesAsync();

            return ServiceResult<ExpenseCategoryReturnDto>.Ok(new ExpenseCategoryReturnDto { Id = category.Id, Name = category.Name });
        }
    }
}
