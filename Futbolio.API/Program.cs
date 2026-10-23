using Futbolio.API.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApiContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Veritabanını son migration'a getir, boşsa örnek verileri ekle (Context/SeedData.cs)
// DbContext 'scoped' bir servistir: her HTTP isteği için bir tane oluşur. Uygulama açılırken
// ortada istek olmadığından CreateScope ile geçici bir kapsam açıp context'i oradan alıyoruz.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
    try
    {
        // Migrate(): Migrations klasöründe olup veritabanına henüz uygulanmamış migration'ları uygular
        // ('dotnet ef database update' ile aynı iş). Veritabanı hiç yoksa oluşturur.
        context.Database.Migrate();
        SeedData.Seed(context);
    }
    catch (Exception ex)
    {
        // Bağlantı hatasında API yine açılır; sorun loglanır
        app.Logger.LogError(ex, "Veritabanı hazırlanamadı. appsettings.json bağlantı cümlesini kontrol edin.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
