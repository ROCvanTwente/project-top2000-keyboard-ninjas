using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TemplateJwtProject.Models
{
	[Table("Playlist")]
	[PrimaryKey(nameof(UserId), nameof(SongId))]
	public class Playlist
	{
		public string UserId { get; set; }
		public int SongId { get; set; }

		public ApplicationUser User { get; set; }
		public Songs Songs { get; set; }
	}
}
