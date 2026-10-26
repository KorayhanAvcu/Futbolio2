"""
Futbolio uçtan uca (end-to-end) test.

Gerçek bir tarayıcı gibi WebUI'a HTTP istekleri atar, sonucu API'den kontrol eder:
    WebUI  ->  API  ->  SQL Server (boş bir test veritabanı)

Örnek verilere (Futbolio.API/Context/SeedData.cs) göre yazıldığı için veritabanı
YENİ ve BOŞ olmalı. En kolay yol run-e2e.ps1 ile çalıştırmak; o her şeyi hazırlar.

Elle çalıştırma:
    python Tests/e2e_test.py --web https://localhost:7288 --api https://localhost:7291/api

Sadece Python standart kütüphanesini kullanır (pip install gerekmez).
Çıkış kodu: 0 = tüm testler geçti, 1 = en az bir test başarısız.
"""
import argparse
import html
import json
import ssl
import sys
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser(description="Futbolio uçtan uca test")
parser.add_argument("--web", default="https://localhost:7288", help="WebUI adresi")
parser.add_argument("--api", default="https://localhost:7291/api", help="API adresi (/api dahil)")
args = parser.parse_args()

WEB = args.web.rstrip("/")
API = args.api.rstrip("/")

# Windows konsolunda Türkçe karakterler bozulmasın
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Geliştirme sertifikası (localhost) Python'un tanıdığı bir sertifika değil; doğrulamayı kapatıyoruz.
# Sadece localhost testleri için uygundur.
SSL = ssl._create_unverified_context()


class NoRedirect(urllib.request.HTTPRedirectHandler):
    """Yönlendirmeleri takip etme; 302 dönüp dönmediğini test edebilelim."""
    def redirect_request(self, *a, **k):
        return None


OPENER = urllib.request.build_opener(urllib.request.HTTPSHandler(context=SSL), NoRedirect)


def request(method, url, body=None):
    """(durum kodu, cevap metni) döner. 4xx/5xx de hata fırlatmaz, normal cevap gibi döner."""
    data = json.dumps(body, ensure_ascii=False).encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json; charset=utf-8")
    try:
        with OPENER.open(req, timeout=30) as res:
            return res.status, res.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def page(path):
    """WebUI sayfası. Razor Türkçe karakterleri &#x131; gibi yazar; html.unescape ile normal metne çeviriyoruz."""
    status, body = request("GET", WEB + path)
    return status, html.unescape(body)


def api(path):
    """API'den JSON okur."""
    status, body = request("GET", API + path)
    return status, parse(body)


def post(path, body=None):
    """WebUI'daki admin işlemleri (fetch ile yapılanların aynısı)."""
    status, text = request("POST", WEB + path, body)
    return status, parse(text)


def parse(text):
    try:
        return json.loads(text)
    except ValueError:
        return {}


results = []


def check(name, ok, detail=""):
    results.append((name, bool(ok), detail))


# ---------------------------------------------------------------------------
# Testler. Her grup ayrı bir fonksiyon; biri hata fırlatırsa diğerleri yine çalışır.
# ---------------------------------------------------------------------------

def test_seed():
    teams = api("/Teams")[1]
    matches = api("/Matches")[1]
    check("Örnek veri: 20 takım", len(teams) == 20, len(teams))
    check("Örnek veri: 30 maç", len(matches) == 30, len(matches))

    for week in (1, 2, 3):
        ms = [m for m in matches if m["week"] == week]
        ids = [t for m in ms for t in (m["homeTeamId"], m["awayTeamId"])]
        check(f"Örnek veri: {week}. hafta 10 maç, takım tekrarı yok", len(ms) == 10 and len(set(ids)) == 20)

    for match_id in (1, 2, 3):
        d = api(f"/Matches/{match_id}")[1]
        home_goals = sum(g["teamId"] == d["homeTeamId"] for g in d["goals"])
        away_goals = len(d["goals"]) - home_goals
        check(f"Örnek veri: maç {match_id} gol kaydı = skor, kart ve değişiklik var",
              (home_goals, away_goals) == (d["homeScore"], d["awayScore"]) and d["cards"] and d["substitutions"],
              f"{d['homeTeamName']} {d['homeScore']}-{d['awayScore']} {d['awayTeamName']}")

    check("API: maç detayında kart tipi geliyor",
          all("cardType" in c for c in api("/Matches/1")[1]["cards"]))


