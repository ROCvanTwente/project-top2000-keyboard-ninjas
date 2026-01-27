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
	}
}
