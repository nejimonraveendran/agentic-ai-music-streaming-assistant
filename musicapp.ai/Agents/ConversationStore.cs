using Microsoft.Agents.AI;
using System.Collections.Concurrent;

sealed class ConversationStore
{
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public async Task<AgentSession> GetOrCreateAsync(string conversationId, ChatClientAgent agent)
    {
        if (_sessions.TryGetValue(conversationId, out var session))
            return session;

        session = await agent.CreateSessionAsync();

        _sessions[conversationId] = session;

        return session;
    }
}