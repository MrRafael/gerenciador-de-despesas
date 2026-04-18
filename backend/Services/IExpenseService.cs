using MyFinBackend.Dto;
using MyFinBackend.Model;

namespace MyFinBackend.Services
{
    public interface IExpenseService
    {
        Task<ServiceResult<List<ExpenseReturnDto>>> GetByUserIdAsync(string userId, string contextUserId);
        Task<ServiceResult<List<ExpenseReturnDto>>> GetByDateRangeAsync(string userId, string contextUserId, DateOnly startDate, DateOnly endDate);
        Task<ServiceResult<ExpenseReturnDto>> GetByIdAsync(int id, string contextUserId);
        Task<ServiceResult<ExpenseReturnDto>> CreateWithSplitAsync(CreateExpenseDto dto, string contextUserId);
        Task<ServiceResult<List<ExpenseReturnDto>>> CreateBulkAsync(BulkExpenseToSaveDto bulk, string contextUserId);
        Task<ServiceResult<ExpenseReturnDto>> UpdateGroupAsync(int expenseId, UpdateExpenseGroupDto dto, string contextUserId);
        Task<ServiceResult> DeleteAsync(int expenseId, string contextUserId);
    }
}
