using AutoMapper;
using Futbolio.API.Context;
using Futbolio.API.DTOs.Team;
using Futbolio.API.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Futbolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public TeamsController(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET /api/teams
        [HttpGet]
        public IActionResult TeamList()
        {
            var teams = _context.Teams.OrderBy(t => t.Name).ToList();
            return Ok(_mapper.Map<List<TeamResultDto>>(teams));
        }

        // GET /api/teams/5
        [HttpGet("{id}")]
        public IActionResult GetTeam(int id)
        {
            var team = _context.Teams.Find(id);
            if (team == null)
                return NotFound("Takım bulunamadı");

            return Ok(_mapper.Map<TeamResultDto>(team));
        }

        // POST /api/teams
        [HttpPost]
        public IActionResult CreateTeam(CreateTeamDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Takım adı boş olamaz");

            var team = _mapper.Map<Team>(dto);

            _context.Teams.Add(team);
            _context.SaveChanges();
            return Ok("Ekleme başarılı");
        }

        // PUT /api/teams/5
        [HttpPut("{id}")]
        public IActionResult UpdateTeam(int id, UpdateTeamDto dto)
        {
            var team = _context.Teams.Find(id);
            if (team == null)
                return NotFound("Takım bulunamadı");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Takım adı boş olamaz");

            _mapper.Map(dto, team);

            _context.SaveChanges();
            return Ok("Güncelleme başarılı");
        }

        // DELETE /api/teams/5
        [HttpDelete("{id}")]
        public IActionResult DeleteTeam(int id)
        {
            var team = _context.Teams.Find(id);
            if (team == null)
                return NotFound("Takım bulunamadı");

            var hasMatches = _context.Matches.Any(m => m.HomeTeamId == id || m.AwayTeamId == id);
            if (hasMatches)
                return BadRequest("Maçı olan takım silinemez, önce maçlarını silin");

            _context.Teams.Remove(team);
            _context.SaveChanges();
            return Ok("Silme başarılı");
        }
    }
}
