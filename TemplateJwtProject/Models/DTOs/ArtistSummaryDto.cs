namespace TemplateJwtProject.Models.DTOs;

public class ArtistSummaryDto
{
    public int ArtistId { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string ArtistNaam { get; set; } = string.Empty;
    public int TotalSongs { get; set; }
    public int? HighestPosition { get; set; }
}
