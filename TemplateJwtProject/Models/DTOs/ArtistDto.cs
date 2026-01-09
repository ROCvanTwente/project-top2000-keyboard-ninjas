// Models/ArtistDto.cs
namespace TemplateJwtProject.Models
{
    public class ArtistDto
    {
        public int ArtistId { get; set; }
        public string Name { get; set; } = null!;
        public int TotalSongs { get; set; }
        // Voeg andere velden toe indien nodig
    }
}