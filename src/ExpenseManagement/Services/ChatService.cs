using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;
using System.ClientModel;

namespace ExpenseManagement.Services;

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IDatabaseService _databaseService;
    private OpenAIClient? _openAIClient;
    private string? _deploymentName;

    public ChatService(
        IConfiguration configuration,
        ILogger<ChatService> logger,
        IDatabaseService databaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _databaseService = databaseService;
        InitializeOpenAIClient();
    }

    private void InitializeOpenAIClient()
    {
        try
        {
            var endpoint = _configuration["OpenAI__Endpoint"];
            _deploymentName = _configuration["OpenAI__DeploymentName"];
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(_deploymentName))
            {
                _logger.LogWarning("OpenAI configuration is missing");
                return;
            }

            TokenCredential credential;
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            _openAIClient = new OpenAIClient(new Uri(endpoint), credential);
            _logger.LogInformation("OpenAI client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI client");
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<Dictionary<string, string>> conversationHistory)
    {
        if (_openAIClient == null || string.IsNullOrEmpty(_deploymentName))
        {
            return "⚠️ OpenAI service is not properly configured. Please check the deployment settings.";
        }

        try
        {
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(GetSystemPrompt())
            };

            // Add conversation history
            foreach (var msg in conversationHistory)
            {
                if (msg["role"] == "user")
                {
                    messages.Add(new ChatRequestUserMessage(msg["content"]));
                }
                else if (msg["role"] == "assistant")
                {
                    messages.Add(new ChatRequestAssistantMessage(msg["content"]));
                }
            }

            // Add current user message
            messages.Add(new ChatRequestUserMessage(userMessage));

            // Define function tools
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                MaxTokens = 1500,
                Temperature = 0.7f
            };

            foreach (var message in messages)
            {
                chatOptions.Messages.Add(message);
            }

            // Add function definitions
            chatOptions.Tools.Add(GetExpensesFunction());
            chatOptions.Tools.Add(CreateExpenseFunction());
            chatOptions.Tools.Add(GetUsersFunction());
            chatOptions.Tools.Add(GetCategoriesFunction());

            // Get response
            var response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
            var responseMessage = response.Value.Choices[0].Message;

            // Check if the model wants to call functions
            if (responseMessage.ToolCalls != null && responseMessage.ToolCalls.Count > 0)
            {
                // Process function calls
                messages.Add(new ChatRequestAssistantMessage(responseMessage));

                foreach (var toolCall in responseMessage.ToolCalls)
                {
                    if (toolCall is ChatCompletionsFunctionToolCall functionToolCall)
                    {
                        var functionResult = await ExecuteFunctionAsync(
                            functionToolCall.Name,
                            functionToolCall.Arguments);

                        messages.Add(new ChatRequestToolMessage(functionResult, functionToolCall.Id));
                    }
                }

                // Get final response after function execution
                var finalOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    MaxTokens = 1500,
                    Temperature = 0.7f
                };

                foreach (var message in messages)
                {
                    finalOptions.Messages.Add(message);
                }

                var finalResponse = await _openAIClient.GetChatCompletionsAsync(finalOptions);
                return finalResponse.Value.Choices[0].Message.Content;
            }

            return responseMessage.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"⚠️ Error: {ex.Message}";
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI assistant for an Expense Management System. You have access to real functions that can:

1. **get_expenses** - Retrieve expense records with optional filters
2. **create_expense** - Create new expense records
3. **get_users** - Get list of users in the system
4. **get_categories** - Get available expense categories

When users ask about expenses, users, or want to create expenses, use these functions to provide accurate, real-time data.

When displaying lists:
- Format numbers as currency (£X.XX)
- Use clear, readable formatting with bullet points or numbered lists
- Bold important information using **text**
- Keep responses concise but informative

