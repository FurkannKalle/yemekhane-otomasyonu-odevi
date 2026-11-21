using System.Globalization;
using Microsoft.EntityFrameworkCore;
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

var app = builder.Build();

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("tr-TR");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/saglik", () => "ok");

app.MapGet("/db-test", async (YazmaDb yazma, OkumaDb okuma) =>
    $"Kaynak: {await yazma.Kullanicilar.CountAsync()} kullanıcı · " +
    $"Replika: {await okuma.Kullanicilar.CountAsync()} kullanıcı · " +
    $"Mehmet'in bakiyesi (replika): {(await okuma.Kullanicilar.FirstAsync(k => k.Id == 7)).Bakiye:C}");

app.Run();