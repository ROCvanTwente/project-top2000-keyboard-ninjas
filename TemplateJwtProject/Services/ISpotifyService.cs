// Services/ISpotifyService.cs

using static TemplateJwtProject.Services.SpotifyService;

public interface ISpotifyService
{
	// New method to get the token
	Task<string> GetNewAccessTokenAsync();

	// Updated method signature: now requires the token
	Task<string?> GetTrackIdAsync(string title, string artist, string accessToken);
	// NEW method for looking up details by ID (used for getting the image URL)
	Task<TrackDetails?> GetAlbumDetailsByIdAsync(string spotifyId, string accessToken);
}