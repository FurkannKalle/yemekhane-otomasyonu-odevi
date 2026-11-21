using System.Security.Cryptography;
using System.Text.Json;
using StackExchange.Redis;

namespace Yemekhane.Servisler;

public record OturumBilgisi(int Id, string Ad, string Rol, DateTime? SonYazma = null);

public class OturumServisi(IConnectionMultiplexer redis)
{
    static readonly TimeSpan Sure = TimeSpan.FromMinutes(30);
    IDatabase Db => redis.GetDatabase();
    static string Anahtar(string token) => $"oturum:{token}";

    public async Task<string> AcAsync(OturumBilgisi bilgi)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        await Db.StringSetAsync(Anahtar(token), JsonSerializer.Serialize(bilgi), Sure);
        return token;
    }

    public async Task<OturumBilgisi?> DogrulaAsync(string token, bool yazmaIstegi)
    {
        var deger = await Db.StringGetSetExpiryAsync(Anahtar(token), Sure);
        if (deger.IsNull) return null;

        var oturum = JsonSerializer.Deserialize<OturumBilgisi>(deger.ToString())!;
        if (yazmaIstegi)
        {
            oturum = oturum with { SonYazma = DateTime.UtcNow };
            await Db.StringSetAsync(Anahtar(token), JsonSerializer.Serialize(oturum), Sure);
        }
        return oturum;
    }

    public Task KapatAsync(string token) => Db.KeyDeleteAsync(Anahtar(token));
}