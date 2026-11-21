using Microsoft.AspNetCore.Mvc;
using Yemekhane.Models;
using Yemekhane.Servisler;
using Yemekhane.Veri;

namespace Yemekhane.Controllers;

public class HesapController(KullaniciDeposu kullanicilar, OturumServisi oturumlar) : Controller
{
    [HttpGet]
    public IActionResult Giris() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kullanici = await kullanicilar.EpostaIleBulAsync(model.Eposta.Trim());
        if (kullanici is null || !BCrypt.Net.BCrypt.Verify(model.Sifre, kullanici.SifreHash))
        {
            ViewBag.Hata = "E-posta ya da şifre hatalı.";
            return View(model);
        }
        if (!kullanici.Aktif)
        {
            ViewBag.Hata = "Hesabınız pasif durumda.";
            return View(model);
        }

        var token = await oturumlar.AcAsync(new OturumBilgisi(kullanici.Id, kullanici.Ad, kullanici.Rol));
        Response.Cookies.Append("oturum", token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
        return RedirectToAction("Gunluk", "Menu");  
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cikis()
    {
        if (Request.Cookies.TryGetValue("oturum", out var token)) await oturumlar.KapatAsync(token);
        Response.Cookies.Delete("oturum");
        return RedirectToAction("Giris");
    }
}