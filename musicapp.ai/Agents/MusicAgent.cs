// using System.ComponentModel;
// using Google.GenAI;
// using Microsoft.Agents.AI;
// using Microsoft.Extensions.AI;
// using Microsoft.VisualBasic;

// internal sealed class MusicAgent
// {

//     const string AgentName = "MusicAgent";

//     private ChatClientAgent _chatClientAgent;
//     private AgentSession _session;

//     public static async Task<MusicAgent> CreateAsync()
//     {
//         var instance = new MusicAgent();
//         await instance.InitializeAsync();
//         return instance;
//     }

//     private async Task InitializeAsync()
//     {
//         string agentInstructions = "You are an agent with the following capabilities: " + 
//                                         "- provide metadata about music tracks from different parts of the world." + Environment.NewLine +
//                                         "- invoke appropriate tools to play, pause, stop, fast-forward, rewind a particular track." + Environment.NewLine +
//                                         "Before playing the track, always invoke the catalog tool, check if the the title contains in the catalog." + Environment.NewLine +  
//                                         "If exact match is found, go ahead and invoke the play tool to play the file by passing the title as the argument to the tool." + Environment.NewLine +
//                                         "If closest match is found, but you are not sure, confirm the user by responding the title what you have found." + Environment.NewLine +
//                                         "Once you receive confirmation from the user, invoke the play tool with the title as the argument."
//                                         ;

//         string apiKey = Environment.GetEnvironmentVariable("GOOGLE_GENAI_API_KEY") ?? throw new InvalidOperationException("Please set the GOOGLE_GENAI_API_KEY environment variable.");
//         string model = Environment.GetEnvironmentVariable("GOOGLE_GENAI_MODEL") ?? "gemini-3.5-flash-lite";

//         _chatClientAgent = new(
//                 new Client(vertexAI: false, apiKey: apiKey).AsIChatClient(model),
//                 name: AgentName,
//                 instructions: agentInstructions,
//                 tools: [
//                     AIFunctionFactory.Create(PlayMusicTrack),
//                     AIFunctionFactory.Create(GetMusicCatalog)
//                     ]
//             );

//         _session = await _chatClientAgent.CreateSessionAsync();
//     }



//     public async Task<AgentResponse> CallAgentAsync(string prompt)
//     {
//         var response = await _chatClientAgent.RunAsync(prompt, _session);
//         return response;
//     }

//     [Description("Play music track.")]
//     static string PlayMusicTrack([Description("The method to play a track by passing a track title.")] string trackTitle)
//     {
//         return $"You requested to play the track {trackTitle}";
//     }

//     [Description("Get music catalog.")]
//     static List<string> GetMusicCatalog([Description("The method to retrieve music catalog.")] string trackTitle)
//     {
//         return new List<string>
//         {
//             "Gange thudiyil.mp3",
//             "Kunjilam Chundil Punchiri.wav",
//             "Maranno Nee Nilaavil (Male).mp3",
//             "Chaithranilaavinte.mp3",
//             "Then nilaavilen.mp3"

//         };
//     }


// }

using Microsoft.Agents.AI;

internal sealed class MusicAgent
{
    private readonly ChatClientAgent _agent;

    public MusicAgent(ChatClientAgent agent)
    {
        _agent = agent;
    }

    public Task<AgentResponse> CallAgentAsync(
        string prompt,
        AgentSession session)
    {
        return _agent.RunAsync(prompt, session);
    }
}