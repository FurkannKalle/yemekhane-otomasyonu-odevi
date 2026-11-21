using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;
using Yemekhane.Servisler;

namespace Yemekhane.Veri;

public class KullaniciDeposu(OkumaDb okuma, VeriKaynagi kaynak)
{
    public Task<Kullanici?> EpostaIleBulAsync(string eposta) =>
        okuma.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(k => k.Eposta == eposta);

    public Task<decimal> BakiyeAsync(OturumBilgisi oturum) =>
        kaynak.Okuma(oturum).Kullanicilar
            .Where(k => k.Id == oturum.Id)
            .Select(k => k.Bakiye)
            .FirstAsync();
}