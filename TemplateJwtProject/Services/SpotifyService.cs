using System.Text.Json.Serialization;
using System.Text.RegularExpressions; // Required for efficient cleaning

namespace TemplateJwtProject.Services
{
	public class SpotifyService : ISpotifyService
	{
		private readonly HttpClient _httpClient;

		// NOTE: Best practice is to load these from Configuration/Secrets
		private const string CLIENT_ID = "0cb2703b19c94145ab356ff98ca9e6ce";
		private const string CLIENT_SECRET = "6f8f155e91d6424a8a54fd61d7c3611e";
		private const string TOKEN_URL = "https://accounts.spotify.com/api/token";

		public SpotifyService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			// FIX: Ensure the URI has the protocol for the BaseAddress
			_httpClient.BaseAddress = new Uri("https://api.spotify.com/v1/");
		}

		/// <summary>
		/// Always fetches a brand new Access Token for the Client Credentials Flow.
		/// </summary>
		public async Task<string> GetNewAccessTokenAsync()
		{
			var tokenUrl = "https://accounts.spotify.com/api/token";

			var requestContent = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("grant_type", "client_credentials")
			});

			// Base64 encode the Client ID and Secret
			var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));
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

		// Helper model for token response
		private class SpotifyTokenResponse
		{
			[JsonPropertyName("expires_in")]
			public int ExpiresIn { get; set; }

			[JsonPropertyName("access_token")]
			public string? AccessToken { get; set; }
		}

		/// <summary>
		/// Searches Spotify for a track using a token provided by the caller (the Controller).
		/// </summary>
		public async Task<string?> GetTrackIdAsync(string title, string artist, string accessToken)
		{
			// 1. CLEAN THE DATA BEFORE SEARCHING
			var cleanedTitle = CleanTrackTitle(title);
			var cleanedArtist = CleanArtistName(artist);

			// Log the cleaned values for verification (optional)
			// Console.WriteLine($"DEBUG: Cleaned Title: '{cleanedTitle}', Cleaned Artist: '{cleanedArtist}'");

			// Set the Authorization header using the provided token
			_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

			// Use the search query format: q=track:"title" artist:"artist"
			// IMPORTANT: Use the cleaned variables here
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

		// Services/SpotifyService.cs (Add this new method)

		/// <summary>
		/// Fetches track details (specifically ImgUrl) using the known Spotify Track ID.
		/// </summary>
		public async Task<TrackDetails?> GetAlbumDetailsByIdAsync(string spotifyId, string accessToken)
		{
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

			var trackResult = await response.Content.ReadFromJsonAsync<TrackItem>(); // TrackItem is enough here

			if (trackResult == null || trackResult.Album?.Images == null)
			{
				return null;
			}

			// Extract the largest/best image URL
			var imageUrl = trackResult.Album.Images
				.OrderByDescending(i => i.Height)
				.FirstOrDefault()?.Url;

			return new TrackDetails
			{
				Id = trackResult.Id, // Already known, but included for completeness
				ImgUrl = imageUrl
			};
		}

		// --- NEW PRIVATE HELPER METHODS FOR CLEANING ---

		/// <summary>
		/// Removes featured artists (ft., feat.) from the artist name.
		/// </summary>
		private string CleanArtistName(string artist)
		{
			if (string.IsNullOrEmpty(artist)) return artist;

			// Define common "featured" separators
			string[] separators = { " ft. ", " feat. ", " featuring ", " x ", " & " };

			foreach (var sep in separators)
			{
				var index = artist.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
				if (index > 0)
				{
					// Return only the text before the separator
					return artist.Substring(0, index).Trim();
				}
			}
			return artist;
		}

		/// <summary>
		/// Removes version qualifiers (Albumversie, Live, Remix) from the song title.
		/// Uses RegEx to strip text within parentheses and brackets.
		/// </summary>
		private string CleanTrackTitle(string title)
		{
			if (string.IsNullOrEmpty(title)) return title;

			// Regex to match and remove text inside parentheses or square brackets
			// e.g., (Albumversie), (Remix), [Live], etc.
			// Pattern: \(.*?\)|\[.*?\] - Matches anything within () or [] non-greedily
			var cleanedTitle = Regex.Replace(title, @"\(.*?\)|\[.*?\]", string.Empty);

			return cleanedTitle.Trim();
		}


		// --- SpotifySearchResult Classes (Unchanged) ---
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

		public class TrackDetails
		{
			public string? Id { get; set; }
			public string? ImgUrl { get; set; }
		}

		public class Album
		{
			public List<ImageItem>? Images { get; set; } // <-- NEW
		}

		public class ArtistItem
		{
			public string? Name { get; set; }
		}


	}
}