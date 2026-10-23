using Futbolio.WebUI.DTOs.Match;
using Futbolio.WebUI.DTOs.MatchCard;
using Futbolio.WebUI.DTOs.MatchGoal;
using Futbolio.WebUI.DTOs.Substitution;
using Futbolio.WebUI.DTOs.Team;
using Futbolio.WebUI.Services;
using Futbolio.WebUI.ViewModels.Match;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Futbolio.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MatchesController : Controller
    {
        // API'deki MatchStatus ile aynı (0: Planlandı, 1: Canlı, 2: Tamamlandı)
        private const int StatusScheduled = 0;
        private const int StatusCompleted = 2;

        // API string alanları boş kabul etmediği için yer tutucu
        private const string NoValue = "-";

        private static readonly string[] CardTypes = { "Yellow", "Red" };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFootballDataService _data;
        private readonly string _apiUrl;

        // Constructor parametrelerini ASP.NET Core kendisi verir (Dependency Injection).
        // Program.cs'te kayıtlı servisler (HttpClient, IFootballDataService, appsettings) buraya otomatik gelir.
        public MatchesController(IHttpClientFactory httpClientFactory, IFootballDataService data, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _data = data;
            _apiUrl = (config["ApiSettings:BaseUrl"] ?? "https://localhost:7191/api/").TrimEnd('/');
        }

        // Maç listesi (+ modal için takım listesi)
        public async Task<IActionResult> Index()
        {
            var model = new MatchIndexViewModel();

            try
            {
                // İki isteği aynı anda başlatıp Task.WhenAll ile ikisinin de bitmesini bekliyoruz;
                // sırayla await etmekten daha hızlı. Görevler bittiği için aşağıdaki .Result beklemeden değeri verir.
                var matchesTask = _data.GetMatchesAsync();
                var teamsTask = _data.GetTeamsAsync();
                await Task.WhenAll(matchesTask, teamsTask);

                model.Matches = matchesTask.Result;
                model.Teams = teamsTask.Result;
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Maçlar veya takımlar API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
            }

            return View(model);
        }

        // Maç detayı: goller, kartlar ve oyuncu değişiklikleri burada yönetilir
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var matchTask = _data.GetMatchAsync(id);
                var teamsTask = _data.GetTeamsAsync();
                await Task.WhenAll(matchTask, teamsTask);

                if (matchTask.Result == null)
                {
                    // TempData: yönlendirmeden (Redirect) sonraki ilk sayfada okunup silinen geçici veri.
                    // ViewBag yönlendirmede kaybolur, TempData kaybolmaz.
                    TempData["MatchesError"] = "Maç bulunamadı ya da silinmiş.";
                    return RedirectToAction(nameof(Index));
                }

                var match = matchTask.Result;
                var teams = teamsTask.Result;

                return View(new MatchDetailViewModel
                {
                    Match = match,
                    HomeTeam = teams.FirstOrDefault(t => t.Id == match.HomeTeamId),
                    AwayTeam = teams.FirstOrDefault(t => t.Id == match.AwayTeamId)
                });
            }
            catch (HttpRequestException)
            {
                TempData["MatchesError"] = "Maç bilgisi API üzerinden alınamadı. API'nin çalıştığını kontrol edin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ---------- Maç ----------

        // Maç ekleme (modaldan fetch ile JSON gelir, API: POST /api/Matches)
        [HttpPost]
        // [FromBody]: dto form alanlarından değil, isteğin gövdesindeki JSON'dan doldurulur
        // (matches.js fetch ile JSON gönderir). JSON okunamazsa dto null gelir.
        public async Task<IActionResult> Create([FromBody] CreateMatchDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            var error = CheckMatch(dto.HomeTeamId, dto.AwayTeamId, dto.Week, dto.Status, dto.HomeScore, dto.AwayScore);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            // FindTeams üç değeri birlikte döndürür (tuple); bu satır onları üç ayrı değişkene ayırır.
            var (home, away, teamError) = await FindTeams(dto.HomeTeamId, dto.AwayTeamId);
            if (teamError != null)
                return teamError;

            // '!' derleyiciye "bu null değil" der: teamError null ise home ve away kesin doludur.
            dto.HomeTeamName = home!.Name;
            dto.HomeTeamLogoUrl = OrNoValue(home.LogoUrl);
            dto.AwayTeamName = away!.Name;
            dto.AwayTeamLogoUrl = OrNoValue(away.LogoUrl);
            dto.Stadium = StadiumOrDefault(dto.Stadium, home);
            if (dto.Status == StatusScheduled) { dto.HomeScore = 0; dto.AwayScore = 0; }

            return await SendToApi(
                client => client.PostAsync(_apiUrl + "/Matches", ToJsonContent(dto)),
                "Maç eklenemedi.");
        }

        // Maç güncelleme (API: PUT /api/Matches/{id})
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateMatchDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            if (dto.Id <= 0)
                return BadRequest(new { success = false, message = "Geçersiz maç." });

            var error = CheckMatch(dto.HomeTeamId, dto.AwayTeamId, dto.Week, dto.Status, dto.HomeScore, dto.AwayScore);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            var (home, away, teamError) = await FindTeams(dto.HomeTeamId, dto.AwayTeamId);
            if (teamError != null)
                return teamError;

            dto.HomeTeamName = home!.Name;
            dto.HomeTeamLogoUrl = OrNoValue(home.LogoUrl);
            dto.AwayTeamName = away!.Name;
            dto.AwayTeamLogoUrl = OrNoValue(away.LogoUrl);
            dto.Stadium = StadiumOrDefault(dto.Stadium, home);
            if (dto.Status == StatusScheduled) { dto.HomeScore = 0; dto.AwayScore = 0; }

            return await SendToApi(
                client => client.PutAsync(_apiUrl + "/Matches/" + dto.Id, ToJsonContent(dto)),
                "Maç güncellenemedi.");
        }

        // Maç silme (API: DELETE /api/Matches/{id}; gol, kart ve değişiklikler de silinir)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Geçersiz maç." });

            return await SendToApi(
                client => client.DeleteAsync(_apiUrl + "/Matches/" + id),
                "Maç silinemedi.");
        }

        // ---------- Gol (API: /api/MatchGoals) ----------

        // Id 0 ise ekler, doluysa günceller
        [HttpPost]
        public async Task<IActionResult> SaveGoal([FromBody] UpdateMatchGoalDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            if (string.IsNullOrWhiteSpace(dto.PlayerName))
                return BadRequest(new { success = false, message = "Gol atan oyuncunun adını girin." });

            var (teamName, error) = await CheckEvent(dto.MatchId, dto.TeamId, dto.Minute);
            if (error != null)
                return error;

            dto.TeamName = teamName!;
            dto.PlayerName = dto.PlayerName.Trim();

            if (dto.Id > 0)
            {
                return await SendToApi(
                    client => client.PutAsync(_apiUrl + "/MatchGoals/" + dto.Id, ToJsonContent(dto)),
                    "Gol güncellenemedi.");
            }

            var create = new CreateMatchGoalDto
            {
                MatchId = dto.MatchId,
                TeamId = dto.TeamId,
                TeamName = dto.TeamName,
                PlayerName = dto.PlayerName,
                Minute = dto.Minute
            };

            return await SendToApi(
                client => client.PostAsync(_apiUrl + "/MatchGoals", ToJsonContent(create)),
                "Gol eklenemedi.");
        }

        [HttpPost]
        // '=>' ile yazılan tek satırlık metot. async/await gerekmez, DeleteEvent'in Task'ını olduğu gibi döndürür.
        public Task<IActionResult> DeleteGoal(int id) =>
            DeleteEvent("/MatchGoals/", id, "Gol silinemedi.");

        // ---------- Kart (API: /api/MatchCards) ----------

        [HttpPost]
        public async Task<IActionResult> SaveCard([FromBody] UpdateMatchCardDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            if (string.IsNullOrWhiteSpace(dto.PlayerName))
                return BadRequest(new { success = false, message = "Kart gören oyuncunun adını girin." });

            var cardType = CardTypes.FirstOrDefault(t => string.Equals(t, dto.CardType?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (cardType == null)
                return BadRequest(new { success = false, message = "Kart tipi Sarı ya da Kırmızı olmalı." });

            var (teamName, error) = await CheckEvent(dto.MatchId, dto.TeamId, dto.Minute);
            if (error != null)
                return error;

            dto.TeamName = teamName!;
            dto.PlayerName = dto.PlayerName.Trim();
            dto.CardType = cardType;

            if (dto.Id > 0)
            {
                return await SendToApi(
                    client => client.PutAsync(_apiUrl + "/MatchCards/" + dto.Id, ToJsonContent(dto)),
                    "Kart güncellenemedi.");
            }

            var create = new CreateMatchCardDto
            {
                MatchId = dto.MatchId,
                TeamId = dto.TeamId,
                TeamName = dto.TeamName,
                PlayerName = dto.PlayerName,
                Minute = dto.Minute,
                CardType = dto.CardType
            };

            return await SendToApi(
                client => client.PostAsync(_apiUrl + "/MatchCards", ToJsonContent(create)),
                "Kart eklenemedi.");
        }

        [HttpPost]
        public Task<IActionResult> DeleteCard(int id) =>
            DeleteEvent("/MatchCards/", id, "Kart silinemedi.");

        // ---------- Oyuncu değişikliği (API: /api/Substitutions) ----------

        [HttpPost]
        public async Task<IActionResult> SaveSubstitution([FromBody] UpdateSubstitutionDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            if (string.IsNullOrWhiteSpace(dto.PlayerIn) || string.IsNullOrWhiteSpace(dto.PlayerOut))
                return BadRequest(new { success = false, message = "Oyuna giren ve çıkan oyuncunun adını girin." });

            if (string.Equals(dto.PlayerIn.Trim(), dto.PlayerOut.Trim(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Giren ve çıkan oyuncu aynı olamaz." });

            var (teamName, error) = await CheckEvent(dto.MatchId, dto.TeamId, dto.Minute);
            if (error != null)
                return error;

            dto.TeamName = teamName!;
            dto.PlayerIn = dto.PlayerIn.Trim();
            dto.PlayerOut = dto.PlayerOut.Trim();

            if (dto.Id > 0)
            {
                return await SendToApi(
                    client => client.PutAsync(_apiUrl + "/Substitutions/" + dto.Id, ToJsonContent(dto)),
                    "Oyuncu değişikliği güncellenemedi.");
            }

            var create = new CreateSubstitutionDto
            {
                MatchId = dto.MatchId,
                TeamId = dto.TeamId,
                TeamName = dto.TeamName,
                PlayerIn = dto.PlayerIn,
                PlayerOut = dto.PlayerOut,
                Minute = dto.Minute
            };

            return await SendToApi(
                client => client.PostAsync(_apiUrl + "/Substitutions", ToJsonContent(create)),
                "Oyuncu değişikliği eklenemedi.");
        }

        [HttpPost]
        public Task<IActionResult> DeleteSubstitution(int id) =>
            DeleteEvent("/Substitutions/", id, "Oyuncu değişikliği silinemedi.");

        // ---------- Yardımcılar ----------

        // API'ye gitmeden yakalanabilen iş kuralları. Aynı hafta kontrolünü API yapar.
        private static string? CheckMatch(int homeId, int awayId, int week, int status, int homeScore, int awayScore)
        {
            if (homeId <= 0 || awayId <= 0)
                return "Ev sahibi ve deplasman takımı seçilmeli.";

            if (homeId == awayId)
                return "Ev sahibi ve deplasman takımı aynı olamaz.";

            if (week < 1)
                return "Hafta 1 veya daha büyük olmalı.";

            if (status < StatusScheduled || status > StatusCompleted)
                return "Geçersiz maç durumu.";

            if (homeScore < 0 || awayScore < 0)
                return "Skor negatif olamaz.";

            return null;
        }

        // Gol/kart/değişiklik kaydı geçerli bir maça ve o maçın takımlarından birine bağlı olmalı.
        // API takım adını da istediği için adı maçtan alıp döner.
        private async Task<(string? teamName, IActionResult? error)> CheckEvent(int matchId, int teamId, int minute)
        {
            if (minute < 0 || minute > 130)
                return (null, BadRequest(new { success = false, message = "Dakika 0 ile 130 arasında olmalı." }));

            try
            {
                var match = await _data.GetMatchAsync(matchId);
                if (match == null)
                    return (null, NotFound(new { success = false, message = "Maç bulunamadı." }));

                if (teamId == match.HomeTeamId) return (match.HomeTeamName, null);
                if (teamId == match.AwayTeamId) return (match.AwayTeamName, null);

                return (null, BadRequest(new { success = false, message = "Takım bu maçın takımlarından biri olmalı." }));
            }
            catch (HttpRequestException)
            {
                return (null, ApiUnavailable());
            }
        }

        private async Task<IActionResult> DeleteEvent(string path, int id, string errorMessage)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Geçersiz kayıt." });

            return await SendToApi(client => client.DeleteAsync(_apiUrl + path + id), errorMessage);
        }

        // Ev sahibi ve deplasman takımını API'den bulur (isim, logo ve stadyum için)
        private async Task<(TeamResultDto? home, TeamResultDto? away, IActionResult? error)> FindTeams(int homeId, int awayId)
        {
            try
            {
                var teams = await _data.GetTeamsAsync();

                var home = teams.FirstOrDefault(t => t.Id == homeId);
                var away = teams.FirstOrDefault(t => t.Id == awayId);
                if (home == null || away == null)
                {
                    return (null, null, BadRequest(
                        new { success = false, message = "Ev sahibi veya deplasman takımı bulunamadı." }));
                }

                return (home, away, null);
            }
            catch (HttpRequestException)
            {
                return (null, null, ApiUnavailable());
            }
        }

        private static string OrNoValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? NoValue : value;

        // Stadyum boş geldiyse ev sahibi takımın stadyumu kullanılır
        private static string StadiumOrDefault(string? stadium, TeamResultDto home) =>
            !string.IsNullOrWhiteSpace(stadium) ? stadium.Trim() : OrNoValue(home.Stadium);

        private IActionResult ApiUnavailable() =>
            StatusCode(503, new
            {
                success = false,
                message = "API bağlantısı kurulamadı. API'nin çalıştığını kontrol edin."
            });

        // Gövde JSON olarak okunamadığında, modelin neden reddedildiğini söyler
        private IActionResult UnreadableRequest()
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                    $"{x.Key}: {(string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)}"));

            var detail = string.Join(" | ", errors);

            return BadRequest(new
            {
                success = false,
                message = "İstek gövdesi okunamadı. " +
                          (string.IsNullOrWhiteSpace(detail) ? "(gövde boş ya da JSON değil)" : detail)
            });
        }

        private static StringContent ToJsonContent(object dto)
        {
            var json = JsonConvert.SerializeObject(dto);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<IActionResult> SendToApi(
            // call: "HttpClient alıp API'ye istek atan fonksiyon". Her action kendi isteğini (Post/Put/Delete)
            // lambda olarak verir (client => client.PostAsync(...)); hata yönetimi bu metotta tek yerde kalır.
            Func<HttpClient, Task<HttpResponseMessage>> call,
            string errorMessage)
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await call(client);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true });
                }

                // API'nin döndürdüğü hata metnini de göster (ör. "Takımlardan biri bu hafta zaten...")
                var detail = await response.Content.ReadAsStringAsync();
                if (detail.Length > 500) detail = detail.Substring(0, 500);

                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    message = $"{errorMessage} " +
                              (string.IsNullOrWhiteSpace(detail) ? $"(API: {(int)response.StatusCode})" : detail.Trim('"'))
                });
            }
            // 'when' filtresi: sadece bu iki hata türünü yakalar (bağlantı hatası ve zaman aşımı).
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ApiUnavailable();
            }
        }
    }
}
