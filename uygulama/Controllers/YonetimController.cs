using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Yemekhane.Models;
using Yemekhane.Servisler;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

public class YonetimController(YonetimDeposu depo, YonetimServisi servis, MenuDeposu menuler) : OturumluController
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);   
        if (context.Result is null && Oturum.Rol != "yonetici")
        {
            TempData["Hata"] = "Bu sayfa yalnızca yöneticiler içindir.";
            context.Result = RedirectToAction("Gunluk", "Menu");
        }
    }

    public async Task<IActionResult> Index(DateOnly? baslangic, int? yemekhaneId, int? duzenle)
    {
        var ilkGun = baslangic ?? DateOnly.FromDateTime(DateTime.Now);
        var form = duzenle is int id ? await servis.FormAsync(id) : null;
        return View(new YonetimSayfasi
        {
            Yemekhaneler = await menuler.YemekhanelerAsync(),
            Ogunler = await menuler.OgunlerAsync(),
            Yemekler = await depo.YemeklerAsync(),
            Form = form ?? new MenuFormu { Tarih = ilkGun.ToString("yyyy-MM-dd") },
            Baslangic = ilkGun,
            YemekhaneId = yemekhaneId,
            Rapor = await depo.RaporAsync(Oturum, ilkGun, yemekhaneId),
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(MenuFormu form)
    {
        var hata = await servis.KaydetAsync(form);
        if (hata is null) TempData["Basari"] = form.Id is null ? "Menü oluşturuldu." : "Menü güncellendi.";
        else TempData["Hata"] = hata;
        return RedirectToAction("Index", new { baslangic = form.Tarih, duzenle = hata is null ? null : form.Id });
    }
}