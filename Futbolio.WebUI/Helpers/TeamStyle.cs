using Futbolio.WebUI.DTOs.Team;
using Microsoft.AspNetCore.Html;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Futbolio.WebUI.Helpers
{
    // Takım arması (logo ya da renkli rozet) ve maç durumu metinleri. Tüm sayfalarda ortak.
    public static class TeamStyle
    {
        public const string DefaultColorA = "#1E5BD8";
        public const string DefaultColorB = "#FFFFFF";

        // API MatchStatus ile aynı (0: Planlandı, 1: Canlı, 2: Tamamlandı)
        public const int StatusScheduled = 0;
        public const int StatusLive = 1;
        public const int StatusCompleted = 2;

        public static string StatusKey(int status) => status switch
        {
            StatusLive => "Live",
            StatusCompleted => "Finished",
            _ => "NotStarted"
        };

        public static string StatusText(int status) => status switch
        {
            StatusLive => "Canlı",
            StatusCompleted => "Maç bitti",
            _ => "Başlamadı"
        };

        public static bool HasScore(int status) => status == StatusLive || status == StatusCompleted;

        // Takım kodu boşsa adın ilk 3 harfi
        public static string Code(TeamResultDto? team, string fallbackName = "")
        {
            if (team != null && !string.IsNullOrWhiteSpace(team.Code))
                return team.Code.ToUpperInvariant();

            var name = team?.Name ?? fallbackName;
            return (name.Length >= 3 ? name.Substring(0, 3) : name).ToUpperInvariant();
        }

        public static string ColorA(TeamResultDto? team) =>
            string.IsNullOrWhiteSpace(team?.ColorA) ? DefaultColorA : team.ColorA;

        public static string ColorB(TeamResultDto? team) =>
            string.IsNullOrWhiteSpace(team?.ColorB) ? DefaultColorB : team.ColorB;

        // Rozet yazısı: ana rengin açıklığına göre koyu/açık (teams.js inkFor ile aynı)
        public static string Ink(string hex)
        {
            var h = hex.TrimStart('#');
            if (h.Length != 6 || !int.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                return "#FFFFFF";

            // #RRGGBB sayısından kırmızı, yeşil ve mavi değerlerini ayırır (>> kaydırma, & 0xFF son 8 biti alır).
            // 0.299 / 0.587 / 0.114: gözün renklere duyarlılığına göre parlaklık formülü.
            var r = (rgb >> 16) & 0xFF;
            var g = (rgb >> 8) & 0xFF;
            var b = rgb & 0xFF;
            return 0.299 * r + 0.587 * g + 0.114 * b > 170 ? "#10224A" : "#FFFFFF";
        }

        public static string CrestStyle(TeamResultDto? team)
        {
            var a = ColorA(team);
            return $"--crest-a:{a};--crest-b:{ColorB(team)};--crest-ink:{Ink(a)}";
        }

        // LogoUrl "-" ya da "none" gibi yer tutucu olabilir; sadece gerçek adresleri logo say
        public static bool HasLogo(string? url) =>
            !string.IsNullOrWhiteSpace(url) &&
            (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("/"));

        // <img class="team-crest"> ya da renkli <span class="team-crest">KOD</span>
        // Admin paneldeki küçük armalarda kod yazılmaz (showCode: false)
        public static IHtmlContent Crest(TeamResultDto? team, string modifier = "", string fallbackName = "", bool showCode = true)
        {
            // Razor @ ile yazılan metni otomatik encode eder (< > zararsız olur). Burada HTML'i kendimiz
            // ürettiğimiz için HtmlString ile "bu hazır HTML" diyoruz; içindeki değişkenleri enc.Encode
            // ile kendimiz encode ediyoruz. Encode edilmezse takım adına yazılan bir <script> çalışabilirdi (XSS).
            var enc = HtmlEncoder.Default;
            var cls = ("team-crest " + modifier).Trim();

            if (HasLogo(team?.LogoUrl))
                return new HtmlString($"<img class=\"{cls}\" src=\"{enc.Encode(team!.LogoUrl!)}\" alt=\"\" aria-hidden=\"true\">");

            return new HtmlString(
                $"<span class=\"{cls}\" style=\"{enc.Encode(CrestStyle(team))}\" aria-hidden=\"true\">{(showCode ? enc.Encode(Code(team, fallbackName)) : "")}</span>");
        }

        // 93 -> "90+3'", 45 -> "45'"
        public static string MinuteLabel(int minute) =>
            minute > 90 ? $"90+{minute - 90}'" : $"{minute}'";
    }
}
