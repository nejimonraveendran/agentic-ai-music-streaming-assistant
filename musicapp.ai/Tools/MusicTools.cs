using System.ComponentModel;
using musicapp.ai.Infrastructure;

internal sealed class MusicTools(LibraryService _libraryService)
{


    // [Description("Play a music track.")]
    // public static string PlayMusicTrack(
    //     [Description("The exact title of the track to play.")]
    //     string trackTitle)
    // {
    //     Console.WriteLine($"PlayMusicTrack: {trackTitle}");

    //     return $"You requested to play the track {trackTitle}";
    // }


    [Description("Search the music catalog by title.")]
    public IEnumerable<TrackSearchResult?> SearchCatalogByTitle(string title)
    {
        Console.WriteLine($"SearchCatalogByTitle invoked");

        return _libraryService.SearchTracksByTitle(title);
   }
    


    // [Description("Get the music catalog.")]
    // public static List<string> GetMusicCatalog(IMusicCache musicCache)
    // {
    //     Console.WriteLine($"GetMusicCatalog invoked");

    //     var trackFiles = musicCache.GetAllTracks();

    //     return
    //     [
    //         "Gange thudiyil.mp3",
    //         "Kunjilam Chundil Punchiri.wav",
    //         "Maranno Nee Nilaavil (Male).mp3",
    //         "Chaithranilaavinte.mp3",
    //         "Then nilaavilen.mp3"
    //     ];
    // }
}