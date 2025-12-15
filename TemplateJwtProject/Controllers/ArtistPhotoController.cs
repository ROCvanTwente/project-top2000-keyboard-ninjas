using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TemplateJwtProject.Data;
using TemplateJwtProject.Services;
// NOTE: Ensure your Artist Model is correctly referenced here
using TemplateJwtProject.Models;

[ApiController]
[Route("api/[controller]")]
public class ArtistPhotoController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly ISpotifyService _spotifyService;
	// Keeping the delay at 250ms for efficiency, but be mindful of the rate limit
	private const int RequestDelayMs = 250;

	public ArtistPhotoController(AppDbContext context, ISpotifyService spotifyService)
	{
		_context = context;
		_spotifyService = spotifyService;
	}

	/// <summary>
	/// Initiates the bulk lookup and update for all artists missing a Photo URL.
	/// It works by using a matched song's details to reliably find the Spotify Artist ID.
	/// </summary>
	[HttpPost("bulk-update")]
	public async Task<IActionResult> BulkUpdateArtistPhotos()
	{
		// 1. Get ALL Artists that are missing a Photo
		// This is the total pool we need to process. Assuming your column is named 'Photo'.
		var artistsToProcess = await _context.Artist
			.Where(a => a.Photo == null)
			.ToListAsync();

		var updatedCount = 0;
		var totalCount = artistsToProcess.Count;

		// --- TOKEN MANAGEMENT SETUP ---
		const int TokenRefreshInterval = 1000;
		string currentAccessToken = string.Empty;
		// ------------------------------

		Console.WriteLine($"Starting photo update for {totalCount} artists...");

		for (int i = 0; i < artistsToProcess.Count; i++)
		{
			var artist = artistsToProcess[i];

			// 1a. CONDITIONAL TOKEN REFRESH
			if (i % TokenRefreshInterval == 0 || string.IsNullOrEmpty(currentAccessToken))
			{
				try
				{
					currentAccessToken = await _spotifyService.GetNewAccessTokenAsync();
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Fatal Error: Failed to retrieve Spotify Access Token. Details: {ex.Message}");
				}
			}

			// 2. Find a reliable song associated with this Artist
			// We only look for songs that ALREADY have a SpotifyId, as these are confirmed matches.
			var matchedSong = await _context.Songs
				.FirstOrDefaultAsync(s => s.ArtistId == artist.ArtistId && s.SpotifyId != null);

			if (matchedSong == null)
			{
				// Skip artists for whom we haven't found a single matching song ID yet.
				Console.WriteLine($"SKIPPED: No matched Spotify track found for Artist: {artist.Name}.");
				continue;
			}

			try
			{
				// 3. Search the track to get the Spotify Primary Artist ID
				// We use the song's clean Title and Artist Name to get the full TrackDetails object
				// which includes the crucial PrimaryArtistId.
				var trackDetails = await _spotifyService.GetTrackDetailsAsync(matchedSong.Titel, artist.Name, currentAccessToken);

				if (string.IsNullOrEmpty(trackDetails?.PrimaryArtistId))
				{
					// This can happen if the original song match was tenuous.
					Console.WriteLine($"SKIPPED: Could not extract Spotify Artist ID for Artist: {artist.Name}.");
					continue;
				}

				// 4. Use the specific Spotify Artist ID to fetch the high-quality photo
				var photoUrl = await _spotifyService.GetArtistPhotoUrlAsync(trackDetails.PrimaryArtistId, currentAccessToken);

				if (!string.IsNullOrEmpty(photoUrl))
				{
					// 5. Update the Artist's Photo column
					artist.Photo = photoUrl; // Assuming your Artist model has a 'Photo' property
					updatedCount++;
					Console.WriteLine($"SUCCESS: Found Photo for Artist: {artist.Name}.");
				}

				// 6. Rate Limiting Delay
				await Task.Delay(RequestDelayMs);
			}
			catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
			{
				Console.WriteLine("Rate limit hit (429). Pausing for 5 seconds.");
				await Task.Delay(5000);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing artist {artist.Name}: {ex.Message}");
			}
		}

		// 7. Commit all changes to the database at once (batch update)
		await _context.SaveChangesAsync();

		return Ok($"Bulk artist photo update complete. Processed {totalCount} artists. Updated Photos for {updatedCount} artists.");
	}
}