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
		public async Task<ActionResult> GetListByYear(int year)
		{
			var entries = await _context.Top2000Entries
				.Where(e => e.Year == year)
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