using Futbolio.WebUI.DTOs.Team;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Futbolio.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TeamsController : Controller
    {
        // Logo yüklenene kadar API'nin LogoUrl alanı için yer tutucu
        private const string NoLogo = "-";

        private readonly IHttpClientFactory _httpClientFactory;

        // API adresi appsettings.json -> ApiSettings:BaseUrl
        private readonly string ApiUrl;

        public TeamsController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            ApiUrl = (config["ApiSettings:BaseUrl"] ?? "https://localhost:7191/api/").TrimEnd('/') + "/Teams";
        }

        // Takım listesi
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(ApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Takımlar API üzerinden alınamadı.";
                    return View(new List<TeamResultDto>());
                }

                var json = await response.Content.ReadAsStringAsync();

                var teams =
                    JsonConvert.DeserializeObject<List<TeamResultDto>>(json)
                    ?? new List<TeamResultDto>();

                return View(teams);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "API bağlantısı kurulamadı. API'nin çalıştığını kontrol edin.";
                return View(new List<TeamResultDto>());
            }
        }

        // Takım ekleme (modaldan fetch ile JSON gelir)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.Name)) missing.Add("Name");
            if (string.IsNullOrWhiteSpace(dto.Code)) missing.Add("Code");
            if (string.IsNullOrWhiteSpace(dto.City)) missing.Add("City");
            if (missing.Count > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Eksik alan: " + string.Join(", ", missing)
                });
            }

            if (string.IsNullOrWhiteSpace(dto.LogoUrl)) dto.LogoUrl = NoLogo;

            return await SendToApi(
                client => client.PostAsync(ApiUrl, ToJsonContent(dto)),
                "Takım eklenemedi.");
        }

        // Takım güncelleme (API: PUT /api/Teams/{id})
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateTeamDto dto)
        {
            if (dto == null)
                return UnreadableRequest();

            if (dto.Id <= 0 || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Eksik alan: " + (dto.Id <= 0 ? "Id " : "") +
                              (string.IsNullOrWhiteSpace(dto.Name) ? "Name" : "")
                });
            }

            if (string.IsNullOrWhiteSpace(dto.LogoUrl)) dto.LogoUrl = NoLogo;

            return await SendToApi(
                client => client.PutAsync(ApiUrl + "/" + dto.Id, ToJsonContent(dto)),
                "Takım güncellenemedi.");
        }

        // Takım silme (API: DELETE /api/Teams/{id})
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Geçersiz takım." });
            }

            return await SendToApi(
                client => client.DeleteAsync(ApiUrl + "/" + id),
                "Takım silinemedi.");
        }

        // ---------- Yardımcılar ----------

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

                // API'nin döndürdüğü hata metnini de göster
                var detail = await response.Content.ReadAsStringAsync();
                if (detail.Length > 500) detail = detail.Substring(0, 500);

                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    message = $"{errorMessage} (API: {(int)response.StatusCode}) " +
                              (string.IsNullOrWhiteSpace(detail) ? "[API yanıt gövdesi boş]" : detail)
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "API bağlantısı kurulamadı. API'nin çalıştığını kontrol edin."
                });
            }
        }
    }
}