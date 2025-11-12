using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

public class ReservationsController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}