namespace ExpenseManagement.Services;

public class DummyChatService : IChatService
{
    public Task<string> GetChatResponseAsync(string userMessage, List<Dictionary<string, string>> conversationHistory)
    {
        var response = @"👋 **GenAI Services Not Deployed**

The GenAI chat functionality requires Azure OpenAI and Azure AI Search services to be deployed.

**To enable chat functionality:**

1. Run the `deploy-with-chat.sh` script instead of `deploy.sh`
2. This will deploy:
   - Azure OpenAI with GPT-4o model
   - Azure AI Search for RAG capabilities
   - Proper managed identity configurations

**What you can do now:**

- ✅ View and manage expenses
- ✅ Create and edit users
- ✅ Use the REST APIs (see /swagger)
- ✅ Test the application functionality

Once GenAI services are deployed, you'll be able to:
- 💬 Chat with an AI assistant about your expenses
- 📊 Get insights and summaries
- 🔍 Search and query data using natural language

**Your message was:** " + userMessage;

        return Task.FromResult(response);
    }
}
