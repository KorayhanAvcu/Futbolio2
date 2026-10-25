using Futbolio.API.Entities;
using Futbolio.API.Entities.Common;
using System.Globalization;

namespace Futbolio.API.Context
{
    // GERÇEK VERİ: Serie A 2025-26 sezonunun ilk 3 haftası (23 Ağustos - 15 Eylül 2025).
    // 20 takım, 3 hafta x 10 maç (hepsi oynandı), 5 maçta gol/kart/oyuncu değişikliği detayı.
    // Skorlar, tarih/saatler ve maç olayları resmi maç raporlarından alındı (bkz. ACIKLAMALAR.md).
    // Sadece Teams tablosu boşsa çalışır; mevcut verilere dokunmaz.
    public static class SeedData
    {
        public static void Seed(ApiContext context)
        {
            if (context.Teams.Any())
                return;

            // ---------- Takımlar (2025-26 Serie A) ----------
            var teams = new List<Team>
            {
                NewTeam("Atalanta",      "ATA", "Bergamo",  "north",  "New Balance Arena",                         24950, 1907, "#1E6FB8", "#111111"),
                NewTeam("Bologna",       "BOL", "Bologna",  "north",  "Stadio Renato Dall'Ara",                    38279, 1909, "#1A2F5A", "#A21C26"),
                NewTeam("Cagliari",      "CAG", "Cagliari", "island", "Unipol Domus",                              16416, 1920, "#A5162B", "#0E2A56"),
                NewTeam("Como",          "COM", "Como",     "north",  "Stadio Giuseppe Sinigaglia",                13602, 1907, "#0B3E91", "#FFFFFF"),
                NewTeam("Cremonese",     "CRE", "Cremona",  "north",  "Stadio Giovanni Zini",                      20641, 1903, "#C8102E", "#8A8D8F"),
                NewTeam("Fiorentina",    "FIO", "Firenze",  "center", "Stadio Artemio Franchi",                    43147, 1926, "#5B2A86", "#FFFFFF"),
                NewTeam("Genoa",         "GEN", "Genova",   "north",  "Stadio Luigi Ferraris",                     36599, 1893, "#A2172E", "#002B5C"),
                NewTeam("Hellas Verona", "VER", "Verona",   "north",  "Stadio Marcantonio Bentegodi",              39371, 1903, "#0A2D6E", "#FFD200"),
                NewTeam("Inter",         "INT", "Milano",   "north",  "San Siro",                                  75817, 1908, "#0A4FA8", "#101418"),
                NewTeam("Juventus",      "JUV", "Torino",   "north",  "Allianz Stadium",                           41507, 1897, "#16181D", "#E8E8E8"),
                NewTeam("Lazio",         "LAZ", "Roma",     "center", "Stadio Olimpico",                           70634, 1900, "#8FD3F4", "#FFFFFF"),
                NewTeam("Lecce",         "LEC", "Lecce",    "south",  "Stadio Via del Mare",                       31533, 1908, "#F6D000", "#D21F26"),
                NewTeam("Milan",         "MIL", "Milano",   "north",  "San Siro",                                  75817, 1899, "#C8141E", "#111111"),
                NewTeam("Napoli",        "NAP", "Napoli",   "south",  "Stadio Diego Armando Maradona",             54726, 1926, "#1E9BD7", "#0C3C78"),
                NewTeam("Parma",         "PAR", "Parma",    "north",  "Stadio Ennio Tardini",                      22352, 1913, "#FFD200", "#1D3E8A"),
                NewTeam("Pisa",          "PIS", "Pisa",     "center", "Arena Garibaldi - Stadio Romeo Anconetani", 10000, 1909, "#0B2C69", "#111111"),
                NewTeam("Roma",          "ROM", "Roma",     "center", "Stadio Olimpico",                           70634, 1927, "#8E1F2F", "#F0B323"),
                NewTeam("Sassuolo",      "SAS", "Sassuolo", "north",  "Mapei Stadium",                             21584, 1920, "#00A651", "#111111"),
                NewTeam("Torino",        "TOR", "Torino",   "north",  "Stadio Olimpico Grande Torino",             27958, 1906, "#7E1B1B", "#E9D9B0"),
                NewTeam("Udinese",       "UDI", "Udine",    "north",  "Bluenergy Stadium",                         25144, 1896, "#26282C", "#FFFFFF"),
            };

            context.Teams.AddRange(teams);
            context.SaveChanges();

            var byCode = teams.ToDictionary(t => t.Code);

            // Ev sahibi stadyumunda oynanır.
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

            // Detaylı maçlar listenin başında: Id 1, 2, 3 bunlar olur (testler bu sıraya dayanır).
            var milanCremonese  = Game(1, "2025-08-23 20:45", "MIL", "CRE", 1, 2, done);
            var romaBologna     = Game(1, "2025-08-23 20:45", "ROM", "BOL", 1, 0, done);
            var juventusInter   = Game(3, "2025-09-13 18:00", "JUV", "INT", 4, 3, done);
            var interTorino     = Game(1, "2025-08-25 20:45", "INT", "TOR", 5, 0, done);
            var fiorentinaNapoli = Game(3, "2025-09-13 20:45", "FIO", "NAP", 1, 3, done);

            var matches = new List<Match>
            {
                milanCremonese,
                romaBologna,
                juventusInter,
                interTorino,
                fiorentinaNapoli,

                // ---------- 1. Hafta (23-25 Ağustos 2025) ----------
                Game(1, "2025-08-23 18:30", "GEN", "LEC", 0, 0, done),
                Game(1, "2025-08-23 18:30", "SAS", "NAP", 0, 2, done),
                Game(1, "2025-08-24 18:30", "CAG", "FIO", 1, 1, done),
                Game(1, "2025-08-24 18:30", "COM", "LAZ", 2, 0, done),
                Game(1, "2025-08-24 20:45", "ATA", "PIS", 1, 1, done),
                Game(1, "2025-08-24 20:45", "JUV", "PAR", 2, 0, done),
                Game(1, "2025-08-25 18:30", "UDI", "VER", 1, 1, done),

                // ---------- 2. Hafta (29-31 Ağustos 2025) ----------
                Game(2, "2025-08-29 18:30", "CRE", "SAS", 3, 2, done),
                Game(2, "2025-08-29 20:45", "LEC", "MIL", 0, 2, done),
                Game(2, "2025-08-30 18:30", "BOL", "COM", 1, 0, done),
                Game(2, "2025-08-30 18:30", "PAR", "ATA", 1, 1, done),
                Game(2, "2025-08-30 20:45", "NAP", "CAG", 1, 0, done),
                Game(2, "2025-08-30 20:45", "PIS", "ROM", 0, 1, done),
                Game(2, "2025-08-31 18:30", "GEN", "JUV", 0, 1, done),
                Game(2, "2025-08-31 18:30", "TOR", "FIO", 0, 0, done),
                Game(2, "2025-08-31 20:45", "INT", "UDI", 1, 2, done),
                Game(2, "2025-08-31 20:45", "LAZ", "VER", 4, 0, done),

                // ---------- 3. Hafta (13-15 Eylül 2025) ----------
                Game(3, "2025-09-13 15:00", "CAG", "PAR", 2, 0, done),
                Game(3, "2025-09-14 12:30", "ROM", "TOR", 0, 1, done),
                Game(3, "2025-09-14 15:00", "ATA", "LEC", 4, 1, done),
                Game(3, "2025-09-14 15:00", "PIS", "UDI", 0, 1, done),
                Game(3, "2025-09-14 18:00", "SAS", "LAZ", 1, 0, done),
                Game(3, "2025-09-14 20:45", "MIL", "BOL", 1, 0, done),
                Game(3, "2025-09-15 18:30", "VER", "CRE", 0, 0, done),
                Game(3, "2025-09-15 20:45", "COM", "GEN", 1, 1, done),
            };

            // ---------- Detaylı maçlar (gol sayıları skorla aynı) ----------
            // Details aşağıda tanımlı; C#'ta local function tanımından önce çağrılabilir.
            // new[] { ("CRE", "Federico Baschirotto", 28), ... } bir tuple dizisi: (takım kodu, oyuncu, dakika).
            // 90'ı geçen dakikalar uzatmadır: 91 -> ekranda 90+1'.

            // 1. hafta: Milan 1-2 Cremonese (San Siro, 23.08.2025)
            Details(milanCremonese, byCode["MIL"], byCode["CRE"],
                goals: new[] { ("CRE", "Federico Baschirotto", 28), ("MIL", "Strahinja Pavlović", 46), ("CRE", "Federico Bonazzoli", 61) },
                cards: new[] { ("CRE", "Alberto Grassi", 48, "Yellow"), ("CRE", "Filippo Terracciano", 69, "Yellow"), ("CRE", "Martín Payero", 77, "Yellow"),
                               ("MIL", "Álex Jiménez", 78, "Yellow"), ("CRE", "Warren Bondo", 88, "Yellow") },
                subs: new[] { ("MIL", "Álex Jiménez", "Pervis Estupiñán", 46), ("CRE", "Warren Bondo", "Alberto Grassi", 56), ("CRE", "Martín Payero", "Jari Vandeputte", 56),
                              ("MIL", "Samuel Chukwueze", "Fikayo Tomori", 65), ("CRE", "Antonio Sanabria", "David Okereke", 65), ("MIL", "Ardon Jashari", "Luka Modrić", 74),
                              ("CRE", "Federico Ceccherini", "Matteo Bianchetti", 83), ("CRE", "Manuel De Luca", "Federico Bonazzoli", 83) });

            // 1. hafta: Roma 1-0 Bologna (Olimpico, 23.08.2025)
            Details(romaBologna, byCode["ROM"], byCode["BOL"],
                goals: new[] { ("ROM", "Wesley", 53) },
                cards: new[] { ("BOL", "Remo Freuler", 19, "Yellow"), ("ROM", "Gianluca Mancini", 67, "Yellow") },
                subs: new[] { ("BOL", "Santiago Castro", "Ciro Immobile", 30), ("BOL", "Martin Vitík", "Nicolò Casale", 46), ("ROM", "Neil El Aynaoui", "Stephan El Shaarawy", 60),
                              ("BOL", "Nadir Zortea", "Lorenzo De Silvestri", 72), ("BOL", "Giovanni Fabbian", "Jens Odgaard", 72), ("ROM", "Paulo Dybala", "Matías Soulé", 74),
                              ("ROM", "Artem Dovbyk", "Evan Ferguson", 74), ("BOL", "Federico Bernardeschi", "Remo Freuler", 80), ("ROM", "Devyne Rensch", "Angeliño", 83) });

            // 3. hafta: Juventus 4-3 Inter (Allianz Stadium, 13.09.2025)
            Details(juventusInter, byCode["JUV"], byCode["INT"],
                goals: new[] { ("JUV", "Lloyd Kelly", 14), ("INT", "Hakan Çalhanoğlu", 30), ("JUV", "Kenan Yıldız", 38), ("INT", "Hakan Çalhanoğlu", 65),
                               ("INT", "Marcus Thuram", 76), ("JUV", "Khéphren Thuram", 83), ("JUV", "Vasilije Adžić", 91) },
                cards: new[] { ("JUV", "Teun Koopmeiners", 33, "Yellow"), ("JUV", "Manuel Locatelli", 70, "Yellow"), ("INT", "Henrikh Mkhitaryan", 86, "Yellow"), ("INT", "Francesco Acerbi", 90, "Yellow") },
                subs: new[] { ("INT", "Piotr Zieliński", "Nicolò Barella", 64), ("INT", "Federico Dimarco", "Carlos Augusto", 64), ("INT", "Ange-Yoan Bonny", "Lautaro Martínez", 64),
                              ("JUV", "Loïs Openda", "Dušan Vlahović", 73), ("JUV", "Vasilije Adžić", "Teun Koopmeiners", 73), ("JUV", "Juan Cabal", "Manuel Locatelli", 73),
                              ("INT", "Matteo Darmian", "Denzel Dumfries", 77), ("JUV", "João Mário", "Weston McKennie", 79), ("JUV", "Jonathan David", "Federico Gatti", 79),
                              ("INT", "Petar Sučić", "Hakan Çalhanoğlu", 81) });

            // 1. hafta: Inter 5-0 Torino (San Siro, 25.08.2025) - maçta kart çıkmadı
            Details(interTorino, byCode["INT"], byCode["TOR"],
                goals: new[] { ("INT", "Alessandro Bastoni", 18), ("INT", "Marcus Thuram", 36), ("INT", "Lautaro Martínez", 52), ("INT", "Marcus Thuram", 62), ("INT", "Ange-Yoan Bonny", 72) },
                cards: Array.Empty<(string, string, int, string)>(),
                subs: new[] { ("TOR", "Adrien Tameze", "Emirhan İlkhan", 58), ("TOR", "Zakaria Aboukhlal", "Nikola Vlašić", 58), ("TOR", "Ché Adams", "Giovanni Simeone", 64),
                              ("INT", "Ange-Yoan Bonny", "Marcus Thuram", 67), ("INT", "Carlos Augusto", "Federico Dimarco", 67), ("INT", "Piotr Zieliński", "Henrikh Mkhitaryan", 79),
                              ("INT", "Andy Diouf", "Lautaro Martínez", 79), ("TOR", "Marcus Pedersen", "Valentino Lazaro", 80), ("TOR", "Tino Anjorin", "Cyril Ngonge", 80),
                              ("INT", "Luis Henrique", "Alessandro Bastoni", 86) });

            // 3. hafta: Fiorentina 1-3 Napoli (Franchi, 13.09.2025) - oyunculara kart çıkmadı
            Details(fiorentinaNapoli, byCode["FIO"], byCode["NAP"],
                goals: new[] { ("NAP", "Kevin De Bruyne", 6), ("NAP", "Rasmus Højlund", 14), ("NAP", "Sam Beukema", 51), ("FIO", "Luca Ranieri", 79) },
                cards: Array.Empty<(string, string, int, string)>(),
                subs: new[] { ("FIO", "Roberto Piccoli", "Edin Džeko", 46), ("FIO", "Jacopo Fazzini", "Simon Sohm", 65), ("FIO", "Hans Nicolussi Caviglia", "Nicolò Fagioli", 65),
                              ("NAP", "Mathías Olivera", "Leonardo Spinazzola", 69), ("NAP", "David Neres", "Matteo Politano", 69), ("NAP", "Eljif Elmas", "Kevin De Bruyne", 69),
                              ("NAP", "Lorenzo Lucca", "Rasmus Højlund", 73), ("FIO", "Tariq Lamptey", "Pietro Comuzzo", 87), ("FIO", "Mattia Viti", "Marin Pongračić", 90),
                              ("NAP", "Billy Gilmour", "Stanislav Lobotka", 90) });

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
