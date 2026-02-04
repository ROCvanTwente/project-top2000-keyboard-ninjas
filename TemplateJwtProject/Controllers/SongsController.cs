using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;
using TemplateJwtProject.Models;

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

		[HttpPut]
		public async Task<ActionResult> UpdateSong([FromBody] Songs updatedSong)
		{
			var existingSong = await _context.Songs.FindAsync(updatedSong.SongId);
			if (existingSong == null)
			{
				return NotFound(new { message = $"Song with ID {updatedSong.SongId} not found" });
			}
			existingSong.ReleaseYear = updatedSong.ReleaseYear;
			existingSong.ImgUrl = updatedSong.ImgUrl;
			existingSong.Lyrics = updatedSong.Lyrics;
			await _context.SaveChangesAsync();
			return Ok(new { message = "Song updated successfully" });
		}
	}
}
