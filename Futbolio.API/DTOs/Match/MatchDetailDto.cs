using Futbolio.API.DTOs.MatchCard;
using Futbolio.API.DTOs.MatchGoal;
using Futbolio.API.DTOs.Substitution;

namespace Futbolio.API.DTOs.Match
{
    public class MatchDetailDto
    {
        public int Id { get; set; }

        public int HomeTeamId { get; set; }

        public string HomeTeamName { get; set; } 

        public string HomeTeamLogoUrl { get; set; } 

        public int AwayTeamId { get; set; }

        public string AwayTeamName { get; set; } 

        public string AwayTeamLogoUrl { get; set; } 

        public int Week { get; set; }

        public DateTime MatchDate { get; set; }

        public int HomeScore { get; set; }

        public int AwayScore { get; set; }

        public int Status { get; set; }

        public string Stadium { get; set; }

        public IList<MatchGoalResultDto> Goals { get; set; } 

        public IList<MatchCardResultDto> Cards { get; set; }

        public IList<SubstitutionResultDto> Substitutions { get; set; } 
    }
}