namespace TemplateJwtProject.Models.DTOs;

// DTO voor evergreen nummers (nummers die in elk jaar in de lijst staan)
public class EvergreenDto
{
	public int SongId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public int YearsInList { get; set; }  // Aantal jaren dat het nummer in de lijst staat
	public int? ReleaseYear { get; set; }
	public string? ImgUrl { get; set; }
	public int? BestPosition { get; set; }  // Beste positie ooit behaald
}
