using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

public class RezervasyonController(RezervasyonDeposu rezervasyonlar) : OturumluController
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yap(int menuId, int yemekhaneId, string baslangic)
    {
        try
        {
            var sonuc = await rezervasyonlar.YapAsync(Oturum.Id, menuId);
            TempData["Basari"] = $"Rezervasyon yapıldı. Ücret {sonuc.Ucret:C}, kalan bakiye {sonuc.Bakiye:C}.";
        }
        catch (MySqlException hata) when (hata.Number == 1644)
        {
            TempData["Hata"] = hata.Message;
        }
        return RedirectToAction("Haftalik", "Menu", new { yemekhaneId, baslangic });
    }
}