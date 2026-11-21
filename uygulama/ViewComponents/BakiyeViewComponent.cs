using Microsoft.AspNetCore.Mvc;
using Yemekhane.Servisler;
using Yemekhane.Veri;

namespace Yemekhane.ViewComponents;

public class BakiyeViewComponent(KullaniciDeposu kullanicilar) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(OturumBilgisi oturum) =>
        Content((await kullanicilar.BakiyeAsync(oturum)).ToString("C"));
}