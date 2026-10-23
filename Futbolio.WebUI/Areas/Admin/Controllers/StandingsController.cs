using Futbolio.WebUI.Helpers;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StandingsController : Controller
    {
        private readonly IFootballDataService _data;

        public StandingsController(IFootballDataService data)
        {
            _data = data;
        }

        // Salt okunur: puan durumu API'de tamamlanan maçlardan hesaplanır, elle düzenlenmez
        public async Task<IActionResult> Index()
        {
            var model = new AdminStandingsViewModel();

            try
            {
                var standingsTask = _data.GetStandingsAsync();
                var teamsTask = _data.GetTeamsAsync();
                var matchesTask = _data.GetMatchesAsync();
                await Task.WhenAll(standingsTask, teamsTask, matchesTask);

                model.Rows = standingsTask.Result.OrderBy(s => s.Position).ToList();
                model.Teams = teamsTask.Result.ToDictionary(t => t.Id);
                model.CompletedCount = matchesTask.Result.Count(m => m.Status == TeamStyle.StatusCompleted);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Puan durumu API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
            }

            return View(model);
        }
    }
}
