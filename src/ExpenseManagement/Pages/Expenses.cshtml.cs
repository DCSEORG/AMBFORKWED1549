using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ExpensesModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    public ErrorInfo? Error { get; set; }
    public bool HasError => Error != null;

    public ExpensesModel(IDatabaseService databaseService, ILogger<ExpensesModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        ViewData["Page"] = "Expenses";
        ViewData["Title"] = "Expenses";

        try
        {
            Expenses = await _databaseService.GetExpensesAsync();
            Users = await _databaseService.GetUsersAsync();
            Statuses = await _databaseService.GetExpenseStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            
            Error = new ErrorInfo
            {
                Message = ex.Message,
                FileName = "ExpensesModel.cs",
                LineNumber = 32
            };

            LoadDummyData();
        }
    }

    private void LoadDummyData()
    {
        Expenses = new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "John Smith",
                CategoryName = "Travel",
                StatusName = "Draft",
                AmountMinor = 12500,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-2),
                Description = "Sample expense - Database not connected"
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 2,
                UserName = "Jane Doe",
                CategoryName = "Meals",
                StatusName = "Submitted",
                AmountMinor = 3450,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-5),
                Description = "Sample expense - Database not connected"
            }
        };

        Users = new List<User>
        {
            new User { UserId = 1, UserName = "John Smith", Email = "john@example.com" },
            new User { UserId = 2, UserName = "Jane Doe", Email = "jane@example.com" }
        };

        Statuses = new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }
}
