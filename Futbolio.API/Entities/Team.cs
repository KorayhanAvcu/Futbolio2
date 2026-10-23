using Futbolio.API.Entities.Common;

namespace Futbolio.API.Entities
{
    public class Team : BaseEntity
    {
        public string Name { get; set; }

        public string? LogoUrl { get; set; }

        public string City { get; set; }

        public string Stadium { get; set; } 

        public string Code { get; set; }

        public string Region { get; set; } 

        public int Capacity { get; set; }

        public int Founded { get; set; }

        public string ColorA { get; set; } = "#1E5BD8";

        public string ColorB { get; set; } = "#FFFFFF";

        public bool IsActive { get; set; } 

        // Navigation Properties

        public IList<Match> HomeMatches { get; set; }

        public IList<Match> AwayMatches { get; set; }

        public IList<Match_Goal> Goals { get; set; }

        public IList<Match_Card> Cards { get; set; }

        public IList<Substitution> Substitutions { get; set; }
    }
}