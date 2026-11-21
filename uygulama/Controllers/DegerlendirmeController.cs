using Microsoft.AspNetCore.Mvc;
using Yemekhane.Servisler;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

public class DegerlendirmeController(DegerlendirmeDeposu degerlendirmeler, DegerlendirmeServisi servis) : OturumluController
{
    public async Task<IActionResult> Index() => View(await degerlendirmeler.ListeAsync(Oturum));
  
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(int menuId, int puan, string? yorum)
    {
        var hata = await servis.EkleAsync(Oturum.Id, menuId, puan, yorum);
        if (hata is null) TempData["Basari"] = "Değerlendirmeniz kaydedildi. Teşekkürler!";
        else TempData["Hata"] = hata;
        return RedirectToAction("Index");
    }
}