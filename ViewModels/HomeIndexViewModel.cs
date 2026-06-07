using WaldauCastle.Models;

namespace WaldauCastle.ViewModels;

public class HomeIndexViewModel
{
    public IReadOnlyList<Event> UpcomingEvents { get; init; } = [];
}
