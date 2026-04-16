using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;
using ProductContentGenerator.Models;

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

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        return View(products);
    }

    // Returnerar detaljvy för en produkt via AJAX
    [HttpGet]
    public IActionResult Detail(string variantId)
    {
        var products = _sessionStore.GetProducts();
        var product = products.FirstOrDefault(p => p.VariantId == variantId);

        if (product == null)
            return NotFound();

        return PartialView("_ProductDetail", product);
    }
}