using Futbolio.WebUI.Helpers;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Standing;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.WebUI.Controllers
{
    public class StandingsController : Controller
    {
        private readonly IFootballDataService _data;

        public StandingsController(IFootballDataService data)
        {
            _data = data;
        }

        // Puan durumu API'de (GET /api/standings) tamamlanan maçlardan hesaplanır.
        // Form sütunu için maç listesi de alınır.
        public async Task<IActionResult> Index()
        {
            var model = new StandingsViewModel();

            try
            {
                var standingsTask = _data.GetStandingsAsync();
                var teamsTask = _data.GetTeamsAsync();
                var matchesTask = _data.GetMatchesAsync();
                await Task.WhenAll(standingsTask, teamsTask, matchesTask);

                var teams = teamsTask.Result.ToDictionary(t => t.Id);
                var completed = matchesTask.Result
                    .Where(m => m.Status == TeamStyle.StatusCompleted)
                    .OrderBy(m => m.MatchDate)
                    .ToList();

                model.CompletedMatchCount = completed.Count;
                model.LastCompletedWeek = completed.Count > 0 ? completed.Max(m => m.Week) : 0;

                model.Rows = standingsTask.Result
                    .OrderBy(s => s.Position)
                    .Select(s => new StandingRowViewModel
                    {
                        Standing = s,
                        // GetValueOrDefault: sözlükte bu anahtar yoksa hata vermek yerine null döner.
                        Team = teams.GetValueOrDefault(s.TeamId),
                        Form = BuildForm(s.TeamId, completed)
                    })
                    .ToList();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Puan durumu API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
            }

            return View(model);
        }

        // Takımın son 5 tamamlanan maçı, eskiden yeniye
        private static string BuildForm(int teamId, List<DTOs.Match.MatchResultDto> completed)
        {
            var results = completed
                .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                // Maçlar tarihe göre sıralı olduğu için TakeLast(5) = takımın en son 5 maçı.
                .TakeLast(5)
                .Select(m =>
                {
                    var scored = m.HomeTeamId == teamId ? m.HomeScore : m.AwayScore;
                    var conceded = m.HomeTeamId == teamId ? m.AwayScore : m.HomeScore;
                    return scored > conceded ? 'G' : scored == conceded ? 'B' : 'M';
                });

            return new string(results.ToArray());
        }
    }
}
