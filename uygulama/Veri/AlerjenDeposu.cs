using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;
using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class AlerjenDeposu(OkumaDb okuma, YazmaDb yazma, VeriKaynagi kaynak)
{
    public Task<List<Alerjen>> TumuAsync() =>
        okuma.Alerjenler.AsNoTracking().OrderBy(a => a.Ad).ToListAsync();

    public Task<List<Alerjen>> KullanicininAsync(OturumBilgisi oturum)
    {
        var db = kaynak.Okuma(oturum);
        return (from ka in db.KullaniciAlerjenleri
                join a in db.Alerjenler on ka.AlerjenId equals a.Id
                where ka.KullaniciId == oturum.Id
                orderby a.Ad
                select a).AsNoTracking().ToListAsync();
    }

    public async Task KaydetAsync(int kullaniciId, List<int> alerjenIdler)
    {
        await using var islem = await yazma.Database.BeginTransactionAsync();
        await yazma.KullaniciAlerjenleri.Where(k => k.KullaniciId == kullaniciId).ExecuteDeleteAsync();
        yazma.KullaniciAlerjenleri.AddRange(
            alerjenIdler.Select(id => new KullaniciAlerjeni { KullaniciId = kullaniciId, AlerjenId = id }));
        await yazma.SaveChangesAsync();
        await islem.CommitAsync();
    }
}
