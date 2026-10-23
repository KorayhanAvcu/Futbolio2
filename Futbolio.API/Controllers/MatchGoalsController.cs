using AutoMapper;
using Futbolio.API.Context;
using Futbolio.API.DTOs.MatchGoal;
using Futbolio.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Futbolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchGoalsController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public MatchGoalsController(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET /api/matchgoals/match/10
        [HttpGet("match/{matchId}")]
        public IActionResult GoalListByMatch(int matchId)
        {
            if (!_context.Matches.Any(m => m.Id == matchId))
                return NotFound("Maç bulunamadı");

            var goals = _context.MatchGoals
                .Include(g => g.Team)
                .Where(g => g.MatchId == matchId)
                .OrderBy(g => g.Minute)
                .ToList();

            return Ok(_mapper.Map<List<MatchGoalResultDto>>(goals));
        }

        // GET /api/matchgoals/5
        [HttpGet("{id}")]
        public IActionResult GetGoal(int id)
        {
            var goal = _context.MatchGoals
                .Include(g => g.Team)
                .FirstOrDefault(g => g.Id == id);

            if (goal == null)
                return NotFound("Gol kaydı bulunamadı");

            return Ok(_mapper.Map<MatchGoalResultDto>(goal));
        }

        // POST /api/matchgoals
        [HttpPost]
        public IActionResult CreateGoal(CreateMatchGoalDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PlayerName))
                return BadRequest("Oyuncu adı boş olamaz");

            var check = CheckMatchAndTeam(dto.MatchId, dto.TeamId, dto.Minute);
            if (check != null)
                return check;

            var goal = _mapper.Map<Match_Goal>(dto);

            _context.MatchGoals.Add(goal);
            _context.SaveChanges();
            return Ok("Ekleme başarılı");
        }

        // PUT /api/matchgoals/5
        [HttpPut("{id}")]
        public IActionResult UpdateGoal(int id, UpdateMatchGoalDto dto)
        {
            var goal = _context.MatchGoals.Find(id);
            if (goal == null)
                return NotFound("Gol kaydı bulunamadı");

            if (string.IsNullOrWhiteSpace(dto.PlayerName))
                return BadRequest("Oyuncu adı boş olamaz");

            var check = CheckMatchAndTeam(dto.MatchId, dto.TeamId, dto.Minute);
            if (check != null)
                return check;

            _mapper.Map(dto, goal);

            _context.SaveChanges();
            return Ok("Güncelleme başarılı");
        }

        // DELETE /api/matchgoals/5
        [HttpDelete("{id}")]
        public IActionResult DeleteGoal(int id)
        {
            var goal = _context.MatchGoals.Find(id);
            if (goal == null)
                return NotFound("Gol kaydı bulunamadı");

            _context.MatchGoals.Remove(goal);
            _context.SaveChanges();
            return Ok("Silme başarılı");
        }

        // Maç var mı, takım bu maçın takımlarından biri mi, dakika makul mü?
        // Sorun varsa hata cevabını döner, sorun yoksa null döner.
        private IActionResult? CheckMatchAndTeam(int matchId, int teamId, int minute)
        {
            var match = _context.Matches.Find(matchId);
            if (match == null)
                return NotFound("Maç bulunamadı");

            if (teamId != match.HomeTeamId && teamId != match.AwayTeamId)
                return BadRequest("Takım bu maçın takımlarından biri olmalı");

            if (minute < 0 || minute > 130)
                return BadRequest("Dakika 0 ile 130 arasında olmalı");

            return null;
        }
    }
}
