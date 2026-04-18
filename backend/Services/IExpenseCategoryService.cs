using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public interface IExpenseCategoryService
    {
        Task<ServiceResult<ExpenseCategoryReturnDto>> GetByIdAsync(int id, string contextUserId);
        Task<ServiceResult<List<ExpenseCategoryReturnDto>>> GetByUserIdAsync(string userId, string contextUserId);
        Task<ServiceResult<ExpenseCategoryReturnDto>> CreateAsync(ExpenseCategory category, string contextUserId);
    }
}
