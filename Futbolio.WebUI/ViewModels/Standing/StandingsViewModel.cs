using Futbolio.WebUI.DTOs.Standing;
using Futbolio.WebUI.DTOs.Team;

namespace Futbolio.WebUI.ViewModels.Standing
{
    public class StandingsViewModel
    {
        public List<StandingRowViewModel> Rows { get; set; } = new();

        // Tamamlanan maçı olan son hafta (0 = henüz tamamlanan maç yok)
        public int LastCompletedWeek { get; set; }

        public int CompletedMatchCount { get; set; }
    }

    public class StandingRowViewModel
    {
        public StandingDto Standing { get; set; } = new();

        // Arma rengi, kod ve logo için (takım bulunamazsa null)
        public TeamResultDto? Team { get; set; }

        // Son 5 tamamlanan maç, eskiden yeniye. G: Galibiyet, B: Beraberlik, M: Mağlubiyet
        public string Form { get; set; } = string.Empty;

        public int FormPoints => Form.Sum(c => c == 'G' ? 3 : c == 'B' ? 1 : 0);
    }
}
