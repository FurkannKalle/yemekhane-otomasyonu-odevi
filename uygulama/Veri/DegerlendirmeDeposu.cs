using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;
using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class DegerlendirmeDeposu(YazmaDb yazma, VeriKaynagi kaynak)
{
    public Task<List<DegerlendirmeSatiri>> ListeAsync(OturumBilgisi oturum)
    {
        var db = kaynak.Okuma(oturum);
        return (from r in db.Rezervasyonlar
                join d in db.MenuDoluluklari on r.MenuId equals d.MenuId
                join g in db.Degerlendirmeler
                    on new { r.KullaniciId, r.MenuId } equals new { g.KullaniciId, g.MenuId } into oylar
                from g in oylar.DefaultIfEmpty()
                where r.KullaniciId == oturum.Id && r.Durum == "kullanildi"
                orderby d.Tarih descending, d.OgunId descending
                select new DegerlendirmeSatiri
                {
                    MenuId = r.MenuId,
                    Tarih = d.Tarih,
                    Ogun = d.Ogun,
                    Yemekhane = d.Yemekhane,
                    Puan = g != null ? g.Puan : null,
                    Yorum = g != null ? g.Yorum : null,
                }).ToListAsync();
    }

    public async Task<(bool Kullanildi, bool OyVar)> DurumAsync(int kullaniciId, int menuId)
    {
        var kullanildi = await yazma.Rezervasyonlar.AnyAsync(r =>
            r.KullaniciId == kullaniciId && r.MenuId == menuId && r.Durum == "kullanildi");
        var oyVar = await yazma.Degerlendirmeler.AnyAsync(g =>
            g.KullaniciId == kullaniciId && g.MenuId == menuId);
        return (kullanildi, oyVar);
    }

    public async Task EkleAsync(Degerlendirme degerlendirme)
    {
        yazma.Degerlendirmeler.Add(degerlendirme);
        await yazma.SaveChangesAsync();
    }
}