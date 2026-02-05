using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;
using TemplateJwtProject.Models;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SongsController : ControllerBase
	{
		private readonly AppDbContext _context;

		public SongsController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetAllSongs([FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? search = null)
		{
			if (page < 1) page = 1;
			if (pageSize < 1 || pageSize > 100) pageSize = 100;

			var query = _context.Songs.Include(s => s.Artist).AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				var searchLower = search.ToLower();
				query = query.Where(s =>
					s.Titel.ToLower().Contains(searchLower) ||
					s.Artist.Name.ToLower().Contains(searchLower) ||
					s.ReleaseYear.ToString().Contains(search));
			}

			var totalCount = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

			var songs = await query
				.OrderBy(s => s.SongId)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(s => new
				{
					s.SongId,
					s.Titel,
					s.ReleaseYear,
					s.ImgUrl,
					s.Lyrics,
					Artist = s.Artist.Name
				})
				.ToListAsync();

			return Ok(new
			{
				data = songs,
				pagination = new
				{
					currentPage = page,
					pageSize,
					totalCount,
					totalPages
				}
			});
		}

		[HttpPut("{id}")]
		public async Task<ActionResult> UpdateSong(int id, [FromBody] SongUpdateDto dto)
		{
			var existingSong = await _context.Songs.FindAsync(id);

			if (existingSong == null)
			{
				return NotFound(new { message = $"Song with ID {id} not found" });
			}

			// Map only the allowed fields
			existingSong.ReleaseYear = dto.ReleaseYear;
			existingSong.ImgUrl = dto.ImgUrl;
			existingSong.Lyrics = dto.Lyrics;

			await _context.SaveChangesAsync();
			return Ok(new { message = "Song updated successfully" });
		}
	}
}
