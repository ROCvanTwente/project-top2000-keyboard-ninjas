using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;
using TemplateJwtProject.Models;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Top2000Controller : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly ILogger<Top2000Controller> _logger;

	public Top2000Controller(AppDbContext context, ILogger<Top2000Controller> logger)
	{
		_context = context;
		_logger = logger;
	}

	// GET: api/top2000/year/{year}
	// Haal alle entries van een specifiek jaar op met song en artist info
	[HttpGet("year/{year}")]
	public async Task<ActionResult<IEnumerable<Top2000EntryDto>>> GetEntriesByYear(int year)
	{
		try
		{
			var entries = await _context.Top2000Entries
				.Where(e => e.Year == year)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.OrderBy(e => e.Position)
				.Select(e => new Top2000EntryDto
				{
					SongId = e.SongId,
					Year = e.Year,
					Position = e.Position,
					SongTitle = e.Song.Titel,
					ArtistName = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					ImgUrl = e.Song.ImgUrl,
					Youtube = e.Song.Youtube
				})
				.ToListAsync();

			if (!entries.Any())
			{
				return NotFound(new { message = $"No entries found for year {year}" });
			}

			return Ok(entries);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving entries for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving entries" });
		}
	}

	// GET: api/top2000/song/{songId}
	// Haal alle posities van een specifiek nummer op door de jaren heen
	[HttpGet("song/{songId}")]
	public async Task<ActionResult<IEnumerable<SongHistoryDto>>> GetSongHistory(int songId)
	{
		try
		{
			var songExists = await _context.Songs.AnyAsync(s => s.SongId == songId);
			if (!songExists)
			{
				return NotFound(new { message = $"Song with ID {songId} not found" });
			}

			var history = await _context.Top2000Entries
				.Where(e => e.SongId == songId)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.OrderByDescending(e => e.Year)
				.Select(e => new SongHistoryDto
				{
					Year = e.Year,
					Position = e.Position,
					SongTitle = e.Song.Titel,
					ArtistName = e.Song.Artist.Name
				})
				.ToListAsync();

			if (!history.Any())
			{
				return NotFound(new { message = $"No Top 2000 history found for song ID {songId}" });
			}

			return Ok(history);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving history for song {SongId}", songId);
			return StatusCode(500, new { message = "Error retrieving song history" });
		}
	}

	// GET: api/top2000/year/{year}/position/{position}
	// Haal een specifieke positie van een jaar op
	[HttpGet("year/{year}/position/{position}")]
	public async Task<ActionResult<Top2000EntryDto>> GetByYearAndPosition(int year, int position)
	{
		try
		{
			var entry = await _context.Top2000Entries
				.Where(e => e.Year == year && e.Position == position)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.Select(e => new Top2000EntryDto
				{
					SongId = e.SongId,
					Year = e.Year,
					Position = e.Position,
					SongTitle = e.Song.Titel,
					ArtistName = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					ImgUrl = e.Song.ImgUrl,
					Youtube = e.Song.Youtube
				})
				.FirstOrDefaultAsync();

			if (entry == null)
			{
				return NotFound(new { message = $"No entry found at position {position} for year {year}" });
			}

			return Ok(entry);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving entry for year {Year} position {Position}", year, position);
			return StatusCode(500, new { message = "Error retrieving entry" });
		}
	}

	// GET: api/top2000/year/{year}/top/{count}
	// Haal de top X nummers van een jaar op
	[HttpGet("year/{year}/top/{count}")]
	public async Task<ActionResult<IEnumerable<Top2000EntryDto>>> GetTopSongs(int year, int count)
	{
		try
		{
			if (count <= 0 || count > 2000)
			{
				return BadRequest(new { message = "Count must be between 1 and 2000" });
			}

			var topSongs = await _context.Top2000Entries
				.Where(e => e.Year == year)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.OrderBy(e => e.Position)
				.Take(count)
				.Select(e => new Top2000EntryDto
				{
					SongId = e.SongId,
					Year = e.Year,
					Position = e.Position,
					SongTitle = e.Song.Titel,
					ArtistName = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					ImgUrl = e.Song.ImgUrl,
					Youtube = e.Song.Youtube
				})
				.ToListAsync();

			return Ok(topSongs);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving top {Count} songs for year {Year}", count, year);
			return StatusCode(500, new { message = "Error retrieving top songs" });
		}
	}

	// GET: api/top2000/years
	// Haal alle beschikbare jaren op
	[HttpGet("years")]
	public async Task<ActionResult<IEnumerable<int>>> GetAvailableYears()
	{
		try
		{
			var years = await _context.Top2000Entries
				.Select(e => e.Year)
				.Distinct()
				.OrderByDescending(y => y)
				.ToListAsync();

			return Ok(years);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving available years");
			return StatusCode(500, new { message = "Error retrieving years" });
		}
	}

	// GET: api/top2000/stats/{year}
	// Statistieken voor een jaar
	[HttpGet("stats/{year}")]
	public async Task<ActionResult<Top2000StatsDto>> GetYearStats(int year)
	{
		try
		{
			var entries = await _context.Top2000Entries
				.Where(e => e.Year == year)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.ToListAsync();

			if (!entries.Any())
			{
				return NotFound(new { message = $"No entries found for year {year}" });
			}

			// Oudste nummer (laagste release jaar)
			var oldestSong = entries
				.Where(e => e.Song.ReleaseYear.HasValue)
				.OrderBy(e => e.Song.ReleaseYear)
				.FirstOrDefault();

			// Nieuwste nummer (hoogste release jaar)
			var newestSong = entries
				.Where(e => e.Song.ReleaseYear.HasValue)
				.OrderByDescending(e => e.Song.ReleaseYear)
				.FirstOrDefault();

			var stats = new Top2000StatsDto
			{
				Year = year,
				TotalEntries = entries.Count,
				OldestSong = oldestSong != null ? new SongBasicDto
				{
					SongId = oldestSong.SongId,
					Title = oldestSong.Song.Titel,
					Artist = oldestSong.Song.Artist.Name,
					Position = oldestSong.Position,
					ReleaseYear = oldestSong.Song.ReleaseYear
				} : null,
				NewestSong = newestSong != null ? new SongBasicDto
				{
					SongId = newestSong.SongId,
					Title = newestSong.Song.Titel,
					Artist = newestSong.Song.Artist.Name,
					Position = newestSong.Position,
					ReleaseYear = newestSong.Song.ReleaseYear
				} : null
			};

			return Ok(stats);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving stats for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving statistics" });
		}
	}

	// POST: api/top2000
	// Voeg een nieuwe entry toe (Admin only)
	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Top2000Entries>> CreateEntry([FromBody] CreateTop2000EntryDto dto)
	{
		try
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			// Check if song exists
			var songExists = await _context.Songs.AnyAsync(s => s.SongId == dto.SongId);
			if (!songExists)
			{
				return NotFound(new { message = $"Song with ID {dto.SongId} not found" });
			}

			// Check if entry already exists
			var existingEntry = await _context.Top2000Entries
				.FirstOrDefaultAsync(e => e.SongId == dto.SongId && e.Year == dto.Year);

			if (existingEntry != null)
			{
				return Conflict(new { message = $"Entry already exists for song {dto.SongId} in year {dto.Year}" });
			}

			var entry = new Top2000Entries
			{
                SongId = dto.SongId,
				Year = dto.Year,
				Position = dto.Position
			};

			_context.Top2000Entries.Add(entry);
			await _context.SaveChangesAsync();

			_logger.LogInformation("Entry created: Song {SongId}, Year {Year}, Position {Position}", 
				dto.SongId, dto.Year, dto.Position);

			return CreatedAtAction(
				nameof(GetByYearAndPosition), 
				new { year = entry.Year, position = entry.Position }, 
				entry);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating entry");
			return StatusCode(500, new { message = "Error creating entry" });
		}
	}

	// PUT: api/top2000
	// Update een bestaande entry (Admin only)
	[HttpPut]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> UpdateEntry([FromBody] UpdateTop2000EntryDto dto)
	{
		try
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var entry = await _context.Top2000Entries
				.FirstOrDefaultAsync(e => e.SongId == dto.SongId && e.Year == dto.Year);

			if (entry == null)
			{
				return NotFound(new { message = $"Entry not found for song {dto.SongId} in year {dto.Year}" });
			}

			entry.Position = dto.Position;
			await _context.SaveChangesAsync();

			_logger.LogInformation("Entry updated: Song {SongId}, Year {Year}, New Position {Position}", 
				dto.SongId, dto.Year, dto.Position);

			return Ok(entry);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating entry");
			return StatusCode(500, new { message = "Error updating entry" });
		}
	}

	// DELETE: api/top2000/{songId}/{year}
	// Verwijder een entry (Admin only)
	[HttpDelete("{songId}/{year}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteEntry(int songId, int year)
	{
		try
		{
			var entry = await _context.Top2000Entries
				.FirstOrDefaultAsync(e => e.SongId == songId && e.Year == year);

			if (entry == null)
			{
				return NotFound(new { message = $"Entry not found for song {songId} in year {year}" });
			}

			_context.Top2000Entries.Remove(entry);
			await _context.SaveChangesAsync();

			_logger.LogInformation("Entry deleted: Song {SongId}, Year {Year}", songId, year);

			return Ok(new { message = "Entry deleted successfully" });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting entry for song {SongId} year {Year}", songId, year);
			return StatusCode(500, new { message = "Error deleting entry" });
		}
	}

	// GET: api/top2000/search?year={year}&artist={artist}&title={title}
	// Zoek entries op basis van filters
	[HttpGet("search")]
	public async Task<ActionResult<IEnumerable<Top2000EntryDto>>> SearchEntries(
		[FromQuery] int? year,
		[FromQuery] string? artist,
		[FromQuery] string? title)
	{
		try
		{
			var query = _context.Top2000Entries
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.AsQueryable();

			if (year.HasValue)
			{
				query = query.Where(e => e.Year == year.Value);
			}

			if (!string.IsNullOrWhiteSpace(artist))
			{
				query = query.Where(e => e.Song.Artist.Name.Contains(artist));
			}

			if (!string.IsNullOrWhiteSpace(title))
			{
				query = query.Where(e => e.Song.Titel.Contains(title));
			}

			var results = await query
				.OrderBy(e => e.Year)
				.ThenBy(e => e.Position)
				.Select(e => new Top2000EntryDto
				{
					SongId = e.SongId,
					Year = e.Year,
					Position = e.Position,
					SongTitle = e.Song.Titel,
					ArtistName = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					ImgUrl = e.Song.ImgUrl,
					Youtube = e.Song.Youtube
				})
				.ToListAsync();

			return Ok(results);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error searching entries");
			return StatusCode(500, new { message = "Error searching entries" });
		}
	}
}