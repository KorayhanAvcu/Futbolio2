using AutoMapper;
using Futbolio.API.DTOs.Match;
using Futbolio.API.DTOs.MatchCard;
using Futbolio.API.DTOs.MatchGoal;
using Futbolio.API.DTOs.Substitution;
using Futbolio.API.DTOs.Team;
using Futbolio.API.Entities;

namespace Futbolio.API.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ---------- Team ----------
            CreateMap<Team, TeamResultDto>();

            CreateMap<CreateTeamDto, Team>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()))
                .ForMember(d => d.LogoUrl, o => o.NullSubstitute(""))
                .ForMember(d => d.City, o => o.NullSubstitute(""))
                .ForMember(d => d.Stadium, o => o.NullSubstitute(""));

            // Update'te Id hiçbir zaman DTO'dan kopyalanmaz (URL'deki id geçerli)
            CreateMap<UpdateTeamDto, Team>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()))
                .ForMember(d => d.LogoUrl, o => o.NullSubstitute(""))
                .ForMember(d => d.City, o => o.NullSubstitute(""))
                .ForMember(d => d.Stadium, o => o.NullSubstitute(""));


            // ---------- Match ----------
            // HomeTeam.Name -> HomeTeamName, AwayTeam.LogoUrl -> AwayTeamLogoUrl gibi alanları
            // AutoMapper isimlendirmeden (flattening) kendisi eşler.
            CreateMap<Match, MatchResultDto>();

            // Goals, Cards, Substitutions listeleri aşağıdaki map'lerle otomatik dönüşür
            CreateMap<Match, MatchDetailDto>();

            CreateMap<CreateMatchDto, Match>()
                .ForMember(d => d.Stadium, o => o.NullSubstitute(""));

            CreateMap<UpdateMatchDto, Match>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Stadium, o => o.NullSubstitute(""));


            // ---------- Match_Goal ----------
            CreateMap<Match_Goal, MatchGoalResultDto>();

            CreateMap<CreateMatchGoalDto, Match_Goal>()
                .ForMember(d => d.PlayerName, o => o.MapFrom(s => s.PlayerName.Trim()));

            CreateMap<UpdateMatchGoalDto, Match_Goal>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PlayerName, o => o.MapFrom(s => s.PlayerName.Trim()));


            // ---------- Match_Card ----------
            CreateMap<Match_Card, MatchCardResultDto>();

            CreateMap<CreateMatchCardDto, Match_Card>()
                .ForMember(d => d.PlayerName, o => o.MapFrom(s => s.PlayerName.Trim()));

            CreateMap<UpdateMatchCardDto, Match_Card>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PlayerName, o => o.MapFrom(s => s.PlayerName.Trim()));


            // ---------- Substitution ----------
            CreateMap<Substitution, SubstitutionResultDto>();

            CreateMap<CreateSubstitutionDto, Substitution>()
                .ForMember(d => d.PlayerIn, o => o.MapFrom(s => s.PlayerIn.Trim()))
                .ForMember(d => d.PlayerOut, o => o.MapFrom(s => s.PlayerOut.Trim()));

            CreateMap<UpdateSubstitutionDto, Substitution>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PlayerIn, o => o.MapFrom(s => s.PlayerIn.Trim()))
                .ForMember(d => d.PlayerOut, o => o.MapFrom(s => s.PlayerOut.Trim()));
        }
    }
}