Always be helpful, professional, and accurate with financial data.";
    }

    private ChatCompletionsFunctionToolDefinition GetExpensesFunction()
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = "get_expenses",
            Description = "Retrieves expense records from the database with optional filtering by user ID, status ID, or date range",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""userId"": {
                        ""type"": ""integer"",
                        ""description"": ""Optional user ID to filter expenses""
                    },
                    ""statusId"": {
                        ""type"": ""integer"",
                        ""description"": ""Optional status ID (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)""
                    },
                    ""fromDate"": {
                        ""type"": ""string"",
                        ""description"": ""Optional start date in ISO format (yyyy-MM-dd)""
                    },
                    ""toDate"": {
                        ""type"": ""string"",
                        ""description"": ""Optional end date in ISO format (yyyy-MM-dd)""
                    }
                }
            }")
        };
    }

    private ChatCompletionsFunctionToolDefinition CreateExpenseFunction()
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = "create_expense",
            Description = "Creates a new expense record in the database",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""userId"": {
                        ""type"": ""integer"",
                        ""description"": ""User ID who owns this expense""
                    },
                    ""categoryId"": {
                        ""type"": ""integer"",
                        ""description"": ""Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)""
                    },
                    ""amount"": {
                        ""type"": ""number"",
                        ""description"": ""Expense amount in GBP""
                    },
                    ""expenseDate"": {
                        ""type"": ""string"",
                        ""description"": ""Date of expense in ISO format (yyyy-MM-dd)""
                    },
                    ""description"": {
                        ""type"": ""string"",
                        ""description"": ""Optional description of the expense""
                    }
                },
                ""required"": [""userId"", ""categoryId"", ""amount"", ""expenseDate""]
            }")
        };
    }

    private ChatCompletionsFunctionToolDefinition GetUsersFunction()
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = "get_users",
            Description = "Retrieves the list of all active users in the system",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {}
            }")
        };
    }

    private ChatCompletionsFunctionToolDefinition GetCategoriesFunction()
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = "get_categories",
            Description = "Retrieves the list of available expense categories",
            Parameters = BinaryData.FromString(@"{
                ""type"": ""object"",
                ""properties"": {}
            }")
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        _logger.LogInformation("Executing function: {FunctionName} with args: {Args}", functionName, argumentsJson);

        try
        {
            switch (functionName)
            {
                case "get_expenses":
                    return await ExecuteGetExpensesAsync(argumentsJson);

                case "create_expense":
                    return await ExecuteCreateExpenseAsync(argumentsJson);

                case "get_users":
                    return await ExecuteGetUsersAsync();

                case "get_categories":
                    return await ExecuteGetCategoriesAsync();

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> ExecuteGetExpensesAsync(string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
        
        int? userId = args != null && args.ContainsKey("userId") ? args["userId"].GetInt32() : null;
        int? statusId = args != null && args.ContainsKey("statusId") ? args["statusId"].GetInt32() : null;
        DateTime? fromDate = args != null && args.ContainsKey("fromDate") ? DateTime.Parse(args["fromDate"].GetString()!) : null;
        DateTime? toDate = args != null && args.ContainsKey("toDate") ? DateTime.Parse(args["toDate"].GetString()!) : null;

        var expenses = await _databaseService.GetExpensesAsync(userId, statusId, fromDate, toDate);
        
        return JsonSerializer.Serialize(new
        {
            count = expenses.Count,
            expenses = expenses.Select(e => new
            {
                e.ExpenseId,
                e.UserName,
                e.CategoryName,
                e.StatusName,
                Amount = $"£{e.Amount:N2}",
                e.ExpenseDate,
                e.Description
            })
        });
    }

    private async Task<string> ExecuteCreateExpenseAsync(string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
        
        if (args == null)
        {
            return JsonSerializer.Serialize(new { error = "Invalid arguments" });
        }

        var request = new CreateExpenseRequest
        {
            UserId = args["userId"].GetInt32(),
            CategoryId = args["categoryId"].GetInt32(),
            Amount = (decimal)args["amount"].GetDouble(),
            ExpenseDate = DateTime.Parse(args["expenseDate"].GetString()!),
            Description = args.ContainsKey("description") ? args["description"].GetString() : null,
            Currency = "GBP"
        };

        var expenseId = await _databaseService.CreateExpenseAsync(request);
        
        return JsonSerializer.Serialize(new
        {
            success = true,
            expenseId,
            message = "Expense created successfully"
        });
    }

    private async Task<string> ExecuteGetUsersAsync()
    {
        var users = await _databaseService.GetUsersAsync();
        
        return JsonSerializer.Serialize(new
        {
            count = users.Count,
            users = users.Select(u => new
            {
                u.UserId,
                u.UserName,
                u.Email,
                u.RoleName
            })
        });
    }

    private async Task<string> ExecuteGetCategoriesAsync()
    {
        var categories = await _databaseService.GetExpenseCategoriesAsync();
        
        return JsonSerializer.Serialize(new
        {
            count = categories.Count,
            categories = categories.Select(c => new
            {
                c.CategoryId,
                c.CategoryName
            })
        });
    }
}
