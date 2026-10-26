# Futbolio: Proje Açıklamaları

Bu dosya projenin nasıl çalıştığını, hangi dosyanın ne iş yaptığını ve kodda geçen,
ilk bakışta yabancı gelebilecek kavramları anlatır. Kod içindeki kısa yorumlar da
bu dosyadaki başlıklara karşılık gelir.

---

## 1. Genel yapı

```
Tarayıcı  ──►  Futbolio.WebUI (MVC, port 7088)  ──►  Futbolio.API (Web API, port 7191)  ──►  SQL Server
   ▲                   │  HTML sayfa / JSON                    │  JSON                          (FutbolioDb)
   └───────────────────┘                                       └── Entity Framework Core
```

- **API** veritabanına erişen tek projedir. Tüm iş kuralları (aynı takım iki kez oynayamaz,
  puan hesabı vb.) buradadır.
- **WebUI** veritabanını hiç görmez. Her veriyi API'den HTTP ile ister, sayfayı oluşturur.
- **Tarayıcı** API'ye doğrudan gitmez; hep WebUI üzerinden gider (bkz. [CORS](#cors)).

## 2. Çalıştırma

1. `Futbolio.API/appsettings.json` içindeki `DefaultConnection`'ı kendi SQL Server'ına göre ayarla.
   Kendi bilgisayarında genelde `Server=.\SQLEXPRESS;Database=FutbolioDb;Trusted_Connection=True;TrustServerCertificate=True;`
2. Önce **API**'yi çalıştır. İlk açılışta:
   - veritabanı yoksa oluşturulur ve migration'lar uygulanır,
   - Teams tablosu boşsa örnek veriler eklenir (20 takım, 3 hafta × 10 maç, 3 detaylı maç).
3. Sonra **WebUI**'ı çalıştır. API adresi `Futbolio.WebUI/appsettings.json` → `ApiSettings:BaseUrl`.
4. Visual Studio'da ikisini birlikte başlatmak için: Solution'a sağ tık → *Configure Startup Projects* →
   *Multiple startup projects* → ikisini de *Start* yap (API üstte olsun).

| Sayfa | Adres |
|---|---|
| Fikstür (ana sayfa) | `/Fixtures` |
| Puan durumu | `/Standings` |
| Maç detayı | `/MatchDetail/Index/{id}` |
| Admin | `/Admin` |
| API dokümantasyonu (Swagger) | API adresi + `/swagger` |

## 3. Bir isteğin yolculuğu

### Örnek 1: Fikstürde hafta değiştirmek

1. Tarayıcı `/Fixtures` ister → [FixturesController.Index](Futbolio.WebUI/Controllers/FixturesController.cs)
   API'den tüm maçları ve takımları alır, hafta listesini hesaplar, sayfayı çizer.
   Takım renkleri ve hafta listesi sayfaya JSON olarak gömülür
   ([Index.cshtml](Futbolio.WebUI/Views/Fixtures/Index.cshtml) en altı).
2. Kullanıcı "3. Hafta"ya tıklar → [fixtures.js](Futbolio.WebUI/wwwroot/js/fixtures.js)
   `fetch('/Fixtures/Week/3')` ile WebUI'a istek atar (sayfa yenilenmez).
3. `FixturesController.Week` API'nin `GET /api/matches/week/3` adresini çağırır, cevabı JSON olarak geri verir.
4. fixtures.js maç kartlarını HTML'e çevirip sayfaya yerleştirir.

### Örnek 2: Admin'de gol eklemek

1. Admin → Maçlar → göz ikonu → [Detail.cshtml](Futbolio.WebUI/Areas/Admin/Views/Matches/Detail.cshtml).
   Mevcut goller tablo satırlarına `data-*` öznitelikleriyle yazılır.
2. "Gol Ekle" → [match-events.js](Futbolio.WebUI/wwwroot/js/match-events.js) modalı açar, formu doğrular,
   `fetch('/Admin/Matches/SaveGoal', { body: JSON })` gönderir.
3. [Admin MatchesController.SaveGoal](Futbolio.WebUI/Areas/Admin/Controllers/MatchesController.cs):
   oyuncu adı boş mu, dakika 0–130 arasında mı, takım bu maçın takımı mı diye bakar
   (`CheckEvent`). `Id` 0 ise API'ye `POST /api/MatchGoals`, doluysa `PUT /api/MatchGoals/{id}` gönderir.
