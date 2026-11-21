using Microsoft.EntityFrameworkCore;
using Yemekhane.Models;

namespace Yemekhane.Veri;

// kullanicilar tablosuna erişim
public class KullaniciDeposu(OkumaDb okuma)
{
    // Giriş bir okuma işlemidir: replikadan
    public Task<Kullanici?> EpostaIleBulAsync(string eposta) =>
        okuma.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(k => k.Eposta == eposta);
}