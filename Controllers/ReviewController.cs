using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;

namespace ProductContentGenerator.Controllers;

public class ReviewController : Controller
{
    private readonly SessionStore _sessionStore;

    public ReviewController(SessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public IActionResult Index()
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        ViewBag.Prompt = _sessionStore.GetPrompt();

        return View(products);
    }
}