using Yemekhane.Models;
using Yemekhane.Veri;

namespace Yemekhane.Servisler;

public class MenuServisi(MenuDeposu menuler, MenuOnbellegi onbellek, AlerjenDeposu alerjenler)
{
    public static decimal FiyatSec(string rol, decimal ogrenciFiyati, decimal personelFiyati) =>
        rol == "ogrenci" ? ogrenciFiyati : personelFiyati;

    static decimal? YarimFiyat(decimal fiyat, decimal? oran) =>
        oran is decimal o ? Math.Round(fiyat * o, 2, MidpointRounding.AwayFromZero) : null;

    public async Task<GunlukMenu?> GunlukAsync(OturumBilgisi oturum, int yemekhaneId, DateOnly tarih, int ogunId)
    {
        var yemekler = await onbellek.DetayAsync(yemekhaneId, tarih, ogunId);
        if (yemekler.Count == 0) return null;

        var ilk = yemekler[0];
        var doluluk = (await menuler.DolulukAsync(ilk.MenuId))!;
        var alerjenlerim = await alerjenler.KullanicininAsync(oturum);
        var fiyat = FiyatSec(oturum.Rol, ilk.OgrenciFiyati, ilk.PersonelFiyati);
        return new GunlukMenu
        {
            Yemekhane = ilk.Yemekhane,
            Ogun = ilk.Ogun,
            Tarih = ilk.Tarih,
            Fiyat = fiyat,
            YarimFiyat = YarimFiyat(fiyat, ilk.YarimPorsiyonOrani),
            ToplamKalori = yemekler.Sum(y => y.Kalori),
            Kapasite = doluluk.Kapasite,
            BosYer = doluluk.BosYer,
            RezervasyonSon = ilk.RezervasyonSon,
            RezervasyonAcik = doluluk.RezervasyonAcik,
            Yemekler = yemekler,
            AlerjenUyarilari = AlerjenUyarilari(yemekler, alerjenlerim),
        };
    }

    static Dictionary<string, List<string>> AlerjenUyarilari(List<MenuDetay> yemekler, List<Alerjen> alerjenlerim) =>
        yemekler
            .Select(y => (y.Yemek, Eslesen: alerjenlerim
                .Where(a => (y.AlerjenIdler ?? "").Split(',').Contains(a.Id.ToString()))
                .Select(a => a.Ad)
                .ToList()))
            .Where(x => x.Eslesen.Count > 0)
            .ToDictionary(x => x.Yemek, x => x.Eslesen);

    public async Task<List<HaftalikMenu>> HaftalikAsync(OturumBilgisi oturum, int yemekhaneId, DateOnly baslangic)
    {
        var liste = await menuler.HaftalikAsync(oturum, yemekhaneId, baslangic);
        foreach (var m in liste)
        {
            m.Fiyat = FiyatSec(oturum.Rol, m.OgrenciFiyati, m.PersonelFiyati);
            m.YarimFiyat = YarimFiyat(m.Fiyat, m.YarimPorsiyonOrani);
            m.Durum = DurumBelirle(m);
        }
        return liste;
    }

    static string DurumBelirle(HaftalikMenu m) => m.RezervasyonDurumu switch
    {
        "aktif" => "rezerve",
        "kullanildi" => "kullanildi",
        _ when !m.RezervasyonAcik => "suresi_doldu",
        _ when m.BosYer <= 0 => "dolu",
        _ => "acik",
    };
}
