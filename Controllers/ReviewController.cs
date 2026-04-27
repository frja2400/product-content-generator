using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

public class ReviewController : Controller
{
    private readonly SessionStore _sessionStore;
    private readonly ClaudeService _claudeService;

    public ReviewController(SessionStore sessionStore, ClaudeService claudeService)
    {
        _sessionStore = sessionStore;
        _claudeService = claudeService;
    }

    public IActionResult Index()
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        var selectedVariantIds = _sessionStore.GetSelectedProducts();

        ViewBag.Prompt = _sessionStore.GetPrompt();
        ViewBag.EligibleCount = products.Count(p =>
            selectedVariantIds.Contains(p.VariantId ?? "") &&
            p.DataQuality != ProductContentGenerator.Models.DataQuality.Insufficient);

        return View(products);
    }

    [HttpPost]
    public async Task<IActionResult> RetryGeneration(string variantId)
    {
        var products = _sessionStore.GetProducts();
        var product = products.FirstOrDefault(p => p.VariantId == variantId);

        if (product == null)
            return NotFound();

        var prompt = _sessionStore.GetPrompt();
        var result = await _claudeService.GenerateDescriptionAsync(product, prompt);

        var productInSession = products.First(p => p.VariantId == product.VariantId);
        productInSession.GeneratedDescription = result.Success
            ? result.GeneratedDescription
            : product.LongDescription;
        productInSession.GenerationFailed = !result.Success;

        _sessionStore.SaveProducts(products);

        return Json(new
        {
            success = result.Success,
            generatedDescription = productInSession.GeneratedDescription,
            generationFailed = productInSession.GenerationFailed
        });
    }
}