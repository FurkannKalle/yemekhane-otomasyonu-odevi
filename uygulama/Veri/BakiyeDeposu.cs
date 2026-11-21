using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;
using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class BakiyeDeposu(YazmaDb yazma, VeriKaynagi kaynak)
{
    public async Task<YuklemeSonucu> YukleAsync(int kullaniciId, decimal tutar)
    {
        var sonuc = await yazma.Database
            .SqlQuery<YuklemeSonucu>($"CALL sp_bakiye_yukle({kullaniciId}, {tutar})")
            .ToListAsync();
        return sonuc[0];   
    }
    public Task<List<BakiyeHareketi>> HareketlerAsync(OturumBilgisi oturum) =>
        kaynak.Okuma(oturum).BakiyeHareketleri.AsNoTracking()
            .Where(h => h.KullaniciId == oturum.Id)
            .OrderByDescending(h => h.Olusturma).ThenByDescending(h => h.Id)
            .Take(100)
            .ToListAsync();
}