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
		public async Task<ActionResult> GetAllSongs()
		{
			var songs = await _context.Songs
				.Include(s => s.Artist)
				.OrderBy(s => s.SongId)
				.Select(s => new
				{
					s.SongId,
					s.Titel,
					s.ReleaseYear,
					s.ImgUrl,
					s.Lyrics,
					s.Youtube,
					Artist = s.Artist.Name
				})
				.ToListAsync();

			return Ok(songs);
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
