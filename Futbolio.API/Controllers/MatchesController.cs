using AutoMapper;
using Futbolio.API.Context;
using Futbolio.API.DTOs.Match;
using Futbolio.API.Entities;
using Futbolio.API.Entities.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Futbolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public MatchesController(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET /api/matches
        [HttpGet]
        public IActionResult MatchList()
        {
            var matches = _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .OrderBy(m => m.Week)
                .ThenBy(m => m.MatchDate)
                .ToList();

            return Ok(_mapper.Map<List<MatchResultDto>>(matches));
        }

        // GET /api/matches/week/3
        [HttpGet("week/{week}")]
        public IActionResult MatchListByWeek(int week)
        {
            if (week < 1)
                return BadRequest("Hafta 1 veya daha büyük olmalı");

            var matches = _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Week == week)
                .OrderBy(m => m.MatchDate)
                .ToList();

            if (matches.Count == 0)
                return NotFound("Bu hafta için maç bulunamadı");

            return Ok(_mapper.Map<List<MatchResultDto>>(matches));
        }

        // GET /api/matches/10  (skor, goller, kartlar, oyuncu değişiklikleri)
        [HttpGet("{id}")]
        public IActionResult GetMatch(int id)
        {
            // Include içinde OrderBy: listeler dakikaya göre sıralı gelir
            var match = _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Goals.OrderBy(g => g.Minute)).ThenInclude(g => g.Team)
                .Include(m => m.Cards.OrderBy(c => c.Minute)).ThenInclude(c => c.Team)
                .Include(m => m.Substitutions.OrderBy(s => s.Minute)).ThenInclude(s => s.Team)
                .FirstOrDefault(m => m.Id == id);

            if (match == null)
                return NotFound("Maç bulunamadı");

            return Ok(_mapper.Map<MatchDetailDto>(match));
        }

        // POST /api/matches
        [HttpPost]
        public IActionResult CreateMatch(CreateMatchDto dto)
        {
            // 0 = yeni maç, hiçbir maçı hariç tutma
            var error = CheckMatch(dto.HomeTeamId, dto.AwayTeamId, dto.Week, dto.Status, dto.HomeScore, dto.AwayScore, 0);
            if (error != null)
                return BadRequest(error);

            var match = _mapper.Map<Match>(dto);

            // Stadyum boş geldiyse ev sahibi takımın stadyumu kullanılır
            if (string.IsNullOrWhiteSpace(match.Stadium))
                match.Stadium = _context.Teams.Find(dto.HomeTeamId)!.Stadium;

            _context.Matches.Add(match);
            _context.SaveChanges();
            return Ok("Ekleme başarılı");
        }

        // PUT /api/matches/10
        [HttpPut("{id}")]
        public IActionResult UpdateMatch(int id, UpdateMatchDto dto)
        {
            var match = _context.Matches.Find(id);
            if (match == null)
                return NotFound("Maç bulunamadı");

            var error = CheckMatch(dto.HomeTeamId, dto.AwayTeamId, dto.Week, dto.Status, dto.HomeScore, dto.AwayScore, id);
            if (error != null)
                return BadRequest(error);

            _mapper.Map(dto, match);

            if (string.IsNullOrWhiteSpace(match.Stadium))
                match.Stadium = _context.Teams.Find(dto.HomeTeamId)!.Stadium;

            _context.SaveChanges();
            return Ok("Güncelleme başarılı");
        }

        // DELETE /api/matches/10  (gol, kart, değişiklik kayıtları da silinir)
        [HttpDelete("{id}")]
        public IActionResult DeleteMatch(int id)
        {
            var match = _context.Matches.Find(id);
            if (match == null)
                return NotFound("Maç bulunamadı");

            _context.Matches.Remove(match);
            _context.SaveChanges();
            return Ok("Silme başarılı");
        }

        // İş kuralları. Hata varsa mesajı döner, sorun yoksa null döner.
        private string? CheckMatch(int homeId, int awayId, int week, int status, int homeScore, int awayScore, int ignoreMatchId)
        {
            if (homeId == awayId)
                return "Ev sahibi ve deplasman takımı aynı olamaz";

            if (week < 1)
                return "Hafta 1 veya daha büyük olmalı";

            if (!MatchStatus.IsValid(status))
                return "Geçersiz durum (0: Planlandı, 1: Canlı, 2: Tamamlandı)";

            if (homeScore < 0 || awayScore < 0)
                return "Skor negatif olamaz";

            var teamCount = _context.Teams.Count(t => t.Id == homeId || t.Id == awayId);
            if (teamCount != 2)
                return "Ev sahibi veya deplasman takımı bulunamadı";

            // Aynı hafta içinde bir takım sadece bir maçta yer alabilir
            var alreadyPlaying = _context.Matches.Any(m =>
                m.Id != ignoreMatchId &&
                m.Week == week &&
                (m.HomeTeamId == homeId || m.AwayTeamId == homeId ||
                 m.HomeTeamId == awayId || m.AwayTeamId == awayId));

            if (alreadyPlaying)
                return "Takımlardan biri bu hafta zaten başka bir maçta yer alıyor";

            return null;
        }
    }
}
