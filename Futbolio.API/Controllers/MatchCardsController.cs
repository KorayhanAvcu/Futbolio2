using AutoMapper;
using Futbolio.API.Context;
using Futbolio.API.DTOs.MatchCard;
using Futbolio.API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;

namespace Futbolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchCardsController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public MatchCardsController(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Kabul edilen kart tipleri
        private static readonly string[] AllowedCardTypes = { "Yellow", "Red" };

        // GET /api/matchcards/match/10
        [HttpGet("match/{matchId}")]
        public IActionResult CardListByMatch(int matchId)
        {
            if (!_context.Matches.Any(m => m.Id == matchId))
                return NotFound("Maç bulunamadı");

            var cards = _context.MatchCards
                .Include(c => c.Team)
                .Where(c => c.MatchId == matchId)
                .OrderBy(c => c.Minute)
                .ToList();

            return Ok(_mapper.Map<List<MatchCardResultDto>>(cards));
        }

        // GET /api/matchcards/5
        [HttpGet("{id}")]
        public IActionResult GetCard(int id)
        {
            var card = _context.MatchCards
                .Include(c => c.Team)
                .FirstOrDefault(c => c.Id == id);

            if (card == null)
                return NotFound("Kart kaydı bulunamadı");

            return Ok(_mapper.Map<MatchCardResultDto>(card));
        }

        // POST /api/matchcards
        [HttpPost]
        public IActionResult CreateCard(CreateMatchCardDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PlayerName))
                return BadRequest("Oyuncu adı boş olamaz");

            var cardType = NormalizeCardType(dto.CardType);
            if (cardType == null)
                return BadRequest("Kart tipi 'Yellow' veya 'Red' olmalı");

            var check = CheckMatchAndTeam(dto.MatchId, dto.TeamId, dto.Minute);
            if (check != null)
                return check;

            var card = _mapper.Map<Match_Card>(dto);
            card.CardType = cardType; // "yellow" -> "Yellow"

            _context.MatchCards.Add(card);
            _context.SaveChanges();
            return Ok("Ekleme başarılı");
        }

        // PUT /api/matchcards/5
        [HttpPut("{id}")]
        public IActionResult UpdateCard(int id, UpdateMatchCardDto dto)
        {
            var card = _context.MatchCards.Find(id);
            if (card == null)
                return NotFound("Kart kaydı bulunamadı");

            if (string.IsNullOrWhiteSpace(dto.PlayerName))
                return BadRequest("Oyuncu adı boş olamaz");

            var cardType = NormalizeCardType(dto.CardType);
            if (cardType == null)
                return BadRequest("Kart tipi 'Yellow' veya 'Red' olmalı");

            var check = CheckMatchAndTeam(dto.MatchId, dto.TeamId, dto.Minute);
            if (check != null)
                return check;

            _mapper.Map(dto, card);
            card.CardType = cardType;

            _context.SaveChanges();
            return Ok("Güncelleme başarılı");
        }

        // DELETE /api/matchcards/5
        [HttpDelete("{id}")]
        public IActionResult DeleteCard(int id)
        {
            var card = _context.MatchCards.Find(id);
            if (card == null)
                return NotFound("Kart kaydı bulunamadı");

            _context.MatchCards.Remove(card);
            _context.SaveChanges();
            return Ok("Silme başarılı");
        }

        // "yellow" gelirse "Yellow" olarak düzeltir, geçersizse null döner
        private static string? NormalizeCardType(string? cardType)
        {
            return AllowedCardTypes.FirstOrDefault(t =>
                string.Equals(t, cardType?.Trim(), StringComparison.OrdinalIgnoreCase));
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
