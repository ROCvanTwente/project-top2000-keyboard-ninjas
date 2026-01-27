namespace TemplateJwtProject.Models.DTOs;

// DTO voor grootste stijgers en dalers
public class RiserDto
{
	public int SongId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Artist { get; set; } = string.Empty;
	public int CurrentYearPosition { get; set; }
	public int PreviousYearPosition { get; set; }
	public int Rise { get; set; }  // Positief getal = gestegen, negatief = gedaald
	public int Year { get; set; }
	public string? ImgUrl { get; set; }
}