def test_pages():
    for path, code in [("/", 302), ("/Fixtures", 200), ("/Fixtures?hafta=1", 200), ("/Standings", 200),
                       ("/MatchDetail/Index/1", 200), ("/MatchDetail/Index/21", 200),
                       ("/MatchDetail/Index/999", 404),
                       ("/Admin", 200), ("/Admin/Matches", 200), ("/Admin/Matches/Detail/1", 200),
                       ("/Admin/Teams", 200), ("/Admin/Weeks", 200), ("/Admin/Standings", 200),
                       ("/js/admin.js", 200), ("/js/match-events.js", 200)]:
        status, _ = page(path)
        check(f"Sayfa {path} -> {code}", status == code, status)

    status, body = request("GET", WEB + "/Fixtures/Week/3")
    week3 = parse(body)
    check("Fikstür JSON: 3. hafta 10 maç", status == 200 and len(week3) == 10, body[:80])
    status, body = request("GET", WEB + "/Fixtures/Week/99")
    check("Fikstür JSON: maçsız hafta boş liste", status == 200 and parse(body) == [], body[:80])

    _, body = page("/Standings")
    check("Puan durumu: lider Napoli, son sırada Lecce", body.index("Napoli") < body.index("Lecce"))
    check("Puan durumu: form rozetleri var", "form-badge--w" in body)

    _, body = page("/MatchDetail/Index/1")
    check("Maç detayı: golcü, kart ve değişiklik görünüyor",
          all(s in body for s in ("Federico Baschirotto", "Warren Bondo", "Samuel Chukwueze", "i-card--yellow")))
    _, body = page("/MatchDetail/Index/21")
    check("Maç detayı: oynanmış maç", "Maç bitti" in body)

    _, body = page("/Admin/Weeks")
    check("Haftalar: 3 hafta kurallara uygun", body.count("Fikstür kurallara uygun") == 3)


