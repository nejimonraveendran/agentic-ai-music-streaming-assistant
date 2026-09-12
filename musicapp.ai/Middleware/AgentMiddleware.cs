using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

public static class AgentMiddleware
{
    public static AIAgent AddLoggingMiddleware(this ChatClientAgent agent)
    {
        return agent.AsBuilder().Use(runFunc: CustomAgentLoggingMiddleware, runStreamingFunc: null).Build();
    }


    async static Task<AgentResponse> CustomAgentLoggingMiddleware(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("===== LLM REQUEST =====");
        foreach (var message in messages)
        {
            Console.WriteLine($"Role: {message.Role}");
            Console.WriteLine($"Text: {message.Text}");
        }
        Console.WriteLine($"Message count: {messages.Count()}");
        Console.WriteLine("======================");

        var response = await innerAgent.RunAsync(messages, session, options, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("===== LLM RESPONSE =====");
        Console.WriteLine(response.Text);
        Console.WriteLine($"Message count: {response.Messages.Count()}");
        Console.WriteLine("========================");

        return response;
    }
}