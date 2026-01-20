using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Data;

namespace TemplateJwtProject.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PlaylistController : ControllerBase
	{
		private readonly AppDbContext _context;

		public PlaylistController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("{userId}")]
		public async Task<ActionResult> GetPlaylist(string userId)
		{
			var playlist = await _context.Playlist
				.Where(p => p.UserId == userId)
				.Select(p => new
				{
					p.SongId,
					p.Songs.Titel,
					p.Songs.Artist.Name,
					p.Songs.ReleaseYear,
					p.Songs.ImgUrl,
					p.DateAdded
				})
				.ToListAsync();
			return Ok(playlist);
		}

		[HttpPost("add")]
		public async Task<ActionResult> AddSongToPlaylist([FromQuery] string userId, [FromQuery] int songId)
		{
			// TODO: Check if the song or user exists in the database before adding
			var playlistEntry = new Models.Playlist
			{
				UserId = userId,
				SongId = songId
			};
			_context.Playlist.Add(playlistEntry);
			await _context.SaveChangesAsync();
			return Ok();
		}

		[HttpPost("delete")]
		public async Task<ActionResult> RemoveSongFromPlaylist([FromQuery] string userId, [FromQuery] int songId)
		{
			var playlistEntry = await _context.Playlist
				.FirstOrDefaultAsync(p => p.UserId == userId && p.SongId == songId);
			if (playlistEntry == null)
			{
				return NotFound();
			}
			_context.Playlist.Remove(playlistEntry);
			await _context.SaveChangesAsync();
			return Ok();
		}
	}
}