def test_matches_events_teams():
    teams = {t["name"]: t["id"] for t in api("/Teams")[1]}
    inter, milan, roma = teams["Inter"], teams["Milan"], teams["Roma"]
    match = {"homeTeamId": inter, "awayTeamId": milan, "week": 4, "matchDate": "2026-11-08T20:45:00", "status": 0}

    # ---- Maç ekle + iş kuralları ----
    status, b = post("/Admin/Matches/Create", match)
    check("Maç ekle", status == 200 and b.get("success"), b)
    status, b = post("/Admin/Matches/Create", {**match, "homeTeamId": roma, "awayTeamId": inter})
    check("Kural: takım aynı hafta ikinci maça konamaz", status == 400 and "bu hafta" in b.get("message", ""), b)
    status, b = post("/Admin/Matches/Create", {**match, "homeTeamId": roma, "awayTeamId": roma})
    check("Kural: ev sahibi ve deplasman aynı olamaz", status == 400 and "aynı olamaz" in b.get("message", ""), b)
    status, b = post("/Admin/Matches/Create", {**match, "homeTeamId": roma, "awayTeamId": milan, "week": 0})
    check("Kural: hafta 1'den küçük olamaz", status == 400, b)
    new_id = max(m["id"] for m in api("/Matches")[1])

    # ---- Puan durumu: sadece tamamlanan maçlar ----
    points = lambda: {s["teamName"]: s["points"] for s in api("/Standings")[1]}
    before = points()
    status, b = post("/Admin/Matches/Update", {**match, "id": new_id, "status": 2, "homeScore": 3, "awayScore": 0})
    after = points()
    check("Maç güncelle (tamamlandı 3-0)", status == 200, b)
    check("Puan durumu: tamamlanan maç kazanana +3 ekledi",
          after["Inter"] == before["Inter"] + 3 and after["Milan"] == before["Milan"], (before["Inter"], after["Inter"]))
    post("/Admin/Matches/Update", {**match, "id": new_id, "status": 1, "homeScore": 1, "awayScore": 0})
    check("Kural: canlı maç puan durumuna girmez", points()["Inter"] == before["Inter"])

    # ---- Gol ----
    goal = {"matchId": new_id, "teamId": inter, "minute": 93, "playerName": "Hakan Çalhanoğlu"}
    status, b = post("/Admin/Matches/SaveGoal", goal)
    check("Gol ekle", status == 200, b)
    saved = api(f"/Matches/{new_id}")[1]["goals"][0]
    check("Gol: Türkçe karakterli isim doğru kaydedildi", saved["playerName"] == "Hakan Çalhanoğlu", saved["playerName"])
    status, b = post("/Admin/Matches/SaveGoal", {**goal, "id": saved["id"], "teamId": milan, "minute": 10})
    edited = api(f"/Matches/{new_id}")[1]["goals"][0]
    check("Gol düzenle (takım ve dakika değişti)", status == 200 and edited["teamId"] == milan and edited["minute"] == 10, b)
    check("Kural: gol maçın takımlarından birine ait olmalı", post("/Admin/Matches/SaveGoal", {**goal, "teamId": roma})[0] == 400)
    check("Kural: dakika 0-130 arası", post("/Admin/Matches/SaveGoal", {**goal, "minute": 131})[0] == 400)
    check("Kural: oyuncu adı boş olamaz", post("/Admin/Matches/SaveGoal", {**goal, "playerName": "  "})[0] == 400)
    check("Kural: olmayan maça gol eklenemez (404)", post("/Admin/Matches/SaveGoal", {**goal, "matchId": 99999})[0] == 404)
    status, b = post(f"/Admin/Matches/DeleteGoal?id={saved['id']}")
    check("Gol sil", status == 200 and api(f"/Matches/{new_id}")[1]["goals"] == [], b)
    check("Olmayan gol silinemez (404)", post("/Admin/Matches/DeleteGoal?id=99999")[0] == 404)

    # ---- Kart ----
    card = {"matchId": new_id, "teamId": milan, "minute": 40, "playerName": "Fikayo Tomori", "cardType": "red"}
    status, b = post("/Admin/Matches/SaveCard", card)
    cards = api(f"/Matches/{new_id}")[1]["cards"]
    check("Kart ekle ('red' -> 'Red' olarak kaydedilir)", status == 200 and cards[0]["cardType"] == "Red", b)
    check("Kural: kart tipi Sarı ya da Kırmızı", post("/Admin/Matches/SaveCard", {**card, "cardType": "Mavi"})[0] == 400)
    status, b = post(f"/Admin/Matches/DeleteCard?id={cards[0]['id']}")
    check("Kart sil", status == 200, b)

    # ---- Oyuncu değişikliği ----
    sub = {"matchId": new_id, "teamId": inter, "minute": 60, "playerIn": "Davide Frattesi", "playerOut": "Nicolò Barella"}
    status, b = post("/Admin/Matches/SaveSubstitution", sub)
    check("Oyuncu değişikliği ekle", status == 200, b)
    check("Kural: giren ve çıkan aynı oyuncu olamaz",
          post("/Admin/Matches/SaveSubstitution", {**sub, "playerOut": "davide frattesi"})[0] == 400)

    # ---- Maç silinince olayları da silinir (cascade) ----
    post("/Admin/Matches/SaveCard", {**card, "cardType": "Yellow"})
    detail = api(f"/Matches/{new_id}")[1]
    card_id, sub_id = detail["cards"][0]["id"], detail["substitutions"][0]["id"]
    status, b = post(f"/Admin/Matches/Delete?id={new_id}")
    check("Maç sil", status == 200, b)
    check("Silinen maç API'de 404", api(f"/Matches/{new_id}")[0] == 404)
    check("Maçın kartı ve değişikliği de silindi (cascade)",
          api(f"/MatchCards/{card_id}")[0] == 404 and api(f"/Substitutions/{sub_id}")[0] == 404)
    check("Admin: silinmiş maçın detayına gidince listeye yönlenir", page(f"/Admin/Matches/Detail/{new_id}")[0] == 302)

    # ---- Takım ----
    team = {"name": "E2E Test FC", "code": "E2E", "city": "Pisa", "region": "center", "stadium": "Arena Garibaldi",
            "capacity": 14000, "founded": 1909, "colorA": "#1E3A8A", "colorB": "#111111", "isActive": False}
    status, b = post("/Admin/Teams/Create", team)
    check("Takım ekle", status == 200, b)
    created = next(t for t in api("/Teams")[1] if t["name"] == team["name"])
    status, b = post("/Admin/Teams/Update", {**team, "id": created["id"], "city": "Pisa (PI)"})
    check("Takım güncelle", status == 200 and api(f"/Teams/{created['id']}")[1]["city"] == "Pisa (PI)", b)
    status, b = post(f"/Admin/Teams/Delete?id={created['id']}")
    check("Takım sil", status == 200, b)
    status, b = post(f"/Admin/Teams/Delete?id={inter}")
    check("Kural: maçı olan takım silinemez", status == 400 and "silinemez" in b.get("message", ""), b)


def wait_until_up(seconds=90):
    """API ve WebUI açılana kadar bekler (ilk açılışta veritabanı oluşturulduğu için biraz sürebilir)."""
    for _ in range(seconds):
        try:
            if request("GET", API + "/Teams")[0] == 200 and request("GET", WEB + "/Fixtures")[0] == 200:
                return True
        except OSError:
            pass
        time.sleep(1)
    return False


if __name__ == "__main__":
    if not wait_until_up():
        print(f"Sunuculara ulaşılamadı: {WEB} / {API}")
        sys.exit(1)

    for test in (test_seed, test_pages, test_matches_events_teams):
        try:
            test()
        except Exception as e:  # beklenmeyen bir cevap gelirse grubu hatalı say, devam et
            check(f"{test.__name__} çalışırken hata", False, repr(e))

    for name, ok, detail in results:
        print(("OK    " if ok else "HATA  ") + name + ("" if ok else f"   ->  {detail}"))

    passed = sum(ok for _, ok, _ in results)
    print(f"\n{passed}/{len(results)} test geçti")
    sys.exit(0 if passed == len(results) else 1)
