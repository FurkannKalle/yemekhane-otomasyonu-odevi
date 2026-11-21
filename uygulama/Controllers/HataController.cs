using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Yemekhane.Models;

namespace Yemekhane.Controllers;

public class HataController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}