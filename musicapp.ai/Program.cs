using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Threading.Channels;
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
string libraryPath = Environment.GetEnvironmentVariable("LOCAL_LIBRARY_PATH") ?? throw new InvalidOperationException("Please set LOCAL_LIBRARY_PATH");
string geminiApiKey = Environment.GetEnvironmentVariable("GOOGLE_GENAI_API_KEY") ?? throw new InvalidOperationException("Please set GOOGLE_GENAI_API_KEY.");
string geminiAiModel = Environment.GetEnvironmentVariable("GOOGLE_GENAI_MODEL") ?? "gemini-3.5-flash-lite";
const string agentInstructions =
    """
    ### Role:
    You are an music playing agent with the following capabilities:

    ### Playback:
        - Handle requests from user to play music tracks.
        - Invoke appropriate tool to search music catalog find the most relevant track based on the user query.
        - If no match is found (i.e., catalog search tool produced 0 results), respond with a friendly "not found" message.
        - If catalog search tool produced only one result, pick that.
        - If catalog search tool produced more than one result, look at the confidence score returned by the search tool to rank the results.
        - If you are not able to choose the appropriate track, provide the user with the max 3 options out of what you found.   
        - Iteratively ask the user until you can confirm the final choice. 
        - Once you find the track, call the playback tool by passing the track Id and the current conversation Id.
        - Finally respond to the user with a summary of the track being played.
    ### Random Playback:
        - For any user requests to play random tracks, etc., call the track count tool, then generate a random number between 1 and the total count received. Other conditions same as above playback criteria.
    ### Stop, pause requests:
        - Call appropriate stop, pause tools
        
    """;


builder.Services.AddSingleton(new LocalMusicLibraryOptions
{
    LibraryPath = libraryPath,
    SupportedExtensions = [".wav", ".flac", ".mp3"]
});

builder.Services.AddSingleton<IMusicCache, LocalMusicCache>();
builder.Services.AddSingleton<ConversationStore>();
builder.Services.AddSingleton<ChannelStore>();

builder.Services.AddScoped<ConversationContext>();
builder.Services.AddScoped<LibraryService>();
builder.Services.AddScoped<MusicTools>();
builder.Services.AddScoped((Func<IServiceProvider, ChatClientAgent>)(sp =>
{
    var chatClient = new Client(vertexAI: false, apiKey: geminiApiKey).AsIChatClient(geminiAiModel);
    var musicTools = sp.GetRequiredService<MusicTools>();
    
    return new ChatClientAgent(
        chatClient,
        name: AgentName,
        instructions: agentInstructions,
        tools:
        [
            AIFunctionFactory.Create(musicTools.SearchCatalogByTitle),
            AIFunctionFactory.Create(musicTools.PlayTrackByIdAsync),
            AIFunctionFactory.Create(musicTools.GetTrackCount),
            AIFunctionFactory.Create(musicTools.StopOrPauseCurrentPlaybackAsync),
        ]);
}));

                                                                                              
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

ScanLocalMusicLibrary(app);

app.MapPost("/api/music/chat",
    async (ChatRequest request, 
            [FromServices] ChatClientAgent agent, 
            [FromServices] ConversationStore conversations, 
            ConversationContext conversationContext) =>
    {
        string responseText = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(request.Message)) throw new InvalidOperationException("Please type a message");
            conversationContext.ConversationId = request.ConversationId;
            var session = await conversations.GetOrCreateAsync(request.ConversationId, agent);
            var response = await agent.RunAsync(request.Message, session);
            responseText = FormatMessage(response.Text);
        }
        catch (InvalidOperationException ex)
        {
            responseText = ex.Message;
        }catch
        {
            responseText = "Sorry, an unexpected error occured!";
        }
        
        return Results.Ok(new
        {
            request.ConversationId,
            response = responseText
        });
    });

app.MapGet("/api/music/stream/{id:int}", async (int id, LibraryService libraryService) =>
{

    var track = libraryService.GetTrackById(id);
    if (track == null || !File.Exists(track.Path))
    {
        return Results.NotFound();
    }

    var ext = Path.GetExtension(track.Path).ToLowerInvariant();
    var contentType = ext switch
    {
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".flac" => "audio/flac",
        _ => "application/octet-stream"
    };

    return Results.File(track.Path, contentType: contentType, enableRangeProcessing: true);
});


app.MapGet("/api/music/chat/sse/{conversationId}", async (string conversationId, 
    HttpResponse response,
    ChannelStore channelStore,
    CancellationToken cancellationToken
    ) =>
{
    channelStore.StoreConversation(conversationId);
    response.ContentType = "text/event-stream";
    response.Headers.CacheControl = "no-cache";

    while (true)
    {
        try
        {
            var myConversationChannel = channelStore.GetConversation(conversationId);
            if(myConversationChannel == null || myConversationChannel == default) continue;

            await foreach (string item in myConversationChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var obj = JsonSerializer.Deserialize<ChatEventInfo>(item);
                if(!string.Equals(obj?.ConversationId, conversationId)) continue;

                await response.WriteAsync($"data: {JsonSerializer.Serialize(item)}\n\n", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);

            }            
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //client disconnected
            Console.WriteLine($"SSE client disconnected");   
            return;
        }        

        await Task.Delay(100);
    }
});

app.MapPost("/api/music/chat/sse/test/{conversationId}", async (string conversationId, ChannelStore channelStore, LibraryService libraryService) =>
{
    var conversationChannel = channelStore.GetConversation(conversationId);
    var track = libraryService.GetTrackById(Random.Shared.Next(1, 1000));
    await conversationChannel.Writer.WriteAsync(JsonSerializer.Serialize(new ChatEventInfo(EventType: ChatEventType.Test, ConversationId: conversationId, 
        Track: track)));
        
    return Results.Ok("written");
});


app.MapPost("/api/music/search", async (LibraryService libraryService, [FromBody] SearchRequest request) =>
{
    var results = libraryService.SearchTracksByTitle(request.Title);
    return Results.Ok(results);
});


app.Run();

static void ScanLocalMusicLibrary(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var libService = scope.ServiceProvider.GetRequiredService<LibraryService>();
    libService.ScanLibrary();
}


static string FormatMessage(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return text;

    var originalText = text;
    text = text.Trim();

    // Remove ```json ... ``` or ``` ... ```
    if (text.StartsWith("```"))
    {
        var firstNewLine = text.IndexOf('\n');

        if (firstNewLine >= 0)
            text = text[(firstNewLine + 1)..];

        var closingFence = text.LastIndexOf("```");

        if (closingFence >= 0)
            text = text[..closingFence];
    }

    text = text.Trim();

    var start = text.IndexOf('{');
    var end = text.LastIndexOf('}');

    // No JSON object found — return original response unchanged
    if (start < 0 || end < start)
        return originalText;

    return text[start..(end + 1)];
}