using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", async () =>
{
    string apiKey = Environment.GetEnvironmentVariable("OpenAPIKey");
    var client = new OpenAIClient(new ApiKeyCredential(apiKey));

    var model = "gpt-5-nano"; // or your preferred model
    var chat = new ChatClient(model, apiKey);

    // 1) Define the tool
    var tool = ChatTool.CreateFunctionTool(
        functionName: "get_random_token",
        functionDescription: "Return a fresh random token string.",
        functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": { },
              "additionalProperties": false
            }
            """)
    );

    // 2) System + user messages
    var systemPrompt =
        "You are a helper that must ALWAYS call the tool `get_random_token` first. " +
        "Never answer directly without calling that tool. " +
        "After receiving the tool result, reply with exactly:\n" +
        "\"RANDOM_TOKEN: <the token string>\"";

    var messages = new System.Collections.Generic.List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage("go")
        };

    // 3) Call A: force the tool call
    var options = new ChatCompletionOptions
    {
        ToolChoice = ChatToolChoice.CreateFunctionChoice("get_random_token"),
        Tools = { tool }
    };

    ChatCompletion first = await chat.CompleteChatAsync(messages, options);

    // Add the assistant message (with tool call) to the history
    messages.Add(new AssistantChatMessage(first));

    // Get the tool call id
    var toolCallId = first.ToolCalls[0].Id;

    // 4) Execute your tool on your server
    object toolResult = first.ToolCalls[0].FunctionName switch
    {
        "get_random_token" => GetRandomToken(),
        _ => throw new NotImplementedException()
    };

    // 5) Call B: send tool result, then get the final assistant text
    // The ToolChatMessage second parameter is the tool *result* (string or JSON string).
    messages.Add(new ToolChatMessage(toolCallId, JsonSerializer.Serialize(new { toolResult })));

    // For the follow-up call you can omit ToolChoice; tools may be left in options or omitted.
    ChatCompletion second = await chat.CompleteChatAsync(messages /*, options*/);


    // Expect: RANDOM_TOKEN: <guid>
    return new OkObjectResult(second.Content[0]?.Text ?? "Nothing was returned by Open AI");
});

string GetRandomToken() => Guid.NewGuid().ToString();

app.Run();

