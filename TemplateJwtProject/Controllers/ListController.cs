using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;

namespace TemplateJwtProject.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ListController : ControllerBase
	{
		private readonly AppDbContext _context;

		public ListController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("{year}")]
		public async Task<ActionResult> GetListByYear(
			int year,
			[FromQuery] int? decade = null,
			[FromQuery] string? search = null)
		{
			var query = _context.Top2000Entries
				.Where(e => e.Year == year);

			// Apply decenium filter if provided
			if (decade.HasValue)
			{
				int decadeStart = decade.Value;
				int decadeEnd = decade.Value + 9;
				query = query.Where(e => e.Song.ReleaseYear >= decadeStart && e.Song.ReleaseYear <= decadeEnd);
			}

			// Apply search filter for both artist and song if provided
			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(e => 
					EF.Functions.Like(e.Song.Artist.Name, $"%{search}%") ||
					EF.Functions.Like(e.Song.Titel, $"%{search}%"));
			}

			var entries = await query
				.GroupJoin(
					_context.Top2000Entries.Where(e => e.Year == year - 1),
					currentYear => currentYear.SongId,
					previousYear => previousYear.SongId,
					(currentYear, previousYear) => new { currentYear, previousYear })
				.SelectMany(
					x => x.previousYear.DefaultIfEmpty(),
					(x, previousYear) => new { x.currentYear, previousYear })
				.OrderBy(e => e.currentYear.Position)
				.Select(e => new
				{
					e.currentYear.SongId,
					e.currentYear.Year,
					Position = e.currentYear.Position,
					PositionLastYear = e.previousYear != null ? e.previousYear.Position : (int?)null,
					Titel = e.currentYear.Song.Titel,
					ImgUrl = e.currentYear.Song.ImgUrl,
					ReleaseYear = e.currentYear.Song.ReleaseYear,
					Artist = e.currentYear.Song.Artist.Name
				})
				.ToListAsync();

			return Ok(entries);
		}
	}
}