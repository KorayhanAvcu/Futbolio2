using Futbolio.API.Entities;
using Futbolio.API.Entities.Common;
using System.Globalization;

namespace Futbolio.API.Context
{
    // Örnek veriler: 20 takım, 3 hafta x 10 maç, gol/kart/değişikliği girilmiş 3 maç.
    // Sadece Teams tablosu boşsa çalışır; mevcut verilere dokunmaz.
    public static class SeedData
    {
        public static void Seed(ApiContext context)
        {
            if (context.Teams.Any())
                return;

            // ---------- Takımlar ----------
            var teams = new List<Team>
            {
                NewTeam("Atalanta",   "ATA", "Bergamo",   "north",  "Gewiss Stadium",                24950, 1907, "#1E6FB8", "#111111"),
                NewTeam("Bologna",    "BOL", "Bologna",   "north",  "Stadio Renato Dall'Ara",        38279, 1909, "#1A2F5A", "#A21C26"),
                NewTeam("Cagliari",   "CAG", "Cagliari",  "island", "Unipol Domus",                  16416, 1920, "#A5162B", "#0E2A56"),
                NewTeam("Como",       "COM", "Como",      "north",  "Stadio Giuseppe Sinigaglia",    13602, 1907, "#0B3E91", "#FFFFFF"),
                NewTeam("Fiorentina", "FIO", "Firenze",   "center", "Stadio Artemio Franchi",        43147, 1926, "#5B2A86", "#FFFFFF"),
                NewTeam("Frosinone",  "FRO", "Frosinone", "center", "Stadio Benito Stirpe",          16227, 1928, "#1C4E9A", "#FFD200"),
                NewTeam("Genoa",      "GEN", "Genova",    "north",  "Stadio Luigi Ferraris",         36599, 1893, "#A2172E", "#002B5C"),
                NewTeam("Inter",      "INT", "Milano",    "north",  "San Siro",                      75817, 1908, "#0A4FA8", "#101418"),
                NewTeam("Juventus",   "JUV", "Torino",    "north",  "Allianz Stadium",               41507, 1897, "#16181D", "#E8E8E8"),
                NewTeam("Lazio",      "LAZ", "Roma",      "center", "Stadio Olimpico",               70634, 1900, "#8FD3F4", "#FFFFFF"),
                NewTeam("Lecce",      "LEC", "Lecce",     "south",  "Stadio Via del Mare",           31533, 1908, "#F6D000", "#D21F26"),
                NewTeam("Milan",      "MIL", "Milano",    "north",  "San Siro",                      75817, 1899, "#C8141E", "#111111"),
                NewTeam("Monza",      "MON", "Monza",     "north",  "U-Power Stadium",               16917, 1912, "#D2122E", "#FFFFFF"),
                NewTeam("Napoli",     "NAP", "Napoli",    "south",  "Stadio Diego Armando Maradona", 54726, 1926, "#1E9BD7", "#0C3C78"),
                NewTeam("Parma",      "PAR", "Parma",     "north",  "Stadio Ennio Tardini",          22352, 1913, "#FFD200", "#1D3E8A"),
                NewTeam("Roma",       "ROM", "Roma",      "center", "Stadio Olimpico",               70634, 1927, "#8E1F2F", "#F0B323"),
                NewTeam("Sassuolo",   "SAS", "Sassuolo",  "north",  "Mapei Stadium",                 21584, 1920, "#00A651", "#111111"),
                NewTeam("Torino",     "TOR", "Torino",    "north",  "Stadio Olimpico Grande Torino", 27958, 1906, "#7E1B1B", "#E9D9B0"),
                NewTeam("Udinese",    "UDI", "Udine",     "north",  "Bluenergy Stadium",             25144, 1896, "#26282C", "#FFFFFF"),
                NewTeam("Venezia",    "VEN", "Venezia",   "north",  "Stadio Pier Luigi Penzo",       11150, 1907, "#141414", "#F06A1A"),
            };

            context.Teams.AddRange(teams);
            context.SaveChanges();

            var byCode = teams.ToDictionary(t => t.Code);

            // Ev sahibi stadyumunda oynanır. Planlanan maçlarda skor 0-0 tutulur.
            // Game(...) bir local function: sadece bu metodun içinde kullanılan küçük yardımcı.
            // 'new()' dönüş tipinden (Match) anlaşılır, 'new Match()' yazmakla aynı.
            Match Game(int week, string date, string home, string away, int homeScore, int awayScore, int status) => new()
            {
                Week = week,
                MatchDate = DateTime.Parse(date, CultureInfo.InvariantCulture),
                HomeTeamId = byCode[home].Id,
                AwayTeamId = byCode[away].Id,
                HomeScore = homeScore,
                AwayScore = awayScore,
                Status = status,
                Stadium = byCode[home].Stadium,
                Goals = new List<Match_Goal>(),
                Cards = new List<Match_Card>(),
                Substitutions = new List<Substitution>()
            };

            const int done = MatchStatus.Completed;
            const int planned = MatchStatus.Scheduled;

            // ---------- 1. Hafta (tamamlandı) ----------
            var interMilan = Game(1, "2026-10-04 20:45", "INT", "MIL", 2, 1, done);
            var romaLazio = Game(1, "2026-10-04 18:00", "ROM", "LAZ", 1, 1, done);
            var napoliJuve = Game(1, "2026-10-03 20:45", "NAP", "JUV", 2, 0, done);

            var matches = new List<Match>
            {
                interMilan,
                romaLazio,
                napoliJuve,
                Game(1, "2026-10-03 15:00", "ATA", "BOL", 3, 1, done),
                Game(1, "2026-10-03 18:00", "FIO", "TOR", 1, 0, done),
                Game(1, "2026-10-04 12:30", "COM", "PAR", 2, 2, done),
                Game(1, "2026-10-04 15:00", "GEN", "SAS", 0, 1, done),
                Game(1, "2026-10-04 15:00", "CAG", "LEC", 1, 0, done),
                Game(1, "2026-10-05 18:30", "UDI", "VEN", 2, 1, done),
                Game(1, "2026-10-05 20:45", "MON", "FRO", 0, 0, done),

                // ---------- 2. Hafta (tamamlandı) ----------
                Game(2, "2026-10-18 20:45", "MIL", "NAP", 1, 2, done),
                Game(2, "2026-10-17 20:45", "JUV", "ROM", 1, 0, done),
                Game(2, "2026-10-18 18:00", "LAZ", "ATA", 2, 2, done),
                Game(2, "2026-10-17 18:00", "BOL", "INT", 0, 3, done),
                Game(2, "2026-10-17 15:00", "TOR", "COM", 1, 1, done),
                Game(2, "2026-10-18 12:30", "PAR", "FIO", 0, 2, done),
                Game(2, "2026-10-18 15:00", "SAS", "CAG", 2, 0, done),
                Game(2, "2026-10-18 15:00", "LEC", "GEN", 1, 1, done),
                Game(2, "2026-10-19 18:30", "VEN", "MON", 0, 1, done),
                Game(2, "2026-10-19 20:45", "FRO", "UDI", 2, 1, done),

                // ---------- 3. Hafta (planlandı) ----------
                Game(3, "2026-10-31 20:45", "INT", "JUV", 0, 0, planned),
                Game(3, "2026-11-01 20:45", "NAP", "ROM", 0, 0, planned),
                Game(3, "2026-10-31 18:00", "ATA", "MIL", 0, 0, planned),
                Game(3, "2026-11-01 18:00", "LAZ", "FIO", 0, 0, planned),
                Game(3, "2026-10-31 15:00", "COM", "BOL", 0, 0, planned),
                Game(3, "2026-11-01 15:00", "CAG", "TOR", 0, 0, planned),
                Game(3, "2026-11-01 12:30", "GEN", "PAR", 0, 0, planned),
                Game(3, "2026-11-01 15:00", "UDI", "LEC", 0, 0, planned),
                Game(3, "2026-11-02 18:30", "MON", "SAS", 0, 0, planned),
                Game(3, "2026-11-02 20:45", "FRO", "VEN", 0, 0, planned),
            };

            // ---------- Detaylı maçlar (gol sayıları skorla aynı) ----------
            // Details aşağıda tanımlı; C#'ta local function tanımından önce çağrılabilir.
            // new[] { ("INT", "Lautaro Martínez", 23), ... } bir tuple dizisi: (takım kodu, oyuncu, dakika).
            // Goals/Cards/Substitutions listelerine eklenenler SaveChanges'te maçla birlikte kaydedilir;
            // MatchId'yi EF Core kendisi doldurur.
            Details(interMilan, byCode["INT"], byCode["MIL"],
                goals: new[] { ("INT", "Lautaro Martínez", 23), ("MIL", "Christian Pulisic", 51), ("INT", "Marcus Thuram", 78) },
                cards: new[] { ("INT", "Nicolò Barella", 34, "Yellow"), ("MIL", "Fikayo Tomori", 61, "Yellow"), ("MIL", "Youssouf Fofana", 90, "Red") },
                subs: new[] { ("INT", "Davide Frattesi", "Henrikh Mkhitaryan", 65), ("MIL", "Samuel Chukwueze", "Yunus Musah", 70), ("MIL", "Tammy Abraham", "Álvaro Morata", 75), ("INT", "Mehdi Taremi", "Marcus Thuram", 85) });

            Details(romaLazio, byCode["ROM"], byCode["LAZ"],
                goals: new[] { ("ROM", "Artem Dovbyk", 38), ("LAZ", "Taty Castellanos", 72) },
                cards: new[] { ("ROM", "Gianluca Mancini", 20, "Yellow"), ("LAZ", "Matteo Guendouzi", 44, "Yellow"), ("ROM", "Bryan Cristante", 80, "Yellow") },
                subs: new[] { ("ROM", "Stephan El Shaarawy", "Matías Soulé", 60), ("LAZ", "Pedro", "Mattia Zaccagni", 70), ("LAZ", "Boulaye Dia", "Taty Castellanos", 82) });

            Details(napoliJuve, byCode["NAP"], byCode["JUV"],
                goals: new[] { ("NAP", "Scott McTominay", 12), ("NAP", "Romelu Lukaku", 67) },
                cards: new[] { ("JUV", "Manuel Locatelli", 15, "Yellow"), ("NAP", "Frank Anguissa", 50, "Yellow"), ("JUV", "Federico Gatti", 66, "Yellow") },
                subs: new[] { ("JUV", "Francisco Conceição", "Kenan Yıldız", 58), ("JUV", "Arkadiusz Milik", "Dušan Vlahović", 70), ("NAP", "Giovanni Simeone", "Romelu Lukaku", 80) });

            context.Matches.AddRange(matches);
            context.SaveChanges();

            // Takım kodunu ev sahibi / deplasman takımına çevirerek olayları maça bağlar
            void Details(Match match, Team home, Team away,
                (string Team, string Player, int Minute)[] goals,
                (string Team, string Player, int Minute, string CardType)[] cards,
                (string Team, string PlayerIn, string PlayerOut, int Minute)[] subs)
            {
                int TeamId(string code) => code == home.Code ? home.Id : away.Id;

                foreach (var g in goals)
                    match.Goals.Add(new Match_Goal { TeamId = TeamId(g.Team), PlayerName = g.Player, Minute = g.Minute });

                foreach (var c in cards)
                    match.Cards.Add(new Match_Card { TeamId = TeamId(c.Team), PlayerName = c.Player, Minute = c.Minute, CardType = c.CardType });

                foreach (var s in subs)
                    match.Substitutions.Add(new Substitution { TeamId = TeamId(s.Team), PlayerIn = s.PlayerIn, PlayerOut = s.PlayerOut, Minute = s.Minute });
            }
        }

        private static Team NewTeam(string name, string code, string city, string region, string stadium,
                                    int capacity, int founded, string colorA, string colorB) => new()
        {
            Name = name,
            Code = code,
            City = city,
            Region = region,
            Stadium = stadium,
            Capacity = capacity,
            Founded = founded,
            ColorA = colorA,
            ColorB = colorB,
            IsActive = true,
            LogoUrl = null
        };
    }
}
