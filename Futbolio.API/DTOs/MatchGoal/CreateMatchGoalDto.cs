namespace Futbolio.API.DTOs.MatchGoal
{
    public class CreateMatchGoalDto
    {
        public int MatchId { get; set; }

        public int TeamId { get; set; }

        public string? TeamName { get; set; }

        public string PlayerName { get; set; }

        public int Minute { get; set; }
    }
}