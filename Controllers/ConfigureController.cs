using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;
using ProductContentGenerator.Models;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

public class ConfigureController : Controller
{
    private readonly SessionStore _sessionStore;
    private readonly ClaudeService _claudeService;

    private const string DefaultPrompt = "Du är en erfaren copywriter som skriver produktbeskrivningar för ett svenskt apotek. Skriv en SEO-optimerad produktbeskrivning på svenska baserad på produktdatan. Beskrivningen ska vara 150-200 ord, ha en engagerande inledning, innehålla relevanta nyckelord.";

    public ConfigureController(SessionStore sessionStore, ClaudeService claudeService)
    {
        _sessionStore = sessionStore;
        _claudeService = claudeService;
    }

    public IActionResult Index()
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        // Hämta sparad prompt eller använd default
        ViewBag.Prompt = string.IsNullOrEmpty(_sessionStore.GetPrompt())
            ? DefaultPrompt
            : _sessionStore.GetPrompt();

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

    [HttpPost]
    public async Task<IActionResult> RunSample(string prompt, int sampleCount)
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        // Spara prompten i sessionen
        _sessionStore.SavePrompt(prompt);

        // Använd bara produkter med tillräcklig data
        var eligibleProducts = products
            .Where(p => p.DataQuality != DataQuality.Insufficient)
            .ToList();

        // Ta max sampleCount produkter, men aldrig fler än vad som finns
        var sampleProducts = eligibleProducts
            .Take(Math.Min(sampleCount, eligibleProducts.Count))
            .ToList();

        if (sampleProducts.Count == 0)
        {
            TempData["Error"] = "No products with sufficient data found.";
            return RedirectToAction("Index");
        }

        // Generera beskrivningar för sample-produkterna
        foreach (var product in sampleProducts)
        {
            var result = await _claudeService.GenerateDescriptionAsync(product, prompt);

            var productInSession = products.First(p => p.VariantId == product.VariantId);
            productInSession.GeneratedDescription = result.Success
                ? result.GeneratedDescription
                : product.LongDescription;
            productInSession.GenerationFailed = !result.Success;
        }

        // Spara uppdaterad produktlista i sessionen
        _sessionStore.SaveProducts(products);

        return RedirectToAction("Index", "Review");
    }
}