using System.ComponentModel.DataAnnotations;

namespace TemplateJwtProject.Models.DTOs
{
	public class CurrentYearMaxAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value is int year && year >= 1 && year <= DateTime.Now.Year)
			{
				return ValidationResult.Success;
			}
			return new ValidationResult("Release year must be between 1 and the current year");
		}
	}

	public class SongUpdateDto
	{
		[Required]
		[CurrentYearMax]
		public int ReleaseYear { get; set; }
		public string? ImgUrl { get; set; }
		public string? Lyrics { get; set; }
	}
}