4. API ([MatchGoalsController](Futbolio.API/Controllers/MatchGoalsController.cs)) aynı kuralları
   tekrar kontrol eder ve kaydeder. WebUI `{ success: true }` döner.
5. JS sayfayı yeniler; bildirimi göstermek için mesajı önce `sessionStorage`'a bırakır.

> **Neden kurallar iki yerde?** WebUI'daki kontrol kullanıcıya hızlı ve anlaşılır mesaj vermek
> içindir. Asıl güvence API'dedir: API'yi başka bir uygulama (ör. Swagger) çağırsa da kurallar çalışır.

## 4. Dosyalar

### Futbolio.API

| Dosya / klasör | Ne yapar |
|---|---|
| [Program.cs](Futbolio.API/Program.cs) | Servisleri kaydeder; açılışta migration + örnek veri |
| [Context/ApiContext.cs](Futbolio.API/Context/ApiContext.cs) | EF Core: tablolar ve ilişkiler (silme davranışları) |
| [Context/SeedData.cs](Futbolio.API/Context/SeedData.cs) | Gerçek Serie A 2025-26 verileri: ilk 3 hafta, 30 maç, 5 detaylı maç (sadece veritabanı boşsa) |
| `Entities/` | Veritabanı tabloları (Team, Match, Match_Goal, Match_Card, Substitution) |
| `DTOs/` | API'nin aldığı/verdiği veri şekilleri. Entity'yi doğrudan dışarı vermeyiz |
| [Mapping/MappingProfile.cs](Futbolio.API/Mapping/MappingProfile.cs) | AutoMapper: Entity ↔ DTO dönüşümleri |
| [Controllers/MatchesController.cs](Futbolio.API/Controllers/MatchesController.cs) | Maç CRUD + iş kuralları (`CheckMatch`) |
| [Controllers/StandingsController.cs](Futbolio.API/Controllers/StandingsController.cs) | Puan durumu: tamamlanan maçlardan anlık hesap |
| `Controllers/MatchGoals…`, `MatchCards…`, `Substitutions…` | Gol, kart, değişiklik CRUD |

### Futbolio.WebUI

