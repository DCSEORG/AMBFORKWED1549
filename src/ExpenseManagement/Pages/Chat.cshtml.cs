using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    public void OnGet()
    {
        ViewData["Page"] = "Chat";
        ViewData["Title"] = "Chat Assistant";
    }
}
