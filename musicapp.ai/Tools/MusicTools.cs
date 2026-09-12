using System.ComponentModel;
using System.Text.Json;
using musicapp.ai.Infrastructure;

internal sealed class MusicTools(LibraryService _libraryService, 
                                    ChannelStore _channelStore, 
                                    ConversationContext _conversationContext)
{
    [Description("Search the music catalog by title.")]
    public IEnumerable<TrackSearchResult?> SearchCatalogByTitle([Description("Title of the track")] string title)
    {
        Console.WriteLine($"SearchCatalogByTitle invoked for title: {title}");

        return _libraryService.SearchTracksByTitle(title);
    }

    [Description("Search the music catalog by artist, album.")]
    public IEnumerable<TrackSearchResult?> SearchCatalogByTitleArtistAlbum(
        [Description("Name of the artist (optional)")] string? artist,
        [Description("Name of the album (optional)")] string? album
        )
    {
        Console.WriteLine($"SearchCatalogByTitleArtistAlbum invoked for artist: {artist}, album: {album}");

        return _libraryService.SearchTracksByArtistAlbum(artist, album);
    }


    [Description("Play music track by track Id.")]
    public async Task<PlayTrackResult> PlayTrackByIdAsync([Description("Track Id")] int trackId)
    {
        Console.WriteLine($"PlayTrackById invoked by track Id: {trackId}, ConverationId: {_conversationContext.ConversationId}");

        if(_conversationContext == null || string.IsNullOrEmpty(_conversationContext.ConversationId))
            return new PlayTrackResult{ IsSuccess = false, Message = "Invalid conversastion context"};

        var track = _libraryService.GetTrackById(trackId);

        if(track == null)
        {
            return new PlayTrackResult{ IsSuccess = false, Message = "Track not found"};
        }

        var myConversationChannel = _channelStore.GetConversation(_conversationContext.ConversationId);
        if(myConversationChannel == null || myConversationChannel == default)
        {
            return new PlayTrackResult{ IsSuccess = false, Message = "No valid conversation channel found"};
        }

        await myConversationChannel.Writer.WriteAsync(JsonSerializer.Serialize(new ChatEventInfo(EventType: ChatEventType.Play, ConversationId: _conversationContext.ConversationId, Track: track)));
        
        return new PlayTrackResult{ IsSuccess = true, Message = $"Playing track {track.Title}"};
    }


    [Description("Get tracks count.")]
    public int GetTrackCount()
    {
        Console.WriteLine($"GetTrackCount invoked, ConverationId: {_conversationContext.ConversationId}");
        return _libraryService.GetTrackCount();
    }

    [Description("Stop or pause playback current playback if applicable.")]
    public async Task StopOrPauseCurrentPlaybackAsync()
    {
        Console.WriteLine($"StopOrPauseCurrentPlayback invoked, ConverationId: {_conversationContext.ConversationId}");
        
        if(_conversationContext == null || string.IsNullOrEmpty(_conversationContext.ConversationId)) 
            return;
        
        var myConversationChannel = _channelStore.GetConversation(_conversationContext.ConversationId);
        if(myConversationChannel == null || myConversationChannel == default)
            return;
        
        await myConversationChannel.Writer.WriteAsync(JsonSerializer.Serialize(new ChatEventInfo(EventType: ChatEventType.StopOrPause, ConversationId: _conversationContext.ConversationId, Track: null)));

    }
    
}