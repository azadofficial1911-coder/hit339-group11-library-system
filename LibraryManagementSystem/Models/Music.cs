using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Music : Item
    {
        [Required(ErrorMessage = "The artist's name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public string Artist { get; set; } = string.Empty;

        [Required(ErrorMessage = "The music genre is required.")]
        [StringLength(50)]
        [Display(Name = "Music Genre")]
        public string MusicGenre { get; set; } = string.Empty;

        [Range(1800, 2100)]
        [Display(Name = "Release Year")]
        public int ReleaseYear { get; set; }

        [Required(ErrorMessage = "The music format is required.")]
        [StringLength(30)]
        public string Format { get; set; } = string.Empty;
    }
}