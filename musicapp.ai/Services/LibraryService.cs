using musicapp.ai.Infrastructure;
using Raffinert.FuzzySharp;

internal sealed class LibraryService
{

    private LocalMusicLibraryOptions _libraryOptions;
    private readonly IMusicCache _cache;

    public LibraryService(IMusicCache cache, LocalMusicLibraryOptions localMusicLibraryOptions)
    {
        _cache = cache;
        _libraryOptions = localMusicLibraryOptions;
    }

    public void ScanLibrary()
    {
        if (!Directory.Exists(_libraryOptions.LibraryPath))
        {
            return;
        }

        var filePaths = Directory.EnumerateFiles(_libraryOptions.LibraryPath, "*.*", SearchOption.AllDirectories)
                .Where(f => _libraryOptions.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        int id = 1;
        foreach (var filePath in filePaths)
        {
            try
            {
                if (_cache.TrackExists(filePath)) continue;

                var track = GetMusicTrackFromFile(filePath);
                track.Id = id;
                _cache.AddTrack(track);
                id++;                
            }
            catch
            {
                //ignore failed files
            }
        }
    }

    public IEnumerable<TrackSearchResult?> SearchTracksByTitle(string title, int limitResults = 5)
    {
        const double confidenceScore = 85d;
        var query = title.Trim().ToLowerInvariant();

        var foundTracks = _cache.GetAllTracks()
            .Select(t => new
            {
                Track = t, Score = Fuzz.PartialRatio(t.Title.ToLowerInvariant(), query)
            })
            .Where(x => x.Score >= confidenceScore)
            .OrderByDescending(x => x.Score)
            .Take(limitResults)
            .Select(x => new TrackSearchResult
            {
                Id = x.Track.Id,
                Album = x.Track.Album,
                Artist = x.Track.Artist,
                ConfidenceScore = x.Score,
                Duration = $"{x.Track.Duration.Minutes:D2}:{x.Track.Duration.Seconds:D2}",
                Title = x.Track.Title
            })
            .ToList();
        
        return foundTracks;

    }

    private Track GetMusicTrackFromFile(string filePath)
    {
        using var tagFile = TagLib.File.Create(filePath);
        
        return new Track
        { 
            Id = 0, 
            Path = filePath,
            Title = string.IsNullOrWhiteSpace(tagFile.Tag.Title) || tagFile.Tag.Title.StartsWith("Track") ? Path.GetFileNameWithoutExtension(filePath) : tagFile.Tag.Title, 
            Album = tagFile.Tag.Album ?? "Unknown Album",
            Artist = tagFile.Tag.FirstPerformer ?? "Unknown Artist",
            Duration = tagFile.Properties.Duration,
            
        };
        
    }

    public Track? GetTrackById(int id)
    {
        return _cache.GetAllTracks().FirstOrDefault(t => t.Id == id);
    }
}

