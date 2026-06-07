using System.ComponentModel.DataAnnotations;

namespace WaldauCastle.Models;

public class Booking
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ФИО может содержать от 2 до 200 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите телефон")]
    [Phone(ErrorMessage = "Введите корректный номер телефона")]
    [StringLength(20)]
    [Display(Name = "Телефон")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите дату визита")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата визита")]
    public DateTime VisitDate { get; set; }

    [Display(Name = "Вид экскурсии")]
    public ExcursionKind ExcursionKind { get; set; }

    [StringLength(200)]
    [Display(Name = "Экскурсия")]
    public string ExcursionTitle { get; set; } = string.Empty;

    [StringLength(5)]
    [Display(Name = "Время визита")]
    public string? VisitTime { get; set; }

    [Required(ErrorMessage = "Укажите количество человек")]
    [Range(1, 30, ErrorMessage = "От 1 до 30 человек")]
    [Display(Name = "Количество человек")]
    public int PersonsCount { get; set; }

    [Display(Name = "Дата заявки")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Согласие на обработку персональных данных")]
    public bool PersonalDataConsent { get; set; }
}
