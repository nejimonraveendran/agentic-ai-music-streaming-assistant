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