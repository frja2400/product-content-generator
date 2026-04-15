using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;

namespace ProductContentGenerator.Controllers;

public class ConfigureController : Controller
{
    private readonly SessionStore _sessionStore;

    public ConfigureController(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public IActionResult Index()
    {
        var products = _sessionStore.GetProducts();
        return View(products);
    }
}