internal class TrackSearchResult 
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public double ConfidenceScore { get; set; }
    public string? Duration { get; set; }
}