using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense> RecentExpenses { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public ErrorInfo? Error { get; set; }
    public bool HasError => Error != null;

    public IndexModel(IDatabaseService databaseService, ILogger<IndexModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        ViewData["Page"] = "Index";
        ViewData["Title"] = "Dashboard";

        try
        {
            // Load recent expenses
            var allExpenses = await _databaseService.GetExpensesAsync();
            RecentExpenses = allExpenses.Take(10).ToList();
            
            // Load users
            Users = await _databaseService.GetUsersAsync();
            
            // Load categories
            Categories = await _databaseService.GetExpenseCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            
            // Set error information
            Error = new ErrorInfo
            {
                Message = ex.Message,
                FileName = "IndexModel.cs",
                LineNumber = 35
            };

            // Load dummy data for display
            LoadDummyData();
        }
    }

    private void LoadDummyData()
    {
        RecentExpenses = new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserName = "John Smith",
                CategoryName = "Travel",
                StatusName = "Submitted",
                AmountMinor = 12500,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-2),
                Description = "Sample expense - Database not connected"
            },
            new Expense
            {
                ExpenseId = 2,
                UserName = "Jane Doe",
                CategoryName = "Meals",
                StatusName = "Approved",
                AmountMinor = 3450,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-5),
                Description = "Sample expense - Database not connected"
            }
        };

        Users = new List<User>
        {
            new User { UserId = 1, UserName = "John Smith", Email = "john@example.com", RoleName = "Employee" },
            new User { UserId = 2, UserName = "Jane Doe", Email = "jane@example.com", RoleName = "Manager" }
        };

        Categories = new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel" },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals" },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies" }
        };
    }
}
