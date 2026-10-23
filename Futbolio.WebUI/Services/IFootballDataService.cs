using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.DTOs.Standing;
using Futbolio.WebUI.DTOs.Team;

namespace Futbolio.WebUI.Services
{
    // API'den okuma işlemleri. API'ye ulaşılamazsa HttpRequestException fırlatır.
    // Controller'lar somut sınıfı değil bu arayüzü ister. Böylece API'ye nasıl gidildiği
    // FootballDataService'in içinde kalır; ileride değişse controller'lar etkilenmez.
    public interface IFootballDataService
    {
        // GET /api/teams
        Task<List<TeamResultDto>> GetTeamsAsync();

        // GET /api/matches
        Task<List<MatchResultDto>> GetMatchesAsync();

        // GET /api/matches/week/{week}  (o hafta maç yoksa boş liste)
        Task<List<MatchResultDto>> GetMatchesByWeekAsync(int week);

        // GET /api/matches/{id}  (maç yoksa null)
        Task<MatchDetailDto?> GetMatchAsync(int id);

        // GET /api/standings
        Task<List<StandingDto>> GetStandingsAsync();
    }
}
