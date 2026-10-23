namespace Futbolio.WebUI.DTOs.MatchGoal
{
    public class MatchGoalResultDto
    {
        public int Id { get; set; }

        public int MatchId { get; set; }

        public int TeamId { get; set; }

        public string TeamName { get; set; } 

        public string PlayerName { get; set; } 

        public int Minute { get; set; }
    }
}
