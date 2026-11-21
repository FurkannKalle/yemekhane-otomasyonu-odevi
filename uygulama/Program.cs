using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Yemekhane.Servisler;
using Yemekhane.Veri;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var ayar = builder.Configuration;
string Baglanti(string? sunucu) =>
    $"Server={sunucu};Port={ayar["DB_PORT"]};Database={ayar["DB_ADI"]};" +
    $"User ID={ayar["DB_KULLANICI"]};Password={ayar["DB_SIFRE"]};CharSet=utf8mb4";
var mysql = new MySqlServerVersion(new Version(8, 4, 0));
builder.Services.AddDbContext<YazmaDb>(o => o.UseMySql(Baglanti(ayar["DB_YAZMA_HOST"]), mysql));
builder.Services.AddDbContext<OkumaDb>(o => o.UseMySql(Baglanti(ayar["DB_OKUMA_HOST"]), mysql));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect($"{ayar["REDIS_HOST"]}:{ayar["REDIS_PORT"]},abortConnect=false"));


builder.Services.AddSingleton<OturumServisi>();
builder.Services.AddScoped<KullaniciDeposu>();
builder.Services.AddScoped<MenuDeposu>();
builder.Services.AddScoped<MenuServisi>();
builder.Services.AddScoped<RezervasyonDeposu>();

var app = builder.Build();

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("tr-TR");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Hata");
}


app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("oturum", out var token))
    {
        var oturumlar = context.RequestServices.GetRequiredService<OturumServisi>();
        var oturum = await oturumlar.DogrulaAsync(token);
        if (oturum is not null) context.Items["oturum"] = oturum;
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Menu}/{action=Gunluk}/{id?}");

app.MapGet("/saglik", () => "ok");

app.Run();