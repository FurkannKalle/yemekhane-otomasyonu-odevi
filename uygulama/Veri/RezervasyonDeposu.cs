using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;
using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class RezervasyonDeposu(YazmaDb yazma, VeriKaynagi kaynak)
{
    public async Task<RezervasyonSonucu> YapAsync(int kullaniciId, int menuId, string porsiyon)
    {
        var sonuc = await yazma.Database
            .SqlQuery<RezervasyonSonucu>($"CALL sp_rezervasyon_yap({kullaniciId}, {menuId}, {porsiyon})")
            .ToListAsync();
        return sonuc[0];
    }

    public async Task<IptalSonucu> IptalAsync(int kullaniciId, int rezervasyonId)
    {
        var sonuc = await yazma.Database
            .SqlQuery<IptalSonucu>($"CALL sp_rezervasyon_iptal({kullaniciId}, {rezervasyonId})")
            .ToListAsync();
        return sonuc[0];
    }

    public Task<List<RezervasyonSatiri>> ListeleAsync(OturumBilgisi oturum)
    {
        var db = kaynak.Okuma(oturum);
        return (from r in db.Rezervasyonlar
                join d in db.MenuDoluluklari on r.MenuId equals d.MenuId
                where r.KullaniciId == oturum.Id
                orderby d.Tarih descending, d.OgunId descending
                select new RezervasyonSatiri
                {
                    Id = r.Id,
                    Durum = r.Durum,
                    Porsiyon = r.Porsiyon,
                    Ucret = r.Ucret,
                    Tarih = d.Tarih,
                    Ogun = d.Ogun,
                    Yemekhane = d.Yemekhane,
                    RezervasyonSon = d.RezervasyonSon,
                    KullanmaZamani = r.KullanmaZamani,
                    IptalZamani = r.IptalZamani,
                    IptalEdilebilir = r.Durum == "aktif" && DateTime.Now < d.RezervasyonSon,
                }).ToListAsync();
    }
}
