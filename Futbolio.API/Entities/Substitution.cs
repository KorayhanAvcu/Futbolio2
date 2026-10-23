using Futbolio.API.Entities.Common;

namespace Futbolio.API.Entities
{
    public class Substitution : BaseEntity
    {
        public int MatchId { get; set; }

        public int TeamId { get; set; }

        public string PlayerIn { get; set; } = string.Empty;

        public string PlayerOut { get; set; } = string.Empty;

        public int Minute { get; set; }


        // Navigation Properties

        public Match Match { get; set; }

        public Team Team { get; set; }
    }
}