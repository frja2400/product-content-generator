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

        ViewBag.SampleCount = _sessionStore.GetSampleCount();
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

    public class RunSampleAgainRequest
    {
        public string Prompt { get; set; } = "";
        public int SampleCount { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> RunSampleAgain([FromBody] RunSampleAgainRequest request)
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return Json(new { success = false, error = "No products in session" });

        _sessionStore.SavePrompt(request.Prompt);
        _sessionStore.SaveSampleCount(request.SampleCount);

        // Rensa gamla genererade beskrivningar
        foreach (var product in products)
        {
            product.PreviousGeneratedDescription = product.GeneratedDescription;
            product.GeneratedDescription = null;
            product.GenerationFailed = false;
        }

        _sessionStore.SaveProducts(products);

        var selectedVariantIds = _sessionStore.GetSelectedProducts();

        var eligibleProducts = products
            .Where(p => selectedVariantIds.Contains(p.VariantId ?? "") && p.DataQuality != ProductContentGenerator.Models.DataQuality.Insufficient)
            .ToList();

        var sampleProducts = eligibleProducts
            .Take(Math.Min(request.SampleCount, eligibleProducts.Count))
            .ToList();

        if (sampleProducts.Count == 0)
            return Json(new { success = false, error = "No eligible products found" });

        var results = new List<object>();

        foreach (var product in sampleProducts)
        {
            var result = await _claudeService.GenerateDescriptionAsync(product, request.Prompt);

            var productInSession = products.First(p => p.VariantId == product.VariantId);
            productInSession.GeneratedDescription = result.Success
                ? result.GeneratedDescription
                : product.LongDescription;
            productInSession.GenerationFailed = !result.Success;

            results.Add(new
            {
                variantId = product.VariantId,
                displayName = product.DisplayName,
                generatedDescription = productInSession.GeneratedDescription,
                generationFailed = productInSession.GenerationFailed,
                previousGeneratedDescription = productInSession.PreviousGeneratedDescription,
                dataQuality = productInSession.DataQuality.ToString()
            });
        }

        _sessionStore.SaveProducts(products);

        return Json(new { success = true, results });
    }
}