// One time only script to bulk update songs with missing image url.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TemplateJwtProject.Data;
using TemplateJwtProject.Services;

[ApiController]
[Route("api/[controller]")]
public class ImageUpdateController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly ISpotifyService _spotifyService;
	private const int RequestDelayMs = 250; // Delay for rate limiting

	public ImageUpdateController(AppDbContext context, ISpotifyService spotifyService)
	{
		_context = context;
		_spotifyService = spotifyService;
	}

	/// <summary>
	/// Initiates the bulk lookup and update for all songs that have a SpotifyId but are missing the ImgUrl.
	/// </summary>
	[HttpPost("bulk-update")]
	public async Task<IActionResult> BulkUpdateImages()
	{
		// 1. Get all songs that have a SpotifyId but are missing the ImgUrl
		var songsToProcess = await _context.Songs
			.Where(s => s.SpotifyId != null && s.ImgUrl == null) // <-- CRITICAL FILTER
			.ToListAsync();

		var updatedCount = 0;
		var totalCount = songsToProcess.Count;

		// --- TOKEN MANAGEMENT SETUP (Same as previous controller) ---
		const int TokenRefreshInterval = 1000;
		string currentAccessToken = string.Empty;
		// -----------------------------------------------------------

		for (int i = 0; i < songsToProcess.Count; i++)
		{
			var song = songsToProcess[i];

			// 1a. CONDITIONAL TOKEN REFRESH
			if (i % TokenRefreshInterval == 0 || string.IsNullOrEmpty(currentAccessToken))
			{
				try
				{
					Console.WriteLine($"Fetching new Spotify Access Token (Iteration {i})...");
					currentAccessToken = await _spotifyService.GetNewAccessTokenAsync();
				}
				catch (Exception ex)
				{
					// Fatal error if we can't get a token
					return StatusCode(500, $"Fatal Error: Failed to retrieve Spotify Access Token. Details: {ex.Message}");
				}
			}

			try
			{
				// 2. Fetch the album details using the already saved Spotify ID
				// Note: We need a new service method for this, as the previous one searched by Title/Artist.
				// We'll call this GetAlbumDetailsByIdAsync (See Step 2 below)
				var albumDetails = await _spotifyService.GetAlbumDetailsByIdAsync(song.SpotifyId!, currentAccessToken);

				if (albumDetails != null && !string.IsNullOrEmpty(albumDetails.ImgUrl))
				{
					// 3. Update the ImgUrl column
					song.ImgUrl = albumDetails.ImgUrl;
					updatedCount++;
					Console.WriteLine($"SUCCESS: Found ImgUrl for Spotify ID {song.SpotifyId}");
				}
				else
				{
					// Log songs where the image could not be found (e.g., track deleted, data error)
					Console.WriteLine($"SKIPPED: Could not find album image for Spotify ID {song.SpotifyId}");
				}

				// 4. Rate Limiting Delay
				await Task.Delay(RequestDelayMs);
			}
			catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
			{
				Console.WriteLine("Rate limit hit (429). Pausing for 5 seconds.");
				await Task.Delay(5000);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing song with Spotify ID {song.SpotifyId}: {ex.Message}");
			}
		}

		// 5. Commit all changes to the database at once (batch update)
		await _context.SaveChangesAsync();

		return Ok($"Bulk image update complete. Processed {totalCount} songs. Updated ImgUrls for {updatedCount} songs.");
	}
}