// Models/ArtistDto.cs
namespace TemplateJwtProject.Models
{
    public class ArtistDto
    {
        public int ArtistId { get; set; }
        public string Name { get; set; } = null!;
        public string? Photo { get; set; }
        public string? Wiki { get; set; }
        public string? Biography { get; set; }
        public int TotalSongs { get; set; }
        public int? HighestPosition { get; set; }
        public string? BestSongTitle { get; set; }
    }
}