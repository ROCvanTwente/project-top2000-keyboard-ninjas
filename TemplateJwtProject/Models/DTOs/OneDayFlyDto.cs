namespace TemplateJwtProject.Models.DTOs;

public class OneDayFlyDto
{
	public int SongId { get; set; }
	public int Year { get; set; }
	public int Position { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public int? ReleaseYear { get; set; }
	public string? ImgUrl { get; set; }
}
