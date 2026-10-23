namespace Futbolio.API.DTOs.Substitution
{
    public class UpdateSubstitutionDto
    {
        public int Id { get; set; }

        public int MatchId { get; set; }

        public int TeamId { get; set; }

        public string TeamName { get; set; }

        public string PlayerIn { get; set; }

        public string PlayerOut { get; set; }

        public int Minute { get; set; }
    }
}