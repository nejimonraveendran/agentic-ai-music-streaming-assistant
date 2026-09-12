internal sealed class Track{
    public required int Id { get; set; }
    public required string Path { get; set; }
    public required string Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public TimeSpan Duration { get; set; }
    
}