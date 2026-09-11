internal sealed class LocalMusicLibraryOptions
{
    public required string LibraryPath { get; set; }
    public required IEnumerable<string> SupportedExtensions { get; set; }
}