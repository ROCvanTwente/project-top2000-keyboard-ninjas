// Services/ISpotifyService.cs

using static TemplateJwtProject.Services.SpotifyService;

public interface ISpotifyService
{
	// 1. Existing method: Token retrieval
	Task<string> GetNewAccessTokenAsync();

	// 2. Existing method: Track ID search (kept for backward compatibility)
	Task<string?> GetTrackIdAsync(string title, string artist, string accessToken);

	// 3. Existing method: Album Image lookup by Track ID
	Task<TrackDetails?> GetAlbumDetailsByIdAsync(string spotifyId, string accessToken);

	// 4. NEW method: Comprehensive Track Details search (Crucial for getting PrimaryArtistId)
	Task<TrackDetails?> GetTrackDetailsAsync(string title, string artist, string accessToken);

	// 5. NEW method: Artist Photo lookup by Artist ID
	Task<string?> GetArtistPhotoUrlAsync(string spotifyArtistId, string accessToken);
}