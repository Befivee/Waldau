using System.ComponentModel.DataAnnotations;
using WaldauCastle.Models;

namespace WaldauCastle.ViewModels;

public class EventEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название")]
    [StringLength(200)]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите описание")]
    [StringLength(2000)]
    [Display(Name = "Описание")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите дату")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата мероприятия")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddMonths(1);

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string ImagePath { get; set; } = Event.DefaultImagePath;

    [Display(Name = "Изображение")]
    public IFormFile? ImageFile { get; set; }

    public static EventEditViewModel FromEntity(Event entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        EventDate = entity.EventDate,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        ImagePath = entity.ImagePath
    };

    public Event ToEntity() => new()
    {
        Id = Id,
        Title = Title.Trim(),
        Description = Description.Trim(),
        EventDate = EventDate.Date,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        ImagePath = ImagePath
    };
}
