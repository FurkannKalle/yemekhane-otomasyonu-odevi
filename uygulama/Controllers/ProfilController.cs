using Microsoft.AspNetCore.Mvc;
using Yemekhane.Models;
using Yemekhane.Servisler;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

// Aşama 4: profil sayfası, kullanıcı alerjenlerini seçer
public class ProfilController(AlerjenDeposu alerjenler, ProfilServisi servis) : OturumluController
{
    public async Task<IActionResult> Index() =>
        View(new ProfilSayfasi
        {
            Alerjenler = await alerjenler.TumuAsync(),
            Secili = (await alerjenler.KullanicininAsync(Oturum)).Select(a => a.Id).ToHashSet(),
        });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(List<int>? alerjenIdler)
    {
        var hata = await servis.KaydetAsync(Oturum.Id, alerjenIdler ?? []);
        if (hata is null) TempData["Basari"] = "Alerjenleriniz kaydedildi.";
        else TempData["Hata"] = hata;
        return RedirectToAction("Index");
    }
}
