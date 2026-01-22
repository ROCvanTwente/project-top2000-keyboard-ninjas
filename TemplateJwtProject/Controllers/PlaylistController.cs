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
				.OrderBy(p => p.DateAdded)
				.Select(p => new
				{
					p.SongId,
					p.Songs.Titel,
					p.Songs.Artist.ArtistId,
					p.Songs.Artist.Name,
					p.Songs.ReleaseYear,
					p.Songs.ImgUrl,
					p.Songs.SpotifyId,
					p.DateAdded
				})
				.ToListAsync();
			return Ok(playlist);
		}

		[HttpPost("add")]
		public async Task<ActionResult> AddSongToPlaylist([FromQuery] string userId, [FromQuery] int songId)
		{
			var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
			if (!userExists)
			{
				return NotFound("User not found");
			}

			var songExists = await _context.Songs.AnyAsync(s => s.SongId == songId);
			if (!songExists)
			{
				return NotFound("Song not found");
			}

			var alreadyInPlaylist = await _context.Playlist
				.AnyAsync(p => p.UserId == userId && p.SongId == songId);
			if (alreadyInPlaylist)
			{
				return BadRequest("Song is already in the playlist");
			}

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

		[HttpGet("check-song")]
		public async Task<ActionResult> IsSongInPlaylist([FromQuery] string userId, [FromQuery] int songId)
		{
			var isInPlaylist = await _context.Playlist
				.AnyAsync(p => p.UserId == userId && p.SongId == songId);
			return Ok(new { IsInPlaylist = isInPlaylist });
		}
	}
}
