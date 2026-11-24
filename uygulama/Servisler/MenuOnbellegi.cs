using System.Text.Encodings.Web;
using System.Text.Json;
using StackExchange.Redis;
using Yemekhane.Models;
using Yemekhane.Veri;

namespace Yemekhane.Servisler;

public class MenuOnbellegi(IConnectionMultiplexer redis, MenuDeposu menuler, ILogger<MenuOnbellegi> gunluk)
{
    static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    readonly IDatabase redisDb = redis.GetDatabase();

    static string Anahtar(int yemekhaneId, DateOnly tarih, int ogunId) =>
        $"menu:{yemekhaneId}:{tarih:yyyy-MM-dd}:{ogunId}";

    static TimeSpan GunSonunaKadar(DateOnly tarih)
    {
        var gunSonu = tarih.ToDateTime(TimeOnly.MinValue).AddDays(1);
        var bugunSonu = DateTime.Today.AddDays(1);
        return (gunSonu > bugunSonu ? gunSonu : bugunSonu) - DateTime.Now;
    }

    public async Task<List<MenuDetay>> DetayAsync(int yemekhaneId, DateOnly tarih, int ogunId)
    {
        var anahtar = Anahtar(yemekhaneId, tarih, ogunId);

        var kayit = await redisDb.StringGetAsync(anahtar);
        if (kayit.HasValue)
        {
            gunluk.LogInformation("Menü önbelleği İSABET {Anahtar} (Redis'ten okundu)", anahtar);
            return JsonSerializer.Deserialize<List<MenuDetay>>(kayit.ToString(), Json)!;
        }

        gunluk.LogInformation("Menü önbelleği KAYIP {Anahtar} (veritabanından okundu)", anahtar);
        return await DoldurAsync(anahtar, yemekhaneId, tarih, ogunId, kaynaktan: false);
    }

    public async Task YenileAsync(int yemekhaneId, DateOnly tarih, int ogunId)
    {
        var anahtar = Anahtar(yemekhaneId, tarih, ogunId);
        await redisDb.KeyDeleteAsync(anahtar);
        gunluk.LogInformation("Menü önbelleği SİLİNDİ {Anahtar} (yönetici menüyü değiştirdi)", anahtar);

        var detay = await DoldurAsync(anahtar, yemekhaneId, tarih, ogunId, kaynaktan: true);
        if (detay.Count > 0)
            gunluk.LogInformation("Menü önbelleği YENİLENDİ {Anahtar} (kaynaktan okundu)", anahtar);
    }

    async Task<List<MenuDetay>> DoldurAsync(string anahtar, int yemekhaneId, DateOnly tarih, int ogunId, bool kaynaktan)
    {
        var detay = await menuler.DetayAsync(yemekhaneId, tarih, ogunId, kaynaktan);
        if (detay.Count > 0)
            await redisDb.StringSetAsync(anahtar, JsonSerializer.Serialize(detay, Json), GunSonunaKadar(tarih));
        return detay;
    }
}