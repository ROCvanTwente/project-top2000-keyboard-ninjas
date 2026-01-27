namespace TemplateJwtProject.Models.DTOs;

// DTO voor het oudste nummer in een jaar
public class OldestSongDto
{
	public int SongId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public int? ReleaseYear { get; set; }
	public int Position { get; set; }
	public int Year { get; set; }
	public string? ImgUrl { get; set; }
}

// DTO voor hoogste binnenkomers
public class HighestNewEntryDto
{
	public int SongId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public int? ReleaseYear { get; set; }
	public int Position { get; set; }
	public int Year { get; set; }
	public string? ImgUrl { get; set; }
	public bool IsNewEntry { get; set; }
}

// DTO voor records overzicht
public class RecordsOverviewDto
{
	public OldestSongDto? OldestSongOfYear { get; set; }
	public HighestNewEntryDto? HighestNewEntry { get; set; }
	public List<HighestNewEntryDto> TopNewEntries { get; set; } = new();
}

