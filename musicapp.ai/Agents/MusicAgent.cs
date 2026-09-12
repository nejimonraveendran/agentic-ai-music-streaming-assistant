using Microsoft.Agents.AI;

internal sealed class MusicAgent
{
    private readonly AIAgent _agent;

    public MusicAgent(AIAgent agent)
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