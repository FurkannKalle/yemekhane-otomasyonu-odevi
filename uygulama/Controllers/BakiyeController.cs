using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Yemekhane.Models;
using Yemekhane.Veri;
using Yemekhane.Servisler;

namespace Yemekhane.Controllers;

public class BakiyeController(KullaniciDeposu kullanicilar, BakiyeDeposu bakiyeler) : OturumluController
{
    public async Task<IActionResult> Index() =>
        View(new BakiyeSayfasi
        {
            Bakiye = await kullanicilar.BakiyeAsync(Oturum),
            Hareketler = await bakiyeler.HareketlerAsync(Oturum),
        });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yukle(string? tutar)
    {
        var miktar = TutarCozucu.Coz(tutar) ?? 0;
        try
        {
            var sonuc = await bakiyeler.YukleAsync(Oturum.Id, miktar);
            TempData["Basari"] = $"{miktar:C} yüklendi. Yeni bakiye {sonuc.Bakiye:C}.";
        }
        catch (MySqlException hata) when (hata.Number == 1644)
        {
            TempData["Hata"] = hata.Message;   
        }
        return RedirectToAction("Index");
    }
}