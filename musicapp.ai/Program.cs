using System.Runtime.InteropServices.Marshalling;
using DotNetEnv;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using musicapp.ai.Infrastructure;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://0.0.0.0:5050");

const string AgentName = "MusicAgent";

builder.Services.AddSingleton(new LocalMusicLibraryOptions
{
    LibraryPath = Environment.GetEnvironmentVariable("LOCAL_LIBRARY_PATH") ?? throw new InvalidOperationException("Please set LOCAL_LIBRARY_PATH"),
    SupportedExtensions = [".wav", ".flac", ".mp3"]
});

builder.Services.AddSingleton<IMusicCache, LocalMusicCache>();
builder.Services.AddSingleton<LibraryService>();
builder.Services.AddSingleton<MusicTools>();
builder.Services.AddSingleton<ConversationStore>();
builder.Services.AddSingleton(sp =>
{
    string apiKey = Environment.GetEnvironmentVariable("GOOGLE_GENAI_API_KEY") ?? throw new InvalidOperationException("Please set GOOGLE_GENAI_API_KEY.");
    string model = Environment.GetEnvironmentVariable("GOOGLE_GENAI_MODEL") ?? "gemini-3.5-flash-lite";

    // string instructions =
    //     """
    //     You are an music playing agent with the following capabilities:
    //     - Handle requests from user to play music tracks.
    //     - Invoke appropriate tools to search music catalog find the most relevant track based on the user query.
    //     - Respond with track id, title, album, artist, etc.
    //     - If exact/closest match is not found and you are are not sure, ask the user to confirm the title by providing them with what you have found. Suggest only less than 5 closest match.
    //     - If no match is found, respond with a friendly "not found" response.
    //     - MOST IMPORTANT: Respond using this automation-compliant JSON template with no extra frills: {id, title, artist, album, duration, confidence, message}
    //     """;
    string instructions =
        """
        You are an music playing agent with the following capabilities:
        - Handle requests from user to play music tracks.
        - Invoke appropriate tool to search music catalog find the most relevant track based on the user query.
        - If no match is found (i.e., catalog search tool produced 0 results), respond with a friendly "not found" message.
        - If catalog search tool produced only one result, pick that.
        - If catalog search tool produced more than one result, look at the confidence score returned by the search tool to rank the results.
        - If you are not able to choose the appropriate track, provide the user with the max 3 options out of what you found.   
        - Iteratively ask the user until you can confirm the final choice. 
        - MOST IMPORTANT: Finally, respond using this automation-compliant JSON template with no extra frills: {id, title, artist, album, duration, confidence, message}
        """;

    var chatClient = new Client(vertexAI: false, apiKey: apiKey).AsIChatClient(model);

    var musicTools = sp.GetRequiredService<MusicTools>();
    
    return new ChatClientAgent(
        chatClient,
        name: AgentName,
        instructions: instructions,
        tools:
        [
            AIFunctionFactory.Create(musicTools.SearchCatalogByTitle)
        ]);
});


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

ScanLocalMusicLibrary(app);

app.MapPost("/api/music/chat",
    async (ChatRequest request, [FromServices] ChatClientAgent agent, [FromServices] ConversationStore conversations) =>
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new
            {
                error = "Message is required."
            });
        }

        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId) ? Guid.NewGuid().ToString() : request.ConversationId;

        var session = await conversations.GetOrCreateAsync(conversationId, agent);
        var response = await agent.RunAsync(request.Message, session);

        return Results.Ok(new
        {
            conversationId,
            response = response.Text
        });
    });

app.MapPost("/api/music/search", async (LibraryService libraryService, [FromBody] SearchRequest request) =>
{
    var results = libraryService.SearchTracksByTitle(request.Title);
    return Results.Ok(results);
});

app.MapGet("/api/music/stream/{id:int}", (int id, LibraryService libraryService) =>
{
    var track = libraryService.GetTrackById(id);
    if (track == null || !System.IO.File.Exists(track.Path))
    {
        return Results.NotFound();
    }

    var ext = System.IO.Path.GetExtension(track.Path).ToLowerInvariant();
    var contentType = ext switch
    {
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".flac" => "audio/flac",
        _ => "application/octet-stream"
    };

    return Results.File(track.Path, contentType: contentType, enableRangeProcessing: true);
});

app.Run();

static void ScanLocalMusicLibrary(WebApplication app)
{
    var libService = app.Services.GetRequiredService<LibraryService>();
    libService.ScanLibrary();
}