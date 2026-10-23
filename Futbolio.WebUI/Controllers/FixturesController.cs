using Futbolio.WebUI.Helpers;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Match;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.WebUI.Controllers
{
    public class FixturesController : Controller
    {
        private readonly IFootballDataService _data;

        public FixturesController(IFootballDataService data)
        {
            _data = data;
        }

        // Sayfa iskeleti: hafta listesi + takımlar. Maçlar fixtures.js ile Week action'ından gelir.
        public async Task<IActionResult> Index(int? hafta)
        {
            var model = new FixturesViewModel();

            try
            {
                var matchesTask = _data.GetMatchesAsync();
                var teamsTask = _data.GetTeamsAsync();
                await Task.WhenAll(matchesTask, teamsTask);

                var matches = matchesTask.Result;

                model.Weeks = matches
                    .GroupBy(m => m.Week)
                    .OrderBy(g => g.Key)
                    .Select(g => new FixtureWeekViewModel
                    {
                        Week = g.Key,
                        Start = g.Min(m => m.MatchDate).ToString("yyyy-MM-dd"),
                        End = g.Max(m => m.MatchDate).ToString("yyyy-MM-dd")
                    })
                    .ToList();

                // Güncel hafta: tamamlanmamış maçı olan ilk hafta, yoksa son hafta
                var open = matches
                    .Where(m => m.Status != TeamStyle.StatusCompleted)
                    .Select(m => m.Week)
                    // DefaultIfEmpty: liste boşsa Min() hata vermesin diye varsayılan değer koyar.
                    // Weeks[^1]: listenin son elemanı (^1 = sondan birinci).
                    .DefaultIfEmpty(model.Weeks.Count > 0 ? model.Weeks[^1].Week : 0)
                    .Min();

                model.CurrentWeek = hafta.HasValue && model.Weeks.Any(w => w.Week == hafta.Value)
                    ? hafta.Value
                    : open;

                model.TeamCount = teamsTask.Result.Count;
                model.Teams = teamsTask.Result.ToDictionary(t => t.Id, t => new FixtureTeamViewModel
                {
                    Name = t.Name,
                    Code = TeamStyle.Code(t),
                    A = TeamStyle.ColorA(t),
                    B = TeamStyle.ColorB(t),
                    Ink = TeamStyle.Ink(TeamStyle.ColorA(t)),
                    Stadium = t.Stadium ?? "",
                    Logo = TeamStyle.HasLogo(t.LogoUrl) ? t.LogoUrl! : ""
                });
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Fikstür API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
            }

            return View(model);
        }

        // fixtures.js buradan bir haftanın maçlarını ister (API: GET /api/matches/week/{week})
        // Bu action sayfa değil JSON döner. fixtures.js hafta değişince sayfayı yenilemeden
        // fetch('/Fixtures/Week/2') ile çağırır (AJAX).
        [HttpGet]
        public async Task<IActionResult> Week(int id)
        {
            if (id < 1)
                return BadRequest(new { message = "Hafta 1 veya daha büyük olmalı." });

            try
            {
                return Json(await _data.GetMatchesByWeekAsync(id));
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { message = "API bağlantısı kurulamadı." });
            }
        }
    }
}
