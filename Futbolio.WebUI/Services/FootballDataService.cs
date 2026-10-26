using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.DTOs.Standing;
using Futbolio.WebUI.DTOs.Team;
using Newtonsoft.Json;
using System.Net;

namespace Futbolio.WebUI.Services
{
    public class FootballDataService : IFootballDataService
    {
        private readonly HttpClient _client;

       
        public FootballDataService(HttpClient client)
        {
            _client = client;
        }

        public Task<List<TeamResultDto>> GetTeamsAsync() =>
            GetListAsync<TeamResultDto>("Teams");

        public Task<List<MatchResultDto>> GetMatchesAsync() =>
            GetListAsync<MatchResultDto>("Matches");

        public async Task<List<MatchResultDto>> GetMatchesByWeekAsync(int week)
        {
            var response = await GetAsync("Matches/week/" + week);

            // API o hafta maç yoksa 404 döner, bu bir hata değil
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new List<MatchResultDto>();

            return await ReadAsync<List<MatchResultDto>>(response) ?? new List<MatchResultDto>();
        }

        public async Task<MatchDetailDto?> GetMatchAsync(int id)
        {
            var response = await GetAsync("Matches/" + id);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            return await ReadAsync<MatchDetailDto>(response);
        }

        public Task<List<StandingDto>> GetStandingsAsync() =>
            GetListAsync<StandingDto>("Standings");

        // ---------- Yardımcılar ----------

        // API zamanında cevap vermezse HttpClient TaskCanceledException fırlatır.
        // Controller'lar tek bir hata türü yakalasın diye HttpRequestException'a çeviriyoruz.
        private async Task<HttpResponseMessage> GetAsync(string path)
        {
            try
            {
                return await _client.GetAsync(path);
            }
            catch (TaskCanceledException ex)
            {
                throw new HttpRequestException("API zamanında cevap vermedi.", ex);
            }
        }

        private async Task<List<T>> GetListAsync<T>(string path)
        {
            var response = await GetAsync(path);
            return await ReadAsync<List<T>>(response) ?? new List<T>();
        }

        private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
        {
            // 2xx dışındaki cevaplar HttpRequestException olarak controller'a gider
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            // JSON metnini C# nesnesine çevirir. API camelCase (homeTeamId) gönderir, DTO'da HomeTeamId yazar;
            // Newtonsoft büyük/küçük harfe bakmadan eşler.
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
