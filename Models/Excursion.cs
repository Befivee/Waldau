using System.ComponentModel.DataAnnotations;

namespace WaldauCastle.Models;

public class Excursion
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

    [Required]
    [StringLength(50)]
    [Display(Name = "Длительность")]
    public string Duration { get; set; } = string.Empty;

    [Required]
    [Range(0, 100000)]
    [Display(Name = "Цена")]
    public decimal Price { get; set; }

    [StringLength(500)]
    [Display(Name = "Изображение")]
    public string ImagePath { get; set; } = DefaultImagePath;

    public string DisplayImagePath =>
        string.IsNullOrWhiteSpace(ImagePath) ? DefaultImagePath : ImagePath;

    public string DisplayDuration => Helpers.ExcursionDuration.Normalize(Duration);
}
