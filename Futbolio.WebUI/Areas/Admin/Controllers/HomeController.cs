using Futbolio.WebUI.Helpers;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly IFootballDataService _data;

        public HomeController(IFootballDataService data)
        {
            _data = data;
        }

        // Yönetim özeti: sayılar, ilk 5, son sonuçlar, sıradaki maçlar
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            try
            {
                var teamsTask = _data.GetTeamsAsync();
                var matchesTask = _data.GetMatchesAsync();
                var standingsTask = _data.GetStandingsAsync();
                await Task.WhenAll(teamsTask, matchesTask, standingsTask);

                var matches = matchesTask.Result;
                var played = matches.Where(m => TeamStyle.HasScore(m.Status)).ToList();

                model.Teams = teamsTask.Result.ToDictionary(t => t.Id);
                model.TeamCount = teamsTask.Result.Count;
                model.MatchCount = matches.Count;
                model.CompletedCount = matches.Count(m => m.Status == TeamStyle.StatusCompleted);
                model.LiveCount = matches.Count(m => m.Status == TeamStyle.StatusLive);
                model.ScheduledCount = matches.Count(m => m.Status == TeamStyle.StatusScheduled);
                model.WeekCount = matches.Select(m => m.Week).Distinct().Count();
                model.GoalCount = played.Sum(m => m.HomeScore + m.AwayScore);

                model.TopStandings = standingsTask.Result.OrderBy(s => s.Position).Take(5).ToList();
                model.RecentResults = matches
                    .Where(m => m.Status == TeamStyle.StatusCompleted)
                    .OrderByDescending(m => m.MatchDate).Take(5).ToList();
                model.UpcomingMatches = matches
                    .Where(m => m.Status != TeamStyle.StatusCompleted)
                    .OrderBy(m => m.MatchDate).Take(5).ToList();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Veriler API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
            }

            return View(model);
        }
    }
}
