using Futbolio.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

// API cevap vermezse sayfa 100 sn (varsayılan) beklemesin, 15 sn sonra hata versin
builder.Services.ConfigureHttpClientDefaults(http =>
    http.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(15)));

// API okuma servisi. Adres appsettings.json -> ApiSettings:BaseUrl
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7191/api/";
// Controller'da IFootballDataService istendiğinde FootballDataService oluşturulur ve
// BaseAddress'i ayarlı bir HttpClient verilir (Services/FootballDataService.cs).
builder.Services.AddHttpClient<IFootballDataService, FootballDataService>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
