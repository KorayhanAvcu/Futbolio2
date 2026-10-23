using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.DTOs.Team;

namespace Futbolio.WebUI.ViewModels.Match
{
    public class MatchDetailViewModel
    {
        public MatchDetailDto Match { get; set; } = new();

        // Arma, şehir ve kod için (API'de takım silinmişse null olabilir)
        public TeamResultDto? HomeTeam { get; set; }

        public TeamResultDto? AwayTeam { get; set; }

        // Goller, kartlar ve değişiklikler dakikaya göre tek listede
        public List<MatchEventViewModel> Events { get; set; } = new();
    }

    public class MatchEventViewModel
    {
        public int Minute { get; set; }

        // "home" | "away"
        public string Side { get; set; } = "home";

        // "goal" | "yellow" | "red" | "sub"
        public string Kind { get; set; } = "goal";

        // Gol atan, kart gören ya da oyuna giren oyuncu
        public string Player { get; set; } = string.Empty;

        // Değişiklikte oyundan çıkan oyuncu
        public string PlayerOut { get; set; } = string.Empty;

        // Kart ve değişiklikleri sayfada türüne göre gruplamak için
        public string Group => Kind is "yellow" or "red" ? "card" : Kind;
    }
}
