using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

public class RezervasyonController(RezervasyonDeposu rezervasyonlar) : OturumluController
{
    public async Task<IActionResult> Index(string? durum = null)
    {
        var liste = await rezervasyonlar.ListeleAsync(Oturum);
        ViewBag.Durum = durum;
        return View(durum is null ? liste : liste.Where(r => r.Durum == durum).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yap(int menuId, int yemekhaneId, string baslangic, string porsiyon = "tam")
    {
        try
        {
            var sonuc = await rezervasyonlar.YapAsync(Oturum.Id, menuId, porsiyon);
            var porsiyonAdi = porsiyon == "yarim" ? "yarım porsiyon" : "tam porsiyon";
            TempData["Basari"] = $"Rezervasyon yapıldı ({porsiyonAdi}). Ücret {sonuc.Ucret:C}, kalan bakiye {sonuc.Bakiye:C}.";
        }
        catch (MySqlException hata) when (hata.Number == 1644)
        {
            TempData["Hata"] = hata.Message;
        }
        return RedirectToAction("Haftalik", "Menu", new { yemekhaneId, baslangic });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        try
        {
            var sonuc = await rezervasyonlar.IptalAsync(Oturum.Id, id);
            TempData["Basari"] = $"Rezervasyon iptal edildi, {sonuc.Iade:C} iade edildi. Yeni bakiye {sonuc.Bakiye:C}.";
        }
        catch (MySqlException hata) when (hata.Number == 1644)
        {
            TempData["Hata"] = hata.Message;
        }
        return RedirectToAction("Index");
    }
}
