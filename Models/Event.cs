using System.ComponentModel.DataAnnotations;

namespace WaldauCastle.Models;

public class Event
{
    public const string DefaultImagePath = "/images/tour-placeholder.svg";

    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Описание")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Изображение")]
    public string ImagePath { get; set; } = DefaultImagePath;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Дата мероприятия")]
    public DateTime EventDate { get; set; }

    [Display(Name = "Дата создания")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Дата изменения")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayImagePath =>
        string.IsNullOrWhiteSpace(ImagePath) ? DefaultImagePath : ImagePath;
}
