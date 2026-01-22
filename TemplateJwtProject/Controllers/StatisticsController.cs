using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly ILogger<StatisticsController> _logger;

	public StatisticsController(AppDbContext context, ILogger<StatisticsController> logger)
	{
		_context = context;
		_logger = logger;
	}

	// GET: api/statistics
	[HttpGet]
	public async Task<ActionResult<StatisticsDto>> GetStatistics()
	{
		try
		{
			var totalSongs = await _context.Songs.CountAsync();
			var totalArtists = await _context.Artist.CountAsync();
			var totalEntries = await _context.Top2000Entries.CountAsync();
			var totalYears = await _context.Top2000Entries
				.Select(e => e.Year)
				.Distinct()
				.CountAsync();

			var statistics = new StatisticsDto
			{
				TotalSongs = totalSongs,
				TotalArtists = totalArtists,
				TotalEntries = totalEntries,
				TotalYears = totalYears
			};

			return Ok(statistics);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving statistics");
			return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
		}
	}

	// GET: api/statistics/detailed
	[HttpGet("detailed")]
	public async Task<ActionResult> GetDetailedStatistics()
	{
		try
		{
			var totalSongs = await _context.Songs.CountAsync();
			var totalArtists = await _context.Artist.CountAsync();
			var totalEntries = await _context.Top2000Entries.CountAsync();

			var years = await _context.Top2000Entries
				.Select(e => e.Year)
				.Distinct()
				.OrderDescending()
				.ToListAsync();

			// Top 10 artiesten met de meeste nummers
			var topArtists = await _context.Artist
				.Select(a => new
				{
					ArtistId = a.ArtistId,
					Name = a.Name,
					Photo = a.Photo,
					SongCount = a.Songs.Count()
				})
				.OrderByDescending(a => a.SongCount)
				.Take(10)
				.ToListAsync();

			// Top 10 nummers die het vaakst in de lijst staan
			var mostFrequentSongs = await _context.Top2000Entries
				.GroupBy(e => e.SongId)
				.Select(g => new
				{
					SongId = g.Key,
					Appearances = g.Count(),
					BestPosition = g.Min(e => e.Position),
					Song = g.First().Song
				})
				.OrderByDescending(s => s.Appearances)
				.ThenBy(s => s.BestPosition)
				.Take(10)
				.Select(s => new
				{
					s.SongId,
					Title = s.Song.Titel,
					Artist = s.Song.Artist.Name,
					s.Appearances,
					s.BestPosition
				})
				.ToListAsync();

			var detailedStats = new
			{
				TotalSongs = totalSongs,
				TotalArtists = totalArtists,
				TotalEntries = totalEntries,
				TotalYears = years.Count,
				AvailableYears = years,
				TopArtists = topArtists,
				MostFrequentSongs = mostFrequentSongs
			};

			return Ok(detailedStats);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving detailed statistics");
			return StatusCode(500, new { message = "Error retrieving detailed statistics", error = ex.Message });
		}
	}

	// GET: api/statistics/year/{year}
	[HttpGet("year/{year}")]
	public async Task<ActionResult> GetYearStatistics(int year)
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

			// Decade verdeling
			var decadeDistribution = entries
				.Where(e => e.Song.ReleaseYear.HasValue)
				.GroupBy(e => (e.Song.ReleaseYear!.Value / 10) * 10)
				.Select(g => new
				{
					Decade = g.Key,
					Count = g.Count()
				})
				.OrderBy(d => d.Decade)
				.ToList();

			// Top 10 artiesten van dat jaar
			var topArtistsOfYear = entries
				.GroupBy(e => new { e.Song.Artist.ArtistId, e.Song.Artist.Name })
				.Select(g => new
				{
					g.Key.ArtistId,
					g.Key.Name,
					SongCount = g.Count()
				})
				.OrderByDescending(a => a.SongCount)
				.Take(10)
				.ToList();

			var yearStats = new
			{
				Year = year,
				TotalEntries = entries.Count,
				UniqueArtists = entries.Select(e => e.Song.ArtistId).Distinct().Count(),
				DecadeDistribution = decadeDistribution,
				TopArtists = topArtistsOfYear,
				OldestSong = entries
					.Where(e => e.Song.ReleaseYear.HasValue)
					.OrderBy(e => e.Song.ReleaseYear)
					.Select(e => new
					{
						e.SongId,
						Title = e.Song.Titel,
						Artist = e.Song.Artist.Name,
						ReleaseYear = e.Song.ReleaseYear,
						Position = e.Position
					})
					.FirstOrDefault(),
				NewestSong = entries
					.Where(e => e.Song.ReleaseYear.HasValue)
					.OrderByDescending(e => e.Song.ReleaseYear)
					.Select(e => new
					{
						e.SongId,
						Title = e.Song.Titel,
						Artist = e.Song.Artist.Name,
						ReleaseYear = e.Song.ReleaseYear,
						Position = e.Position
					})
					.FirstOrDefault()
			};

			return Ok(yearStats);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving statistics for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving year statistics", error = ex.Message });
		}
	}

	// GET: api/statistics/one-day-flies/{year}
	// Haal alle eendagsvliegen op voor een specifiek jaar (nummers die maar 1x in de Top 2000 voorkomen)
	[HttpGet("one-day-flies/{year}")]
	public async Task<ActionResult<IEnumerable<OneDayFlyDto>>> GetOneDayFlies(int year)
	{
		try
		{
			// Haal eerst alle SongIds op die maar 1 keer voorkomen
			var songsThatAppearOnlyOnce = await _context.Top2000Entries
				.GroupBy(e => e.SongId)
				.Where(g => g.Count() == 1)
				.Select(g => g.Key)
				.ToListAsync();

			// Haal dan de entries op van die nummers voor het gevraagde jaar
			var oneDayFlies = await _context.Top2000Entries
				.Where(e => songsThatAppearOnlyOnce.Contains(e.SongId) && e.Year == year)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.OrderBy(e => e.Position)
				.Select(e => new OneDayFlyDto
				{
					SongId = e.SongId,
					Year = e.Year,
					Position = e.Position,
					Title = e.Song.Titel,
					Artist = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					ImgUrl = e.Song.ImgUrl
				})
				.ToListAsync();

			if (!oneDayFlies.Any())
			{
				return Ok(new List<OneDayFlyDto>()); // Lege lijst teruggeven in plaats van 404
			}

			return Ok(oneDayFlies);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving one-day flies for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving one-day flies", error = ex.Message });
		}
	}

	// GET: api/statistics/one-day-flies
	// Haal alle eendagsvliegen op van alle jaren
	[HttpGet("one-day-flies")]
	public async Task<ActionResult<IEnumerable<OneDayFlyDto>>> GetAllOneDayFlies()
	{
		try
		{
			// Haal eerst alle SongIds op die maar 1 keer voorkomen
			var songsThatAppearOnlyOnce = await _context.Top2000Entries
				.GroupBy(e => e.SongId)
				.Where(g => g.Count() == 1)
				.Select(g => g.Key)
				.ToListAsync();

			// Haal dan alle entries op van die nummers
			var oneDayFlies = await _context.Top2000Entries
				.Where(e => songsThatAppearOnlyOnce.Contains(e.SongId))
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.OrderByDescending(e => e.Year)
				.ThenBy(e => e.Position)
				.Select(e => new OneDayFlyDto
				{
					SongId = e.SongId,
					Year = e.Year,
					Position = e.Position,
					Title = e.Song.Titel,
					Artist = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					ImgUrl = e.Song.ImgUrl
				})
				.ToListAsync();

			return Ok(oneDayFlies);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving all one-day flies");
			return StatusCode(500, new { message = "Error retrieving one-day flies", error = ex.Message });
		}
	}

	// GET: api/statistics/newest-entry/{year}
	// Haal het hoogste nieuwe binnenkomer liedje op (nieuw nummer met de beste positie)
	[HttpGet("newest-entry/{year}")]
	public async Task<ActionResult<HighestNewEntryDto>> GetNewestEntry(int year)
	{
		try
		{
			var previousYear = year - 1;

			// Gebruik LEFT JOIN logica zoals in SQL query om nieuwe entries te vinden
			var highestNewEntry = await (
				from currentYearEntry in _context.Top2000Entries
				join previousYearEntry in _context.Top2000Entries
					on currentYearEntry.SongId equals previousYearEntry.SongId into prevGroup
				from prevEntry in prevGroup.Where(p => p.Year == previousYear).DefaultIfEmpty()
				where currentYearEntry.Year == year
					&& prevEntry == null  // Nieuwe entry (stond niet in vorig jaar)
				orderby currentYearEntry.Position ascending  // Laagste positie nummer = hoogst in de lijst
				select new HighestNewEntryDto
				{
					SongId = currentYearEntry.SongId,
					Title = currentYearEntry.Song.Titel,
					Artist = currentYearEntry.Song.Artist.Name,
					ReleaseYear = currentYearEntry.Song.ReleaseYear,
					Position = currentYearEntry.Position,
					Year = currentYearEntry.Year,
					ImgUrl = currentYearEntry.Song.ImgUrl,
					IsNewEntry = true
				}
			).FirstOrDefaultAsync();

			if (highestNewEntry == null)
			{
				return NotFound(new { message = $"No new entries found for year {year}" });
			}

			return Ok(highestNewEntry);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving highest new entry for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving highest new entry", error = ex.Message });
		}
	}

	// GET: api/statistics/oldest-song
	// Haal het oudste nummer ooit op (over alle jaren heen)
	[HttpGet("oldest-song")]
	public async Task<ActionResult<OldestSongDto>> GetOldestSongEver()
	{
		try
		{
			// Zoek het nummer met het laagste release jaar over alle entries
			var oldestSongEntry = await _context.Songs
				.Where(s => s.ReleaseYear.HasValue && s.Top2000Entries.Any())
				.Include(s => s.Artist)
				.Include(s => s.Top2000Entries)
				.OrderBy(s => s.ReleaseYear)
				.ThenBy(s => s.Top2000Entries.Min(e => e.Year))  // Bij gelijke ReleaseYear, pak het eerste jaar in de lijst
				.FirstOrDefaultAsync();

			if (oldestSongEntry == null)
			{
				return NotFound(new { message = "No songs with release year found" });
			}

			// Vind het eerste jaar waarin dit nummer in de Top 2000 stond
			var firstAppearance = oldestSongEntry.Top2000Entries
				.OrderBy(e => e.Year)
				.First();

			var oldestSong = new OldestSongDto
			{
				SongId = oldestSongEntry.SongId,
				Title = oldestSongEntry.Titel,
				Artist = oldestSongEntry.Artist.Name,
				ReleaseYear = oldestSongEntry.ReleaseYear,
				Position = firstAppearance.Position,
				Year = firstAppearance.Year,  // Eerste jaar in Top 2000
				ImgUrl = oldestSongEntry.ImgUrl
			};

			return Ok(oldestSong);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving oldest song ever");
			return StatusCode(500, new { message = "Error retrieving oldest song", error = ex.Message });
		}
	}

	// GET: api/statistics/oldest-song/{year}
	// Haal het oudste nummer op van een specifiek jaar
	[HttpGet("oldest-song/{year}")]
	public async Task<ActionResult<OldestSongDto>> GetOldestSongOfYear(int year)
	{
		try
		{
			var oldestSong = await _context.Top2000Entries
				.Where(e => e.Year == year && e.Song.ReleaseYear.HasValue)
				.Include(e => e.Song)
					.ThenInclude(s => s.Artist)
				.OrderBy(e => e.Song.ReleaseYear)
				.ThenBy(e => e.Position)
				.Select(e => new OldestSongDto
				{
					SongId = e.SongId,
					Title = e.Song.Titel,
					Artist = e.Song.Artist.Name,
					ReleaseYear = e.Song.ReleaseYear,
					Position = e.Position,
					Year = e.Year,
					ImgUrl = e.Song.ImgUrl
				})
				.FirstOrDefaultAsync();

			if (oldestSong == null)
			{
				return NotFound(new { message = $"No entries with release year found for year {year}" });
			}

			return Ok(oldestSong);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving oldest song for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving oldest song", error = ex.Message });
		}
	}

	// GET: api/statistics/biggest-risers/{year}
	// Haal de grootste stijgers op (nummers die het meest zijn gestegen t.o.v. vorig jaar)
	[HttpGet("biggest-risers/{year}")]
	public async Task<ActionResult<IEnumerable<RiserDto>>> GetBiggestRisers(int year, [FromQuery] int top = 10)
	{
		try
		{
			if (top <= 0 || top > 100)
			{
				return BadRequest(new { message = "Top parameter must be between 1 and 100" });
			}

			var previousYear = year - 1;

			// Gebruik INNER JOIN om alleen nummers te krijgen die in beide jaren staan
			var risers = await (
				from currentYearEntry in _context.Top2000Entries
				join previousYearEntry in _context.Top2000Entries
					on currentYearEntry.SongId equals previousYearEntry.SongId
				where currentYearEntry.Year == year 
					&& previousYearEntry.Year == previousYear
				let rise = previousYearEntry.Position - currentYearEntry.Position  // Positief = gestegen
				orderby rise descending  // Grootste stijging eerst
				select new RiserDto
				{
					SongId = currentYearEntry.SongId,
					Title = currentYearEntry.Song.Titel,
					Artist = currentYearEntry.Song.Artist.Name,
					CurrentYearPosition = currentYearEntry.Position,
					PreviousYearPosition = previousYearEntry.Position,
					Rise = rise,
					Year = currentYearEntry.Year,
					ImgUrl = currentYearEntry.Song.ImgUrl
				}
			).Take(top).ToListAsync();

			if (!risers.Any())
			{
				return Ok(new List<RiserDto>());  // Lege lijst als er geen data is
			}

			return Ok(risers);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving biggest risers for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving biggest risers", error = ex.Message });
		}
	}

	// GET: api/statistics/biggest-fallers/{year}
	// Haal de grootste dalers op (nummers die het meest zijn gedaald t.o.v. vorig jaar)
	[HttpGet("biggest-fallers/{year}")]
	public async Task<ActionResult<IEnumerable<RiserDto>>> GetBiggestFallers(int year, [FromQuery] int top = 10)
	{
		try
		{
			if (top <= 0 || top > 100)
			{
				return BadRequest(new { message = "Top parameter must be between 1 and 100" });
			}

			var previousYear = year - 1;

			// Gebruik INNER JOIN om alleen nummers te krijgen die in beide jaren staan
			var fallers = await (
				from currentYearEntry in _context.Top2000Entries
				join previousYearEntry in _context.Top2000Entries
					on currentYearEntry.SongId equals previousYearEntry.SongId
				where currentYearEntry.Year == year 
					&& previousYearEntry.Year == previousYear
				let rise = previousYearEntry.Position - currentYearEntry.Position  // Negatief = gedaald
				orderby rise ascending  // Grootste daling eerst (meest negatief)
				select new RiserDto
				{
					SongId = currentYearEntry.SongId,
					Title = currentYearEntry.Song.Titel,
					Artist = currentYearEntry.Song.Artist.Name,
					CurrentYearPosition = currentYearEntry.Position,
					PreviousYearPosition = previousYearEntry.Position,
					Rise = rise,  // Negatief getal = gedaald
					Year = currentYearEntry.Year,
					ImgUrl = currentYearEntry.Song.ImgUrl
				}
			).Take(top).ToListAsync();

			if (!fallers.Any())
			{
				return Ok(new List<RiserDto>());  // Lege lijst als er geen data is
			}

			return Ok(fallers);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving biggest fallers for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving biggest fallers", error = ex.Message });
		}
	}

	// GET: api/statistics/same-position/{year}
	// Haal alle nummers op die op dezelfde positie staan als vorig jaar
	[HttpGet("same-position/{year}")]
	public async Task<ActionResult<IEnumerable<RiserDto>>> GetSamePosition(int year)
	{
		try
		{
			var previousYear = year - 1;

			// Gebruik INNER JOIN om nummers te vinden die op dezelfde positie staan
			var samePositionSongs = await (
				from currentYearEntry in _context.Top2000Entries
				join previousYearEntry in _context.Top2000Entries
					on new { currentYearEntry.SongId, Position = currentYearEntry.Position } 
					equals new { previousYearEntry.SongId, Position = previousYearEntry.Position }
				where currentYearEntry.Year == year 
					&& previousYearEntry.Year == previousYear
				orderby currentYearEntry.Position ascending  // Sorteer op positie
				select new RiserDto
				{
					SongId = currentYearEntry.SongId,
					Title = currentYearEntry.Song.Titel,
					Artist = currentYearEntry.Song.Artist.Name,
					CurrentYearPosition = currentYearEntry.Position,
					PreviousYearPosition = previousYearEntry.Position,
					Rise = 0,  // Geen verandering
					Year = currentYearEntry.Year,
					ImgUrl = currentYearEntry.Song.ImgUrl
				}
			).ToListAsync();

			if (!samePositionSongs.Any())
			{
				return Ok(new List<RiserDto>());  // Lege lijst als er geen data is
			}

			return Ok(samePositionSongs);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving songs with same position for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving songs with same position", error = ex.Message });
		}
	}

	// GET: api/statistics/evergreens/{year}
	// Haal alle evergreens op (nummers die in elk jaar vanaf 1999 tot het opgegeven jaar in de lijst staan)
	[HttpGet("evergreens/{year}")]
	public async Task<ActionResult<IEnumerable<EvergreenDto>>> GetEvergreens(int year)
	{
		try
		{
			const int startYear = 1999;  // Top 2000 begon in 1999
			
			if (year < startYear)
			{
				return BadRequest(new { message = $"Year must be {startYear} or later" });
			}

			// Bereken het totaal aantal jaren tussen 1999 en year (inclusief)
			var totalYears = year - startYear + 1;

			// Haal alle nummers op die in elk jaar sinds 1999 voorkomen
			var evergreens = await (
				from entry in _context.Top2000Entries
				where entry.Year >= startYear && entry.Year <= year
				group entry by new 
				{ 
					entry.SongId, 
					entry.Song.Titel, 
					entry.Song.Artist.Name,
					entry.Song.ReleaseYear,
					entry.Song.ImgUrl
				} into g
				where g.Select(e => e.Year).Distinct().Count() == totalYears  // Moet in elk jaar voorkomen
				select new EvergreenDto
				{
					SongId = g.Key.SongId,
					Title = g.Key.Titel,
					Artist = g.Key.Name,
					YearsInList = g.Select(e => e.Year).Distinct().Count(),
					ReleaseYear = g.Key.ReleaseYear,
					ImgUrl = g.Key.ImgUrl,
					BestPosition = g.Min(e => e.Position)
				}
			).OrderBy(e => e.Title).ToListAsync();

			if (!evergreens.Any())
			{
				return Ok(new List<EvergreenDto>());  // Lege lijst als er geen evergreens zijn
			}

			return Ok(evergreens);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving evergreens for year {Year}", year);
			return StatusCode(500, new { message = "Error retrieving evergreens", error = ex.Message });
		}
	}

    // GET: api/statistics/consecutive-songs/{year}
    // Haalt artiesten op die met twee nummers na elkaar in de lijst staan
    [HttpGet("consecutive-songs/{year}")]
    public async Task<ActionResult<IEnumerable<ConsecutiveSongsDto>>> GetConsecutiveSongs(int year)
    {
        try
        {
            var entries = await _context.Top2000Entries
                .Where(e => e.Year == year)
                .OrderBy(e => e.Position)
                .Select(e => new 
                {
                    e.Position,
                    e.Song.ArtistId,
                    ArtistName = e.Song.Artist.Name,
                    SongTitle = e.Song.Titel
                })
                .ToListAsync();

            if (entries.Count < 2)
            {
                return Ok(new List<ConsecutiveSongsDto>());
            }

            var consecutiveSongs = new List<ConsecutiveSongsDto>();
            for (int i = 0; i < entries.Count - 1; i++)
            {
                var currentEntry = entries[i];
                var nextEntry = entries[i + 1];

                if (currentEntry.ArtistId == nextEntry.ArtistId &&
                    nextEntry.Position == currentEntry.Position + 1)
                {
                    consecutiveSongs.Add(new ConsecutiveSongsDto
                    {
                        Artist = currentEntry.ArtistName,
                        Title1 = currentEntry.SongTitle,
                        Position1 = currentEntry.Position,
                        Title2 = nextEntry.SongTitle,
                        Position2 = nextEntry.Position
                    });
                }
            }

            return Ok(consecutiveSongs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consecutive songs for year {Year}", year);
            return StatusCode(500, new { message = "Error retrieving consecutive songs", error = ex.Message });
        }
    }

}