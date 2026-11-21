using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Yemekhane.Servisler;

namespace Yemekhane.Controllers;

public abstract class OturumluController : Controller
{
    protected OturumBilgisi Oturum => (OturumBilgisi)HttpContext.Items["oturum"]!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Items["oturum"] is null)
            context.Result = RedirectToAction("Giris", "Hesap");
    }
}