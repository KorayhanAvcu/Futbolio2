using Futbolio.WebUI.Helpers;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class WeeksController : Controller
    {
        private readonly IFootballDataService _data;

        public WeeksController(IFootballDataService data)
        {
            _data = data;
        }

        // Haftaların özeti: maç sayısı, durumlar ve fikstür kurallarına uygunluk
        public async Task<IActionResult> Index()
        {
            var model = new WeeksViewModel();

            try
            {
                var teamsTask = _data.GetTeamsAsync();
                var matchesTask = _data.GetMatchesAsync();
                await Task.WhenAll(teamsTask, matchesTask);

                var activeTeams = teamsTask.Result.Where(t => t.IsActive).ToList();
                model.TeamCount = activeTeams.Count > 0 ? activeTeams.Count : teamsTask.Result.Count;

                model.Weeks = matchesTask.Result
                    .GroupBy(m => m.Week)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        // Her maçta iki takım; bir takım haftada en fazla bir kez görünmeli
                        // SelectMany: her maçtan iki eleman (ev sahibi + deplasman) üretip tek düz listede toplar.
                        // Aşağıda bu liste takıma göre gruplanır; 2+ kez geçen takım aynı hafta iki maça konmuştur.
                        var appearances = g.SelectMany(m => new[]
                        {
                            (Id: m.HomeTeamId, Name: m.HomeTeamName),
                            (Id: m.AwayTeamId, Name: m.AwayTeamName)
                        }).ToList();

                        var playing = appearances.Select(a => a.Id).Distinct().Count();

                        return new WeekSummaryViewModel
                        {
                            Week = g.Key,
                            Start = g.Min(m => m.MatchDate),
                            End = g.Max(m => m.MatchDate),
                            Total = g.Count(),
                            Completed = g.Count(m => m.Status == TeamStyle.StatusCompleted),
                            Live = g.Count(m => m.Status == TeamStyle.StatusLive),
                            Scheduled = g.Count(m => m.Status == TeamStyle.StatusScheduled),
                            Goals = g.Where(m => TeamStyle.HasScore(m.Status)).Sum(m => m.HomeScore + m.AwayScore),
                            IdleTeams = Math.Max(0, model.TeamCount - playing),
                            DuplicateTeams = appearances
                                .GroupBy(a => a.Id)
                                .Where(x => x.Count() > 1)
                                .Select(x => x.First().Name)
                                .ToList()
                        };
                    })
                    .ToList();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Haftalar API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
            }

            return View(model);
        }
    }
}
