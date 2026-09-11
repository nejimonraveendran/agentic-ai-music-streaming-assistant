using System;
using System.Collections.Generic;

namespace musicapp.ai.Infrastructure;

internal interface IMusicCache
{
    void AddTrack(Track track);
    bool TrackExists(string filePath);
    IEnumerable<Track> GetAllTracks();
}