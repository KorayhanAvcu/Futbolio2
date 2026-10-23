namespace Futbolio.WebUI.DTOs.Team
{
    public class TeamResultDto
    {
        public int Id { get; set; }

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
    }
}