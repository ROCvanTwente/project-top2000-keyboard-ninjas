using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;
using TemplateJwtProject.Models;

namespace TemplateJwtProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtistsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ArtistsController> _logger;

    public ArtistsController(AppDbContext context, ILogger<ArtistsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/artists
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artist>>> GetAllArtists()
    {
        try
        {
            var artists = await _context.Artist
                .Include(a => a.Songs)
                .ToListAsync();

            return Ok(artists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving artists");
            return StatusCode(500, new { message = "Error retrieving artists", error = ex.Message });
        }
    }

    // GET: api/artists/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Artist>> GetArtist(int id)
    {
        try
        {
            var artist = await _context.Artist
                .Include(a => a.Songs)
                    .ThenInclude(s => s.Top2000Entries)
                .FirstOrDefaultAsync(a => a.ArtistId == id);

            if (artist == null)
            {
                return NotFound(new { message = $"Artist with ID {id} not found" });
            }

            return Ok(artist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving artist {ArtistId}", id);
            return StatusCode(500, new { message = "Error retrieving artist", error = ex.Message });
        }
    }

    // GET: api/artists/{id}/songs
    [HttpGet("{id}/songs")]
    public async Task<ActionResult<IEnumerable<Songs>>> GetArtistSongs(int id)
    {
        try
        {
            var songs = await _context.Songs
                .Where(s => s.ArtistId == id)
                .Include(s => s.Top2000Entries)
                .ToListAsync();

            if (!songs.Any())
            {
                return NotFound(new { message = $"No songs found for artist with ID {id}" });
            }

            return Ok(songs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving songs for artist {ArtistId}", id);
            return StatusCode(500, new { message = "Error retrieving artist songs", error = ex.Message });
        }
    }

    // GET: api/artists/search?name={name}
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Artist>>> SearchArtists([FromQuery] string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Search term cannot be empty" });
            }

            var artists = await _context.Artist
                .Where(a => a.Name.Contains(name))
                .Include(a => a.Songs)
                .ToListAsync();

            return Ok(artists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching artists with term {SearchTerm}", name);
            return StatusCode(500, new { message = "Error searching artists", error = ex.Message });
        }
    }

    // GET: api/artists/summary
    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<ArtistDto>>> GetArtistsSummary()
    {
        try
        {
            var artists = await _context.Artist
                .Select(a => new ArtistDto
                {
                    ArtistId = a.ArtistId,
                    Name = a.Name,
                    TotalSongs = a.Songs.Count(),
                    HighestPosition = a.Songs
                        .SelectMany(s => s.Top2000Entries)
                        .OrderBy(t => t.Position)
                        .Select(t => (int?)t.Position)
                        .FirstOrDefault(),
                    BestSongTitle = a.Songs
                        .SelectMany(s => s.Top2000Entries.Select(t => new { Song = s, Entry = t }))
                        .OrderBy(x => x.Entry.Position)
                        .Select(x => x.Song.Titel)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(artists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving artist summaries");
            return StatusCode(500, new { message = "Error retrieving artist summaries", error = ex.Message });
        }
    }
}

// DTO for artist summary
public class ArtistDto
{
    public int ArtistId { get; set; }
    public string Name { get; set; } = null!;
    public int TotalSongs { get; set; }
    public int? HighestPosition { get; set; }
    public string? BestSongTitle { get; set; }
}