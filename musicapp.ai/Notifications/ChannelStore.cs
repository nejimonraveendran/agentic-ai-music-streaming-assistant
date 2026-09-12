using System.Collections.Concurrent;
using System.Threading.Channels;

internal sealed class ChannelStore
{
    private readonly ConcurrentDictionary<string, Channel<string>> _channels = new();

    public void StoreConversation(string converstationId)
    {
        if(_channels.Keys.Contains(converstationId)) return;
        var newChannel = Channel.CreateUnbounded<string>();
        _channels.TryAdd(converstationId, newChannel);
    }

    public Channel<string> GetConversation(string conversationId)
    {
        return _channels.FirstOrDefault(kvp => kvp.Key == conversationId).Value;
    }
}