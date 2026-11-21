using Yemekhane.Models;
using Yemekhane.Veri;

namespace Yemekhane.Servisler;

public class MenuServisi(MenuDeposu menuler)
{
    public static decimal FiyatSec(string rol, decimal ogrenciFiyati, decimal personelFiyati) =>
        rol == "ogrenci" ? ogrenciFiyati : personelFiyati;

    public async Task<GunlukMenu?> GunlukAsync(OturumBilgisi oturum, int yemekhaneId, DateOnly tarih, int ogunId)
    {
        var yemekler = await menuler.DetayAsync(yemekhaneId, tarih, ogunId);
        if (yemekler.Count == 0) return null;   // bu seçim için menü yok

        var ilk = yemekler[0];   // menü bilgileri her satırda aynı
        var doluluk = (await menuler.DolulukAsync(ilk.MenuId))!;
        return new GunlukMenu
        {
            Yemekhane = ilk.Yemekhane,
            Ogun = ilk.Ogun,
            Tarih = ilk.Tarih,
            Fiyat = FiyatSec(oturum.Rol, ilk.OgrenciFiyati, ilk.PersonelFiyati),
            ToplamKalori = yemekler.Sum(y => y.Kalori),
            Kapasite = doluluk.Kapasite,
            BosYer = doluluk.BosYer,
            RezervasyonSon = ilk.RezervasyonSon,
            RezervasyonAcik = doluluk.RezervasyonAcik,
            Yemekler = yemekler,
        };
    }

    public async Task<List<HaftalikMenu>> HaftalikAsync(OturumBilgisi oturum, int yemekhaneId, DateOnly baslangic)
    {
        var liste = await menuler.HaftalikAsync(oturum, yemekhaneId, baslangic);
        foreach (var m in liste)
        {
            m.Fiyat = FiyatSec(oturum.Rol, m.OgrenciFiyati, m.PersonelFiyati);
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