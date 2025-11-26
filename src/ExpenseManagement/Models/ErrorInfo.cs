namespace ExpenseManagement.Models;

public class ErrorInfo
{
    public string Message { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public int? LineNumber { get; set; }
    public string? StackTrace { get; set; }
}
