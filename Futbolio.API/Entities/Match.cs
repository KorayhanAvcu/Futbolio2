using Futbolio.API.Entities.Common;

namespace Futbolio.API.Entities
{
    public class Match : BaseEntity
    {
        public int HomeTeamId { get; set; }

        public int AwayTeamId { get; set; }

        public int Week { get; set; }

        public DateTime MatchDate { get; set; }

        public int HomeScore { get; set; }

        public int AwayScore { get; set; }

        public int Status { get; set; }

        public string Stadium { get; set; } = string.Empty;


        // Navigation Properties

        public Team HomeTeam { get; set; }

        public Team AwayTeam { get; set; }

        public IList<Match_Goal> Goals { get; set; }

        public IList<Match_Card> Cards { get; set; }

        public IList<Substitution> Substitutions { get; set; }
    }
}