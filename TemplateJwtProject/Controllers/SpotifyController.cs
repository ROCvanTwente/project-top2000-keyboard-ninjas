// One time only script to bulk update songs with missing Spotify IDs.
using Microsoft.AspNetCore.Mvc;

namespace TemplateJwtProject.Controllers
{
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.EntityFrameworkCore;
	using System.Net; // For HttpStatusCode
	using TemplateJwtProject.Data;
	using TemplateJwtProject.Services;

	[ApiController]
	[Route("api/[controller]")]
	public class SpotifyController : ControllerBase
	{
		private readonly AppDbContext _context; // Your DbContext
		private readonly ISpotifyService _spotifyService;
		private const int RequestDelayMs = 250; // Delay for rate limiting (4 requests per second)

		public SpotifyController(AppDbContext context, ISpotifyService spotifyService)
		{
			_context = context;
			_spotifyService = spotifyService;
		}

		/// <summary>
		/// Initiates the bulk lookup and update for all songs without a SpotifyTrackId.
		/// </summary>
		[HttpPost("bulk-update")]
		public async Task<IActionResult> BulkUpdateSongs()
		{
			// 1. Get all songs that need an ID
			var songsToProcess = await _context.Songs
				.Include(s => s.Artist)
				.Where(s => s.SpotifyId == null)
				.ToListAsync();

			var foundCount = 0;
			var totalCount = songsToProcess.Count;

			// --- TOKEN MANAGEMENT SETUP ---
			const int TokenRefreshInterval = 1000; // Refresh token every 1000 songs or at the start
			string currentAccessToken = string.Empty; // Holds the token string
													  // ------------------------------

			for (int i = 0; i < songsToProcess.Count; i++)
			{
				var song = songsToProcess[i];

				// 1a. CRITICAL: Check if we need to fetch a new token (at start, or every 'N' songs)
				if (i % TokenRefreshInterval == 0 || string.IsNullOrEmpty(currentAccessToken))
				{
					try
					{
						Console.WriteLine($"Fetching new Spotify Access Token (Iteration {i})...");
						currentAccessToken = await _spotifyService.GetNewAccessTokenAsync();
					}
					catch (Exception ex)
					{
						// If we can't get a token, stop the entire process as we cannot proceed.
						return StatusCode(500, $"Fatal Error: Failed to retrieve Spotify Access Token. Details: {ex.Message}");
					}
				}

				try
				{
					// 2. Search for the Track ID: PASS THE TOKEN!
					// Note: Assuming your song model properties are 'Titel' and 'Artist.Name'
					var trackId = await _spotifyService.GetTrackIdAsync(song.Titel, song.Artist.Name, currentAccessToken);

					if (!string.IsNullOrEmpty(trackId))
					{
						// 3. Update the database entry
						Console.WriteLine($"SUCCESS: Found ID {trackId} for {song.Titel} by {song.Artist.Name}");
						song.SpotifyId = trackId;
						foundCount++;
					}
					else
					{
						// 🚨 ADD THIS BLOCK 🚨
						Console.WriteLine($"SKIPPED: No Spotify ID found for {song.Titel} by {song.Artist.Name}");
					}

					// 4. Rate Limiting Delay - CRITICAL STEP
					await Task.Delay(RequestDelayMs);
				}
				catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429) // Too Many Requests
				{
					Console.WriteLine("Rate limit hit (429). Pausing for 5 seconds.");
					await Task.Delay(5000);
				}
				catch (Exception ex)
				{
					// Log the error and continue to the next song
					Console.WriteLine($"Error processing song {song.Titel} by {song.Artist.Name}: {ex.Message}");
				}
			}

			// 5. Commit all changes to the database at once (batch update)
			await _context.SaveChangesAsync();

			return Ok($"Bulk update complete. Processed {totalCount} songs. Found IDs for {foundCount} songs.");
		}
	}
}
