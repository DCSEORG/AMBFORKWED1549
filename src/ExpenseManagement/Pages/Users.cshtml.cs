using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class UsersModel : PageModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<UsersModel> _logger;

    public List<User> Users { get; set; } = new();
    public List<Role> Roles { get; set; } = new();
    public ErrorInfo? Error { get; set; }
    public bool HasError => Error != null;

    public UsersModel(IDatabaseService databaseService, ILogger<UsersModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        ViewData["Page"] = "Users";
        ViewData["Title"] = "Users";

        try
        {
            Users = await _databaseService.GetUsersAsync();
            Roles = await _databaseService.GetRolesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
            
            Error = new ErrorInfo
            {
                Message = ex.Message,
                FileName = "UsersModel.cs",
                LineNumber = 32
            };

            LoadDummyData();
        }
    }

    private void LoadDummyData()
    {
        Users = new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "John Smith",
                Email = "john@example.com",
                RoleName = "Employee",
                IsActive = true
            },
            new User
            {
                UserId = 2,
                UserName = "Jane Doe",
                Email = "jane@example.com",
                RoleName = "Manager",
                IsActive = true
            }
        };

        Roles = new List<Role>
        {
            new Role { RoleId = 1, RoleName = "Employee", Description = "Regular employee" },
            new Role { RoleId = 2, RoleName = "Manager", Description = "Can approve expenses" }
        };
    }
}
