using System.Text.Json.Serialization;
using System.Text.RegularExpressions; // Required for efficient cleaning
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace TemplateJwtProject.Services
{
	// NOTE: Ensure ISpotifyService interface is defined elsewhere, though not provided here.
	public class SpotifyService : ISpotifyService // Assumes ISpotifyService now contains all the required methods
	{
		private readonly HttpClient _httpClient;

		// NOTE: Best practice is to load these from Configuration/Secrets
		// IMPORTANT: Client ID and Secret are hardcoded here for simplicity but should be secured.
		private const string CLIENT_ID = "0cb2703b19c94145ab356ff98ca9e6ce";
		private const string CLIENT_SECRET = "6f8f155e91d6424a8a54fd61d7c3611e";
		private const string TOKEN_URL = "https://accounts.spotify.com/api/token"; // Mock/Placeholder for Token URL

		public SpotifyService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			// FIX: Ensure the URI has the protocol for the BaseAddress
			// Base Address for Spotify Web API endpoints (e.g., search, tracks)
			_httpClient.BaseAddress = new Uri("https://api.spotify.com/v1/"); // Mock/Placeholder for API Base URL
		}

		// --- PUBLIC API ACCESS METHODS ---

		/// <summary>
		/// Always fetches a brand new Access Token for the Client Credentials Flow.
		/// </summary>
		/// <returns>The Spotify Access Token string.</returns>
		/// <exception cref="Exception">Thrown if the token request fails or the response is invalid.</exception>
		public async Task<string> GetNewAccessTokenAsync()
		{
			var tokenUrl = TOKEN_URL;

			var requestContent = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("grant_type", "client_credentials")
			});

			// Base64 encode the Client ID and Secret for Basic Authentication
			var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));

			// Set the Authorization header for the token request
			_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

			using var response = await _httpClient.PostAsync(tokenUrl, requestContent);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>();

			if (content == null || string.IsNullOrEmpty(content.AccessToken) || content.ExpiresIn <= 0)
			{
				throw new Exception("Invalid or missing access token/expiration time in Spotify token response.");
			}

			return content.AccessToken;
		}

		/// <summary>
		/// Searches Spotify for a track using a token provided by the caller (the Controller).
		/// </summary>
		/// <remarks>KEPT FOR BACKWARD COMPATIBILITY, BUT NEW METHOD SHOULD BE USED FOR FULL DETAILS.</remarks>
		public async Task<string?> GetTrackIdAsync(string title, string artist, string accessToken)
		{
			// ... (Existing implementation remains unchanged) ...

			// 1. CLEAN THE DATA BEFORE SEARCHING
			var cleanedTitle = CleanTrackTitle(title);
			var cleanedArtist = CleanArtistName(artist);

			// Set the Authorization header using the provided token
			_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

			// Use the search query format: q=track:"title" artist:"artist"
			var query = Uri.EscapeDataString($"track:\"{cleanedTitle}\" artist:\"{cleanedArtist}\"");
			var requestUrl = $"search?q={query}&type=track&limit=1";

			Console.WriteLine($"DEBUG: Searching Spotify for: {requestUrl}");

			using var response = await _httpClient.GetAsync(requestUrl);

			if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
			response.EnsureSuccessStatusCode();

			var searchResult = await response.Content.ReadFromJsonAsync<SpotifySearchResult>();

			// Return the ID of the first track found, or null if tracks list is empty
			return searchResult?.Tracks?.Items?.FirstOrDefault()?.Id;
		}

		// ====================================================================
		// NEW METHOD: Comprehensive Track Search (REQUIRED FOR ARTIST PHOTO)
		// ====================================================================

		/// <summary>
		/// Searches Spotify for a track and returns all relevant details (ID, ImgUrl, PrimaryArtistId).
		/// </summary>
		public async Task<TrackDetails?> GetTrackDetailsAsync(string title, string artist, string accessToken)
		{
			// 1. CLEAN THE DATA BEFORE SEARCHING
			var cleanedTitle = CleanTrackTitle(title);
			var cleanedArtist = CleanArtistName(artist);

			// Set the Authorization header using the provided token
			_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

			// Use the search query format: q=track:"title" artist:"artist"
			var query = Uri.EscapeDataString($"track:\"{cleanedTitle}\" artist:\"{cleanedArtist}\"");
			var requestUrl = $"search?q={query}&type=track&limit=1";

			Console.WriteLine($"DEBUG: Searching Spotify for full details: {requestUrl}");

			using var response = await _httpClient.GetAsync(requestUrl);

			if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
			response.EnsureSuccessStatusCode();

			var searchResult = await response.Content.ReadFromJsonAsync<SpotifySearchResult>();
			var track = searchResult?.Tracks?.Items?.FirstOrDefault();

			if (track == null || string.IsNullOrEmpty(track.Id))
			{
				return null;
			}

			// Extract Image URL
			var imageUrl = track.Album?.Images?
				.OrderByDescending(i => i.Height)
				.FirstOrDefault()?.Url;

			// Extract Primary Artist ID (CRITICAL STEP)
			var primaryArtistId = track.Artists?.FirstOrDefault()?.Id;

			return new TrackDetails
			{
				Id = track.Id,
				ImgUrl = imageUrl,
				PrimaryArtistId = primaryArtistId // Now correctly captured
			};
		}


		/// <summary>
		/// Fetches track details (specifically ImgUrl) using the known Spotify Track ID.
		/// </summary>
		public async Task<TrackDetails?> GetAlbumDetailsByIdAsync(string spotifyId, string accessToken)
		{
			// ... (Existing implementation remains unchanged) ...

			// Set the Authorization header
			_httpClient.DefaultRequestHeaders.Authorization =
			new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

			// Endpoint for getting a track by ID is /tracks/{id}
			var requestUrl = $"tracks/{spotifyId}";

			using var response = await _httpClient.GetAsync(requestUrl);

			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Error fetching details for {spotifyId}: {response.StatusCode}");
				return null;
			}

			var trackResult = await response.Content.ReadFromJsonAsync<TrackItem>();

			if (trackResult == null || trackResult.Album?.Images == null)
			{
				return null;
			}

			// Extract the largest/best image URL by sorting by Height descending
			var imageUrl = trackResult.Album.Images
				.OrderByDescending(i => i.Height)
				.FirstOrDefault()?.Url;

			return new TrackDetails
			{
				Id = trackResult.Id, // Already known, but included for completeness
				ImgUrl = imageUrl
			};
		}

		// ====================================================================
		// NEW METHOD: Artist Photo Lookup
		// ====================================================================

		/// <summary>
		/// Fetches the Artist's photo URL using the known Spotify Artist ID.
		/// </summary>
		public async Task<string?> GetArtistPhotoUrlAsync(string spotifyArtistId, string accessToken)
		{
			// Set the Authorization header
			_httpClient.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

			// Endpoint for getting an artist by ID is /artists/{id}
			var requestUrl = $"artists/{spotifyArtistId}"; // 

			using var response = await _httpClient.GetAsync(requestUrl);

			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Error fetching artist details for {spotifyArtistId}: {response.StatusCode}");
				return null;
			}

			// Uses the corrected ArtistDetails model which includes the Images property
			var artistResult = await response.Content.ReadFromJsonAsync<ArtistDetails>();

			if (artistResult == null || artistResult.Images == null)
			{
				return null;
			}

			// Extract the largest/best image URL
			var photoUrl = artistResult.Images
				.OrderByDescending(i => i.Height)
				.FirstOrDefault()?.Url;

			return photoUrl;
		}


		// --- PRIVATE HELPER METHODS FOR CLEANING (Unchanged) ---

		private string CleanArtistName(string artist)
		{
			if (string.IsNullOrEmpty(artist)) return artist;
			string[] separators = { " ft. ", " feat. ", " featuring ", " x ", " & " };
			foreach (var sep in separators)
			{
				var index = artist.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
				if (index > 0) return artist.Substring(0, index).Trim();
			}
			return artist;
		}

		private string CleanTrackTitle(string title)
		{
			if (string.IsNullOrEmpty(title)) return title;
			var cleanedTitle = Regex.Replace(title, @"\(.*?\)|\[.*?\]", string.Empty);
			return cleanedTitle.Trim();
		}


		// --- PRIVATE DATA TRANSFER OBJECTS (DTOs) ---

		private class SpotifyTokenResponse
		{
			[JsonPropertyName("expires_in")]
			public int ExpiresIn { get; set; }

			[JsonPropertyName("access_token")]
			public string? AccessToken { get; set; }
		}

		// --- Spotify Search Result DTOs ---

		public class SpotifySearchResult
		{
			public TracksContainer? Tracks { get; set; }
		}

		public class TracksContainer
		{
			public List<TrackItem>? Items { get; set; }
		}

		public class ImageItem
		{
			public string? Url { get; set; }
			public int Height { get; set; }
			public int Width { get; set; }
		}

		public class TrackItem
		{
			public string? Id { get; set; }
			public string? Name { get; set; }
			public List<ArtistItem>? Artists { get; set; }
			public Album? Album { get; set; }
		}

		public class Album
		{
			public List<ImageItem>? Images { get; set; }
		}

		// Artist Item DTO for the list found inside a Track Search Result
		public class ArtistItem
		{
			public string? Id { get; set; } // CRITICAL: Added for capturing PrimaryArtistId
			public string? Name { get; set; }
		}

		// --- Public Output DTOs ---

		/// <summary>
		/// Final DTO returned by GetTrackDetailsAsync/GetAlbumDetailsByIdAsync.
		/// </summary>
		public class TrackDetails
		{
			public string? Id { get; set; }
			public string? ImgUrl { get; set; }
			// CRITICAL: Now populated in GetTrackDetailsAsync
			public string? PrimaryArtistId { get; set; }
		}

		/// <summary>
		/// DTO for the direct Artist Lookup (endpoint: /artists/{id}).
		/// </summary>
		public class ArtistDetails
		{
			public List<ImageItem>? Images { get; set; } // Must be public for deserialization
		}
	}
}