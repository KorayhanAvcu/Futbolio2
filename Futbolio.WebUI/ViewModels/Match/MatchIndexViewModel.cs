using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.DTOs.Team;

namespace Futbolio.WebUI.ViewModels.Match
{
    public class MatchIndexViewModel
    {
        public List<MatchResultDto> Matches { get; set; } = new();

        // Modaldaki ev sahibi / deplasman seçimleri için
        public List<TeamResultDto> Teams { get; set; } = new();
    }
}
