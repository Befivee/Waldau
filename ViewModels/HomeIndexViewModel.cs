using WaldauCastle.Models;

namespace WaldauCastle.ViewModels;

public class HomeIndexViewModel
{
    public IReadOnlyList<Excursion> UpcomingExcursions { get; init; } = [];
    public IReadOnlyList<Event> UpcomingEvents { get; init; } = [];
}