| Dosya / klasör | Ne yapar |
|---|---|
| [Program.cs](Futbolio.WebUI/Program.cs) | HttpClient ayarları, `IFootballDataService` kaydı, route'lar |
| [Services/FootballDataService.cs](Futbolio.WebUI/Services/FootballDataService.cs) | API'den **okuma** yapan tek yer |
| [Helpers/TeamStyle.cs](Futbolio.WebUI/Helpers/TeamStyle.cs) | Takım arması, durum metinleri, dakika yazımı (90+3') |
| `Controllers/` | Kullanıcı sayfaları: Fixtures, Standings, MatchDetail |
| `Areas/Admin/Controllers/` | Admin: Home (özet), Matches (+ gol/kart/değişiklik), Teams, Weeks, Standings |
| `ViewModels/` | Bir sayfanın ihtiyacı olan verilerin toplandığı sınıflar |
| `Views/`, `Areas/Admin/Views/` | Razor sayfaları |
| `wwwroot/js/` | Sayfa davranışları (filtre, modal, fetch). Her sayfanın kendi dosyası var |

### Tests

| Dosya | Ne yapar |
|---|---|
| [Tests/run-e2e.ps1](Tests/run-e2e.ps1) | Uçtan uca testi tek komutla çalıştırır (bkz. [7. Uçtan uca test](#7-uçtan-uca-test)) |
| [Tests/e2e_test.py](Tests/e2e_test.py) | Test kontrolleri |

**Okuma ve yazma neden farklı yoldan?** Okumalar (`GET`) `IFootballDataService` üzerinden gider.
Admin'deki kayıt işlemleri (`POST/PUT/DELETE`) controller'daki `SendToApi` ile gider, çünkü API'nin
hata mesajını ("Takımlardan biri bu hafta zaten…") kullanıcıya aynen göstermek istiyoruz.

## 5. İş kuralları nerede?

| Kural | Yer |
|---|---|
| Ev sahibi ve deplasman aynı olamaz | API `MatchesController.CheckMatch` (+ WebUI'da ön kontrol) |
| Takım aynı hafta iki maçta olamaz | API `MatchesController.CheckMatch` · Admin Haftalar sayfası da uyarır |
| Puan durumuna sadece tamamlanan maçlar girer | API `StandingsController` (`Status == Completed`) |
| Averaj = atılan − yenilen; sıralama puan → averaj → atılan gol | API `StandingsController` |
| Gol/kart/değişiklik geçerli maça ve o maçın takımına bağlı | API `CheckMatchAndTeam`, WebUI `CheckEvent` |
| Bulunamayan kayıtta 404 | Tüm API controller'ları `NotFound(...)` |

## 6. Kavramlar sözlüğü

### C# ve ASP.NET Core

**Dependency Injection (DI)**: Controller'a `new FootballDataService()` yazmayız; constructor'da
parametre olarak isteriz, ASP.NET Core verir. Hangi arayüze hangi sınıfın verileceği `Program.cs`'te
kaydedilir. *Nerede:* tüm controller'ların constructor'ları.

**IHttpClientFactory / typed client**: `new HttpClient()` her seferinde yeni bağlantı açar ve yük altında
sorun çıkarır. Factory bağlantıları yeniden kullanır. `AddHttpClient<IFootballDataService, FootballDataService>`
servise adresi ayarlı bir HttpClient verir. *Nerede:* `WebUI/Program.cs`, `FootballDataService`.

**async / await, Task.WhenAll**: API'ye istek beklerken sunucu boşa durmasın diye `await` kullanılır.
Birbirinden bağımsız iki isteği aynı anda başlatıp `Task.WhenAll` ile ikisini birlikte bekleriz;
sırayla beklemekten hızlıdır. Bittikten sonra `.Result` bekletmeden değeri verir.
*Nerede:* çoğu `Index` action'ı.

**[FromBody]**: Parametre form alanlarından değil, isteğin gövdesindeki JSON'dan doldurulur.
fetch ile JSON gönderdiğimiz action'larda kullanılır. JSON bozuksa parametre `null` gelir.

**ViewBag / TempData**: İkisi de controller'dan view'a küçük bilgi taşır. `ViewBag` sadece o istekte
yaşar. `TempData` bir yönlendirme (`RedirectToAction`) sonrasındaki sayfaya kadar yaşar.

**Tuple ve ayrıştırma**: `(string? teamName, IActionResult? error)` iki değeri birlikte döndürür.
`var (teamName, error) = await CheckEvent(...)` onları iki değişkene ayırır.

**Local function**: Bir metodun içinde tanımlanan küçük yardımcı fonksiyon (`string Side(int id) => ...`).
Sadece o metotta kullanılır, tanımından önce de çağrılabilir. Razor'daki `@{ ... }` bloklarında da var.

**`?.`, `??`, `!`**:
- `team?.Name`: team null ise hata vermez, sonuç null olur.
- `x ?? y`: x null ise y kullanılır.
- `home!.Name`: derleyiciye "bu null değil" demektir (biz kontrol ettik).

**`=>` (expression-bodied)**: Tek satırlık metodun kısa yazımı.
`int Kare(int x) => x * x;` ile `{ return x * x; }` aynıdır.

**Exception filtresi (`when`)**: `catch (Exception ex) when (ex is A or B)` sadece A ve B hatalarını yakalar.

**HttpRequestException / TaskCanceledException**: API kapalıysa ilki, 15 saniyede cevap vermezse
ikincisi fırlar. Servis ikincisini de birinciye çevirir; controller'lar tek türü yakalar ve sayfada
"API'ye ulaşılamadı" gösterir.

**IHtmlContent / HtmlString**: Razor `@değer` yazınca metni encode eder (`<` → `&lt;`), böylece
takım adına yazılmış bir `<script>` çalışmaz (XSS koruması). `TeamStyle.Crest` HTML'i kendisi ürettiği
için `HtmlString` döner ve içindeki değişkenleri kendisi encode eder.

**Area**: Admin sayfaları `Areas/Admin` altında ayrı bir bölümdür; `[Area("Admin")]` ve
`/Admin/...` adresleriyle kullanıcı sayfalarından ayrılır.

**`@section Styles / Scripts`**: Bir view'ın layout'taki belirli bir yere CSS/JS eklemesi.
Layout'ta `RenderSectionAsync("Scripts")` olan yere yerleşir.

### Entity Framework Core

**DbContext ve scope**: `ApiContext` her HTTP isteği için yeniden oluşturulur ("scoped").
Uygulama açılırken istek olmadığından `Program.cs` `CreateScope()` ile geçici bir kapsam açar.

**Migration / `Database.Migrate()`**: Migration'lar tablo yapısındaki değişikliklerin kaydıdır.
`Migrate()` veritabanına henüz uygulanmamış olanları uygular; `dotnet ef database update` ile aynı iştir.

**Include / ThenInclude**: İlişkili tabloları aynı sorguda getirir (maç + takımları + golleri).
Yazılmazsa `match.HomeTeam` null gelir.

**DeleteBehavior.Cascade / Restrict**: Maç silinince golleri, kartları ve değişiklikleri de silinir
(Cascade). Maçı olan takım silinemez (Restrict). *Nerede:* `ApiContext.OnModelCreating`.

### LINQ

| İfade | Anlamı |
|---|---|
| `Where` / `Select` | Filtrele / dönüştür |
| `GroupBy(m => m.Week)` | Haftaya göre grupla (her grup bir hafta) |
| `SelectMany` | Her elemandan birden çok eleman üretip tek listede topla |
| `TakeLast(5)` | Son 5 eleman |
| `DefaultIfEmpty(x)` | Liste boşsa `x` içeren tek elemanlı liste (Min/Max hata vermesin) |
| `GetValueOrDefault(key)` | Sözlükte yoksa hata yerine `null` |
| `list[^1]` | Listenin son elemanı |

### JavaScript (wwwroot/js)

**IIFE `(function () { ... })();`**: Dosyadaki değişkenler global olmasın diye her şey hemen çalışan
bir fonksiyonun içindedir.

**`data-*` ve `dataset`**: Sunucu, tablo satırına `data-team-id="4"` yazar. JS bunu
`tr.dataset.teamId` ile okur (tireli ad camelCase'e döner; değer her zaman metindir).

**`<script type="application/json">`**: Tarayıcı çalıştırmaz, sadece veri taşır. Fikstür sayfası
takım ve hafta listesini böyle taşır, JS `JSON.parse` ile okur.

**fetch**: Sayfayı yenilemeden sunucuya istek atar. `res.ok` 200–299 arası durumlarda true olur.

**`<dialog>`**: Tarayıcının hazır modal penceresi. `showModal()` açar. İçindeki
`<form method="dialog">` butonu pencereyi kapatıp kendi `value`'sunu `dialog.returnValue`'ya yazar;
silme onayında "confirm" mi "cancel" mı basıldığını böyle anlarız.

**sessionStorage**: Sekme kapanana kadar duran küçük depo. Kayıttan sonra sayfa yenilenir; "Gol eklendi"
bildirimini yenilemeden sonra gösterebilmek için mesaj buraya bırakılır.

**`esc(...)`**: Veriyi `innerHTML` ile sayfaya koymadan önce `< > & " '` karakterlerini zararsız
hale getirir (XSS koruması).

<a id="cors"></a>**CORS**: Tarayıcı, bir sayfanın başka bir adresteki (farklı port da farklı sayılır)
sunucuya JS ile istek atmasını varsayılan olarak engeller. API'de CORS izni açmak yerine tarayıcı
WebUI'a gider, WebUI sunucu tarafında API'yi çağırır. Case'in istediği "UI sadece API'yi tüketir"
yapısı da böyle korunur.

## 7. Uçtan uca test

`Tests` klasöründeki test, projeyi gerçek bir kullanıcı gibi dener. WebUI'a istek atar
(sayfaları açar, admin'den maç/gol/kart/değişiklik/takım ekler, düzenler, siler), sonucu API'den
kontrol eder. Toplam 60 kontrol yapar: örnek veri, tüm sayfalar, iş kuralları ve silme davranışları.

| Dosya | Ne yapar |
|---|---|
| [Tests/run-e2e.ps1](Tests/run-e2e.ps1) | Her şeyi hazırlar, testi çalıştırır, sonra temizler. **Normalde bunu çalıştır.** |
| [Tests/e2e_test.py](Tests/e2e_test.py) | Testlerin kendisi (Python, ek paket gerekmez) |

### Gerekenler

- **.NET SDK** (projeyi derlemek için, zaten kurulu)
- **SQL Server** ve **sqlcmd** (SQL Server ile birlikte gelir)
- **Python 3**: [python.org](https://www.python.org/downloads/). Kurarken "Add python.exe to PATH" seçeneğini işaretle.

### Çalıştırma

Solution klasöründe (`Futbolio.slnx`'in olduğu yer) PowerShell aç:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tests\run-e2e.ps1
```

Visual Studio'da: *View → Terminal* ile alttaki terminale aynı komutu yaz.

Script sırasıyla şunları yapar:

1. API ve WebUI'ı geçici bir klasöre (`%TEMP%\futbolio-e2e`) derler. Visual Studio'da proje açık ya da
   çalışıyor olsa bile çakışmaz.
2. `FutbolioDb_E2E` adlı **ayrı** bir test veritabanını sıfırlar. Asıl `FutbolioDb` veritabanına dokunmaz;
   adında "Test" ya da "E2E" geçmeyen bir veritabanını kabul etmez.
3. API'yi `https://localhost:7291`, WebUI'ı `https://localhost:7288` adresinde başlatır (Visual Studio'nun
   kullandığı 7191/7088 portlarından farklı). API açılırken tabloları kurar ve örnek verileri ekler.
4. Testleri çalıştırır ve her kontrolü `OK` ya da `HATA` olarak yazar.
5. Sunucuları kapatır, test veritabanını ve geçici dosyaları siler.

Sonuç en altta yazar: `60/60 test geçti` ve `TÜM TESTLER GEÇTİ`. Hata varsa çıkış kodu 1 olur ve
sunucu loglarının yeri (`%TEMP%\futbolio-e2e\api.log`, `web.log`) gösterilir.

### Seçenekler

```powershell
# Farklı SQL Server (ör. tam sürüm SQL Server)
powershell -ExecutionPolicy Bypass -File .\Tests\run-e2e.ps1 -Server "localhost"

# Test bitince veritabanını silme (SSMS ile içine bakmak için)
powershell -ExecutionPolicy Bypass -File .\Tests\run-e2e.ps1 -KeepDatabase

# Sunucuları kendin başlattıysan sadece testleri çalıştır (veritabanı boş ve yeni olmalı)
python Tests\e2e_test.py --web https://localhost:7088 --api https://localhost:7191/api
```

### Bilmen gerekenler

- **Testler örnek verilere göre yazıldı.** Örneğin "1. maç Milan 1-2 Cremonese" ya da "lider Napoli" gibi
  kontroller [SeedData.cs](Futbolio.API/Context/SeedData.cs)'teki verilere dayanır. Bu yüzden
  veritabanı her seferinde boş başlamalı; `run-e2e.ps1` bunu kendisi yapar. Örnek verileri
  değiştirirsen ilgili kontrolleri de `e2e_test.py`'de güncelle.
- **`-ExecutionPolicy Bypass` neden gerekli?** Windows varsayılan olarak indirilen/yerel `.ps1`
  dosyalarının çalışmasını engelleyebilir. Bu parametre sadece bu komut için izin verir, ayarı kalıcı değiştirmez.
- **Yeni bir test eklemek:** `e2e_test.py`'deki test fonksiyonlarından birine
  `check("Açıklama", koşul, hata_detayı)` satırı ekle. Koşul `False` olursa test `HATA` olarak yazılır.
  `page(...)` WebUI sayfası açar, `api(...)` API'den JSON okur, `post(...)` admin işlemi yapar.

## 8. Bilinen sınırlamalar

- **Admin panelinde giriş yok.** Adresi bilen herkes kayıt ekleyip silebilir. Gerçek kullanımda
  ASP.NET Core Identity ya da cookie authentication ile `[Authorize]` eklenmeli; giriş geldiğinde
  POST işlemlerine anti-forgery token (CSRF koruması) da eklenmeli.
- **Maç skoru ile gol kayıtları ayrı tutulur.** Skoru maç formundan, golleri detay sayfasından
  girersin. Sayıları uyuşmazsa hem admin hem kullanıcı maç detayında uyarı çıkar.
- **"Ertelendi" durumu yok.** API'de sadece 0 Planlandı, 1 Canlı, 2 Tamamlandı var.
