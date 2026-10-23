using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Match;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.WebUI.Controllers
{
    public class MatchDetailController : Controller
    {
        private readonly IFootballDataService _data;

        public MatchDetailController(IFootballDataService data)
        {
            _data = data;
        }

        // /MatchDetail/Index/5  (API: GET /api/matches/5)
        public async Task<IActionResult> Index(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index", "Fixtures");

            try
            {
                var matchTask = _data.GetMatchAsync(id);
                var teamsTask = _data.GetTeamsAsync();
                await Task.WhenAll(matchTask, teamsTask);

                var match = matchTask.Result;
                if (match == null)
                {
                    // Kullanıcıya açıklamalı bir sayfa gösteriyoruz ama HTTP durum kodu yine 404 olsun
                    // (tarayıcı ve arama motorları sayfanın bulunamadığını bilsin).
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    ViewBag.Message = "Aradığınız maç bulunamadı ya da silinmiş olabilir.";
                    return View("NotFound");
                }

                var teams = teamsTask.Result;
                var model = new MatchDetailViewModel
                {
                    Match = match,
                    HomeTeam = teams.FirstOrDefault(t => t.Id == match.HomeTeamId),
                    AwayTeam = teams.FirstOrDefault(t => t.Id == match.AwayTeamId),
                    Events = BuildEvents(match)
                };

                return View(model);
            }
            catch (HttpRequestException)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                ViewBag.Message = "Maç bilgisi API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
                return View("NotFound");
            }
        }

        // Gol, kart ve değişiklik kayıtlarını dakikaya göre tek bir akışa çevirir
        private static List<MatchEventViewModel> BuildEvents(MatchDetailDto match)
        {
            // Metodun içinde tanımlı küçük yardımcı fonksiyon (local function); sadece burada kullanılır.
            string Side(int teamId) => teamId == match.HomeTeamId ? "home" : "away";

            var events = new List<MatchEventViewModel>();

            events.AddRange((match.Goals ?? new List<DTOs.MatchGoal.MatchGoalResultDto>()).Select(g => new MatchEventViewModel
            {
                Minute = g.Minute,
                Side = Side(g.TeamId),
                Kind = "goal",
                Player = g.PlayerName
            }));

            events.AddRange((match.Cards ?? new List<DTOs.MatchCard.MatchCardResultDto>()).Select(c => new MatchEventViewModel
            {
                Minute = c.Minute,
                Side = Side(c.TeamId),
                Kind = string.Equals(c.CardType, "Red", StringComparison.OrdinalIgnoreCase) ? "red" : "yellow",
                Player = c.PlayerName
            }));

            events.AddRange((match.Substitutions ?? new List<DTOs.Substitution.SubstitutionResultDto>()).Select(s => new MatchEventViewModel
            {
                Minute = s.Minute,
                Side = Side(s.TeamId),
                Kind = "sub",
                Player = s.PlayerIn,
                PlayerOut = s.PlayerOut
            }));

            // Aynı dakikada önce gol, sonra kart, sonra değişiklik
            int Order(string kind) => kind == "goal" ? 0 : kind == "sub" ? 2 : 1;
            return events.OrderBy(e => e.Minute).ThenBy(e => Order(e.Kind)).ToList();
        }
    }
}
