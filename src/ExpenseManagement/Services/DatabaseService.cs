using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services;

public class DatabaseService : IDatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        return new SqlConnection(connectionString);
    }

    // Expense operations
    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetExpenses", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = (object?)userId ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@StatusId", SqlDbType.Int) { Value = (object?)statusId ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.Date) { Value = (object?)fromDate ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.Date) { Value = (object?)toDate ?? DBNull.Value });
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            throw;
        }
        
        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetExpenseById", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", expenseId);
            throw;
        }
        
        return null;
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_CreateExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)Math.Round(request.Amount * 100, MidpointRounding.AwayFromZero));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 1000) { Value = (object?)request.Description ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@ReceiptFile", SqlDbType.NVarChar, 500) { Value = (object?)request.ReceiptFile ?? DBNull.Value });
            
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            throw;
        }
    }

    public async Task<int> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_UpdateExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)Math.Round(request.Amount * 100, MidpointRounding.AwayFromZero));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 1000) { Value = (object?)request.Description ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@ReceiptFile", SqlDbType.NVarChar, 500) { Value = (object?)request.ReceiptFile ?? DBNull.Value });
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_SubmitExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_ApproveExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_RejectExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_DeleteExpense", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    // User operations
    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();
        
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetUsers", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            throw;
        }
        
        return users;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetUserById", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserId", userId);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            throw;
        }
        
        return null;
    }

    public async Task<int> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_CreateUser", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@UserName", request.UserName);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@RoleId", request.RoleId);
            command.Parameters.Add(new SqlParameter("@ManagerId", SqlDbType.Int) { Value = (object?)request.ManagerId ?? DBNull.Value });
            
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    public async Task<int> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_UpdateUser", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@UserName", request.UserName);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@RoleId", request.RoleId);
            command.Parameters.Add(new SqlParameter("@ManagerId", SqlDbType.Int) { Value = (object?)request.ManagerId ?? DBNull.Value });
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    // Lookup operations
    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetExpenseCategories", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense categories");
            throw;
        }
        
        return categories;
    }

    public async Task<List<ExpenseStatus>> GetExpenseStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetExpenseStatuses", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense statuses");
            throw;
        }
        
        return statuses;
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        var roles = new List<Role>();
        
        try
        {
            using var connection = GetConnection();
            using var command = new SqlCommand("sp_GetRoles", connection);
            command.CommandType = CommandType.StoredProcedure;
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    RoleId = reader.GetInt32(0),
                    RoleName = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            throw;
        }
        
        return roles;
    }

    // Helper methods to map database records to models
    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private User MapUser(SqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
            RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
            ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
            ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
