using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.DTOs.Standing;
using Futbolio.WebUI.DTOs.Team;

namespace Futbolio.WebUI.ViewModels.Admin
{
    // Yönetim ana sayfası
    public class DashboardViewModel
    {
        public int TeamCount { get; set; }
        public int MatchCount { get; set; }
        public int CompletedCount { get; set; }
        public int LiveCount { get; set; }
        public int ScheduledCount { get; set; }
        public int WeekCount { get; set; }

        // Oynanan (canlı + tamamlanan) maçlardaki toplam gol
        public int GoalCount { get; set; }

        public List<StandingDto> TopStandings { get; set; } = new();
        public List<MatchResultDto> RecentResults { get; set; } = new();
        public List<MatchResultDto> UpcomingMatches { get; set; } = new();

        // Arma için takım id -> takım
        public Dictionary<int, TeamResultDto> Teams { get; set; } = new();
    }

    // Haftalar sayfası
    public class WeekSummaryViewModel
    {
        public int Week { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Live { get; set; }
        public int Scheduled { get; set; }
        public int Goals { get; set; }

        // O hafta hiç maçı olmayan aktif takım sayısı
        public int IdleTeams { get; set; }

        // Aynı hafta birden fazla maçı olan takımlar (iş kuralı ihlali)
        public List<string> DuplicateTeams { get; set; } = new();
    }

    public class WeeksViewModel
    {
        public List<WeekSummaryViewModel> Weeks { get; set; } = new();
        public int TeamCount { get; set; }

        // Bir haftada olması gereken maç sayısı (20 takım -> 10 maç)
        public int ExpectedMatchesPerWeek => TeamCount / 2;
    }

    // Yönetim puan durumu
    public class AdminStandingsViewModel
    {
        public List<StandingDto> Rows { get; set; } = new();
        public Dictionary<int, TeamResultDto> Teams { get; set; } = new();
        public int CompletedCount { get; set; }
    }
}
