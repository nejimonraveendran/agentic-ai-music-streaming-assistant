using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace musicapp.ai.Infrastructure;

internal class LocalMusicCache : IMusicCache
{
    private readonly List<Track> _cachedFiles = [];
    private readonly Lock _lock = new();

    public IEnumerable<Track> GetAllTracks()
    {
        lock (_lock)
        {
            return _cachedFiles.ToList();
        }
    }

    public bool TrackExists(string filePath)
    {
        return _cachedFiles.Any(x => string.Equals(x.Path, filePath, StringComparison.InvariantCultureIgnoreCase));
    }

    void IMusicCache.AddTrack(Track track)
    {
        if(TrackExists(track.Path)) return;
        _cachedFiles.Add(track);
    }
}