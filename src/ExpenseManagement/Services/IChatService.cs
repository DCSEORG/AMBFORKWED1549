namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<Dictionary<string, string>> conversationHistory);
}
