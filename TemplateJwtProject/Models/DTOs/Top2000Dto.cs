using System.ComponentModel.DataAnnotations;

namespace TemplateJwtProject.Models.DTOs;

// DTO voor het ophalen van entries met alle song info
public class Top2000EntryDto
{
	public int SongId { get; set; }
	public int Year { get; set; }
	public int Position { get; set; }
	public string SongTitle { get; set; } = string.Empty;
	public string ArtistName { get; set; } = string.Empty;
	public int? ReleaseYear { get; set; }
	public string? ImgUrl { get; set; }
	public string? Youtube { get; set; }
}

// DTO voor song geschiedenis (posities door de jaren heen)
public class SongHistoryDto
{
	public int Year { get; set; }
	public int Position { get; set; }
	public string SongTitle { get; set; } = string.Empty;
	public string ArtistName { get; set; } = string.Empty;
}

// DTO voor basis song informatie
public class SongBasicDto
{
	public int SongId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public int Position { get; set; }
	public int? ReleaseYear { get; set; }
}

// DTO voor statistieken van een jaar
public class Top2000StatsDto
{
	public int Year { get; set; }
	public int TotalEntries { get; set; }
	public SongBasicDto? OldestSong { get; set; }
	public SongBasicDto? NewestSong { get; set; }
}

// DTO voor het aanmaken van een entry
public class CreateTop2000EntryDto
{
	[Required]
	public int SongId { get; set; }

	[Required]
	[Range(1999, 2100)]
	public int Year { get; set; }

	[Required]
	[Range(1, 2000)]
	public int Position { get; set; }
}

// DTO voor het updaten van een entry
public class UpdateTop2000EntryDto
{
	[Required]
	public int SongId { get; set; }

	[Required]
	public int Year { get; set; }

	[Required]
	[Range(1, 2000)]
	public int Position { get; set; }
}