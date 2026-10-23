using AutoMapper;
using Futbolio.API.Context;
using Futbolio.API.DTOs.Substitution;
using Futbolio.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Futbolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubstitutionsController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public SubstitutionsController(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET /api/substitutions/match/10
        [HttpGet("match/{matchId}")]
        public IActionResult SubstitutionListByMatch(int matchId)
        {
            if (!_context.Matches.Any(m => m.Id == matchId))
                return NotFound("Maç bulunamadı");

            var items = _context.Substitutions
                .Include(s => s.Team)
                .Where(s => s.MatchId == matchId)
                .OrderBy(s => s.Minute)
                .ToList();

            return Ok(_mapper.Map<List<SubstitutionResultDto>>(items));
        }

        // GET /api/substitutions/5
        [HttpGet("{id}")]
        public IActionResult GetSubstitution(int id)
        {
            var item = _context.Substitutions
                .Include(s => s.Team)
                .FirstOrDefault(s => s.Id == id);

            if (item == null)
                return NotFound("Oyuncu değişikliği bulunamadı");

            return Ok(_mapper.Map<SubstitutionResultDto>(item));
        }

        // POST /api/substitutions
        [HttpPost]
        public IActionResult CreateSubstitution(CreateSubstitutionDto dto)
        {
            var playerError = CheckPlayers(dto.PlayerIn, dto.PlayerOut);
            if (playerError != null)
                return BadRequest(playerError);

            var check = CheckMatchAndTeam(dto.MatchId, dto.TeamId, dto.Minute);
            if (check != null)
                return check;

            var item = _mapper.Map<Substitution>(dto);

            _context.Substitutions.Add(item);
            _context.SaveChanges();
            return Ok("Ekleme başarılı");
        }

        // PUT /api/substitutions/5
        [HttpPut("{id}")]
        public IActionResult UpdateSubstitution(int id, UpdateSubstitutionDto dto)
        {
            var item = _context.Substitutions.Find(id);
            if (item == null)
                return NotFound("Oyuncu değişikliği bulunamadı");

            var playerError = CheckPlayers(dto.PlayerIn, dto.PlayerOut);
            if (playerError != null)
                return BadRequest(playerError);

            var check = CheckMatchAndTeam(dto.MatchId, dto.TeamId, dto.Minute);
            if (check != null)
                return check;

            _mapper.Map(dto, item);

            _context.SaveChanges();
            return Ok("Güncelleme başarılı");
        }

        // DELETE /api/substitutions/5
        [HttpDelete("{id}")]
        public IActionResult DeleteSubstitution(int id)
        {
            var item = _context.Substitutions.Find(id);
            if (item == null)
                return NotFound("Oyuncu değişikliği bulunamadı");

            _context.Substitutions.Remove(item);
            _context.SaveChanges();
            return Ok("Silme başarılı");
        }

        // Giren ve çıkan oyuncu dolu ve farklı olmalı
        private static string? CheckPlayers(string? playerIn, string? playerOut)
        {
            if (string.IsNullOrWhiteSpace(playerIn) || string.IsNullOrWhiteSpace(playerOut))
                return "Giren ve çıkan oyuncu adı boş olamaz";

            if (string.Equals(playerIn.Trim(), playerOut.Trim(), StringComparison.OrdinalIgnoreCase))
                return "Giren ve çıkan oyuncu aynı olamaz";

            return null;
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
