namespace Futbolio.WebUI.ViewModels.Match
{
    public class FixturesViewModel
    {
        // Maçı olan haftalar, küçükten büyüğe
        public List<FixtureWeekViewModel> Weeks { get; set; } = new();

        public int CurrentWeek { get; set; }

        public int TeamCount { get; set; }

        // fixtures.js'in arma ve stadyum bilgisi için kullandığı takım listesi (id -> bilgi)
        public Dictionary<int, FixtureTeamViewModel> Teams { get; set; } = new();
    }

    public class FixtureWeekViewModel
    {
        public int Week { get; set; }

        // yyyy-MM-dd
        public string Start { get; set; } = string.Empty;

        public string End { get; set; } = string.Empty;
    }

    public class FixtureTeamViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
        public string Ink { get; set; } = string.Empty;
        public string Stadium { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
    }
}
