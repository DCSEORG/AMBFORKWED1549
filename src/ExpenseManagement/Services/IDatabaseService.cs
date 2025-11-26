using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IDatabaseService
{
    // Expense operations
    Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<int> CreateExpenseAsync(CreateExpenseRequest request);
    Task<int> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request);
    Task<int> SubmitExpenseAsync(int expenseId);
    Task<int> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<int> RejectExpenseAsync(int expenseId, int reviewerId);
    Task<int> DeleteExpenseAsync(int expenseId);

    // User operations
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<int> CreateUserAsync(CreateUserRequest request);
    Task<int> UpdateUserAsync(int userId, UpdateUserRequest request);

    // Lookup operations
    Task<List<ExpenseCategory>> GetExpenseCategoriesAsync();
    Task<List<ExpenseStatus>> GetExpenseStatusesAsync();
    Task<List<Role>> GetRolesAsync();
}
