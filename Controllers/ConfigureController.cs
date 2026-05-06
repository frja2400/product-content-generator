using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;
using ProductContentGenerator.Models;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

public class ConfigureController : Controller
{
    private readonly SessionStore _sessionStore;
    private readonly ClaudeService _claudeService;
    private readonly BatchJobQueue _batchJobQueue;

    private const string DefaultPrompt = """
    Du är en erfaren copywriter som skriver produktbeskrivningar för MEDS Apotek, ett svenskt e-handelsapotek. Skriv en SEO-optimerad produktbeskrivning på svenska baserad på den givna produktdatan.

    Följ denna struktur:
    1. Öppning (2–3 meningar): Presentera vad produkten är och dess huvudfördelar – fokusera på vad produkten används till och varför kunden behöver den.
    2. Beskrivning (3–5 meningar): Översätt produktens funktioner till konkreta fördelar för användaren. Bygg vidare på det som inte redan nämnts i öppningen.
    3. Avsluta med en punktlista med tre korta highlights.

    Formateringsregler:
    - Inga rubriker, bara löptext och punktlista.
    - Om beskrivningen överstiger ~50 ord, dela upp texten i stycken vid naturliga brytpunkter. Varje stycke ska vara 1–5 meningar långt.
    - Punktlistan ska formateras med • som prefix för varje punkt, exempelvis: • Highlight här
    - Håll en varm, professionell och trovärdig ton som passar ett apotek.
    - Returnera enbart produktbeskrivningen, ingen inledande eller avslutande kommentar.
    - Inkludera inte användningsinstruktioner eller dosering i texten – det hanteras i ett separat fält.
    """;
    public ConfigureController(SessionStore sessionStore, ClaudeService claudeService, BatchJobQueue batchJobQueue)
    {
        _sessionStore = sessionStore;
        _claudeService = claudeService;
        _batchJobQueue = batchJobQueue;
    }

    public IActionResult Index()
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        ViewBag.Prompt = string.IsNullOrEmpty(_sessionStore.GetPrompt())
            ? DefaultPrompt
            : _sessionStore.GetPrompt();

        ViewBag.SampleCount = _sessionStore.GetSampleCount();

        return View(products);
    }

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
    public async Task<IActionResult> RunSample(string prompt, int sampleCount, List<string> selectedVariantIds)
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        _sessionStore.SavePrompt(prompt);
        _sessionStore.SaveSelectedProducts(selectedVariantIds);
        _sessionStore.SaveSampleCount(sampleCount);

        // Rensa gamla genererade beskrivningar innan ny körning
        foreach (var product in products)
        {
            product.GeneratedDescription = null;
            product.GenerationFailed = false;
            product.PreviousGeneratedDescription = null;
        }

        var eligibleProducts = products
            .Where(p => selectedVariantIds.Contains(p.VariantId ?? "") && p.DataQuality != DataQuality.Insufficient)
            .ToList();

        var sampleProducts = eligibleProducts
            .Take(Math.Min(sampleCount, eligibleProducts.Count))
            .ToList();

        if (sampleProducts.Count == 0)
        {
            TempData["Error"] = "No products with sufficient data found.";
            return RedirectToAction("Index");
        }

        foreach (var product in sampleProducts)
        {
            var result = await _claudeService.GenerateDescriptionAsync(product, prompt);

            var productInSession = products.First(p => p.VariantId == product.VariantId);
            productInSession.GeneratedDescription = result.Success
                ? result.GeneratedDescription
                : product.LongDescription;
            productInSession.GenerationFailed = !result.Success;
        }

        _sessionStore.SaveProducts(products);

        return RedirectToAction("Index", "Review");
    }

    [HttpPost]
    public IActionResult RunAll(string prompt)
    {
        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        _sessionStore.SavePrompt(prompt);

        var selectedVariantIds = _sessionStore.GetSelectedProducts();

        var eligibleProducts = products
            .Where(p => selectedVariantIds.Contains(p.VariantId ?? "") &&
                p.DataQuality != DataQuality.Insufficient &&
                string.IsNullOrWhiteSpace(p.GeneratedDescription))
            .ToList();

        if (eligibleProducts.Count == 0)
        {
            // Alla produkter är redan genererade, rensa jobbet och gå direkt till export
            _batchJobQueue.Clear();
            return RedirectToAction("Index", "Export");
        }

        var job = new BatchJob
        {
            Products = eligibleProducts,
            AllProducts = products,
            Prompt = prompt,
            Total = eligibleProducts.Count
        };

        _batchJobQueue.Enqueue(job);
        Console.WriteLine($"Job enqueued with {job.Total} products");

        return RedirectToAction("Progress");
    }

    public IActionResult Progress()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetProgress()
    {
        var job = _batchJobQueue.Peek();

        if (job == null)
            return Json(new { completed = 0, total = 0, done = false });

        return Json(new { completed = job.Completed, total = job.Total, done = job.IsDone });
    }
}