using System.ComponentModel.DataAnnotations;
using WaldauCastle.Validation;

namespace WaldauCastle.ViewModels;

public class BookingCreateViewModel
{
    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ФИО может содержать от 2 до 200 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите телефон")]
    [RegularExpression(@"^\+7\d{10}$", ErrorMessage = "Введите 10 цифр номера после +7")]
    [Display(Name = "Телефон")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите дату визита")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата визита")]
    public DateTime? VisitDate { get; set; }

    [Required(ErrorMessage = "Укажите количество человек")]
    [Range(1, 30, ErrorMessage = "От 1 до 30 человек")]
    [Display(Name = "Количество человек")]
    public int PersonsCount { get; set; } = 2;

    public int? ExcursionId { get; set; }

    [Display(Name = "Экскурсия")]
    public string? ExcursionTitle { get; set; }

    [Display(Name = "Время визита")]
    public string? VisitTime { get; set; }

    [MustBeTrue(ErrorMessage = "Необходимо дать согласие на обработку персональных данных")]
    [Display(Name = "Я согласен(а) на обработку персональных данных")]
    public bool PersonalDataConsent { get; set; }
}
