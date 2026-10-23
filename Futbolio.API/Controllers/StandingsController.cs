using Futbolio.API.Context;
using Futbolio.API.DTOs.Standing;
using Futbolio.API.Entities.Common;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StandingsController : ControllerBase
    {
        private readonly ApiContext _context;

        public StandingsController(ApiContext context)
        {
            _context = context;
        }

        // GET /api/standings
        // Ayrı tablo yok: sadece "Tamamlandı" maçlardan anlık hesaplanır.
        // Sıralama: Puan -> Averaj -> Atılan Gol
        [HttpGet]
        public IActionResult StandingList()
        {
            var teams = _context.Teams.ToList();
            var matches = _context.Matches.Where(m => m.Status == MatchStatus.Completed).ToList();

            // Her takım için boş bir puan satırı hazırla
            var table = new Dictionary<int, StandingDto>();
            foreach (var team in teams)
            {
                table[team.Id] = new StandingDto
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    LogoUrl = team.LogoUrl
                };
            }

            // Tamamlanan her maçı iki takımın satırına işle
            foreach (var match in matches)
            {
                var home = table[match.HomeTeamId];
                var away = table[match.AwayTeamId];

                home.Played++;
                away.Played++;

                home.GoalsFor += match.HomeScore;
                home.GoalsAgainst += match.AwayScore;
                away.GoalsFor += match.AwayScore;
                away.GoalsAgainst += match.HomeScore;

                if (match.HomeScore > match.AwayScore)
                {
                    home.Won++;
                    home.Points += 3;
                    away.Lost++;
                }
                else if (match.HomeScore < match.AwayScore)
                {
                    away.Won++;
                    away.Points += 3;
                    home.Lost++;
                }
                else
                {
                    home.Drawn++;
                    away.Drawn++;
                    home.Points += 1;
                    away.Points += 1;
                }
            }

            // Averaj = Atılan Gol - Yenilen Gol
            foreach (var row in table.Values)
            {
                row.GoalDifference = row.GoalsFor - row.GoalsAgainst;
            }

            var values = table.Values
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDifference)
                .ThenByDescending(s => s.GoalsFor)
                .ThenBy(s => s.TeamName)
                .ToList();

            for (var i = 0; i < values.Count; i++)
            {
                values[i].Position = i + 1;
            }

            return Ok(values);
        }
    }
}
