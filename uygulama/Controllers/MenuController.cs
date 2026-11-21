using Microsoft.AspNetCore.Mvc;
using Yemekhane.Models;
using Yemekhane.Servisler;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

public class MenuController(MenuDeposu menuler, MenuServisi menuServisi) : OturumluController
{
    public async Task<IActionResult> Gunluk(int yemekhaneId = 1, DateOnly? tarih = null, int ogunId = 2)
    {
        var gun = tarih ?? DateOnly.FromDateTime(DateTime.Now);
        var sayfa = new GunlukMenuSayfasi
        {
            Yemekhaneler = await menuler.YemekhanelerAsync(),
            Ogunler = await menuler.OgunlerAsync(),
            YemekhaneId = yemekhaneId,
            Tarih = gun,
            OgunId = ogunId,
            Menu = await menuServisi.GunlukAsync(Oturum, yemekhaneId, gun, ogunId),
        };
        return View(sayfa);
    }
}