using Microsoft.AspNetCore.Mvc;

namespace Yemekhane.Controllers;

public class HomeController : OturumluController
{
    public IActionResult Index() => View(Oturum);
}