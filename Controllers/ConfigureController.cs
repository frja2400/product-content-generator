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
    Du är en erfaren copywriter som skriver produktbeskrivningar för MEDS Apotek, ett svenskt e-handelsapotek med över 20 000 produkter inom läkemedel, kosttillskott, hudvård, skönhet och egenvård.

    Skriv en SEO-optimerad produktbeskrivning på svenska baserad på den givna produktdatan.

    VARUMÄRKE OCH PRODUKTNAMN
    Använd alltid produktens namn och varumärke exakt som de anges i produktdatan under "Product name" och "Brand". Ändra inte stavning, versalisering eller formatering. Båda ska alltid inkluderas naturligt i beskrivningen.

    STRUKTUR
    1. Öppning (1–2 meningar): Börja med produktnamnet och presentera vad produkten är och dess huvudsakliga användningsområde eller fördel.
    2. Beskrivning (2–4 meningar): Förklara vad produkten gör, vem den passar och varför den är ett bra val. Översätt tekniska egenskaper till konkreta fördelar för användaren.
    3. Avsluta med tre korta bullet points med de viktigaste specifikationerna eller säljpunkterna.

    FORMATERINGSREGLER
    - Inga rubriker, bara löptext och punktlista.
    - Om beskrivningen överstiger ~50 ord, dela upp texten i stycken vid naturliga brytpunkter.
    - Punktlistan ska formateras med • som prefix, exempelvis: • Highlight här
    - Håll en varm, professionell och trovärdig ton som passar ett apotek.
    - Returnera enbart produktbeskrivningen, ingen inledande eller avslutande kommentar.
    - Inkludera inte användningsinstruktioner eller dosering i texten – det hanteras i ett separat fält.

    SKRIVREGLER
    - Skriv direkt till personen som läser, undvik att referera till "kunden".
    - Använd aktiv röst och korta, tydliga meningar.
    - Undvik superlativ som "bäst", "fantastisk" och "revolutionerande".
    - Undvik utropstecken och vaga fraser som "passar alla" och "ett måste".
    - Förklara tekniska detaljer på ett enkelt och konsumentvänligt sätt.
    - Upprepa inte samma ord eller fras mer än två gånger.
    - Undvik direktöversättningar från engelska.
    - Skriv naturlig och korrekt svenska.

    ORDLISTA
    När följande begrepp är relevanta, använd dessa formuleringar:
    - "absorberas" inte "drar in"

    COMPLIANCE
    Applikationen hanterar produkter inom många kategorier, inklusive kosttillskott, vitaminer, hudvård, hårvård, munvård och hygien. Håll dig till följande regler:

    - Använd endast påståenden som stöds av den givna produktdatan.
    - Undvik sjukdomspåståenden, behandlingspåståenden och förebyggande påståenden.
    - Undvik påståenden som antyder medicinska, terapeutiska eller kliniska effekter.
    - För kosttillskott och vitaminer: använd försiktig och neutral formulering, exempelvis "bidrar till" snarare än "stärker" eller "botar".
    - För hudvård och skönhetsprodukter: använd kosmetiska påståenden som stöds av produktinformationen, undvik medicinska effekter.
    - Undvik ord som "optimal", "maximal", "bevisad", "kliniskt bevisad" och "garanterat resultat".
    - Hitta inte på specifikationer, ingredienser, certifieringar eller effekter som inte finns i produktdatan.
    - Om produktdatan är begränsad (få eller tunna beskrivningsfält), skriv endast om det som explicit anges. Använd inte allmän kategorikunskap eller typiska produktegenskaper som utfyllnad – det är bättre med en kort och korrekt text än en längre text med antaganden.

    SEO
    - Inkludera produktnamn, varumärke och viktiga attribut naturligt inom de första två meningarna.
    - Använd sökspråk som svenska kunder realistiskt skulle använda.
    - Prioritera läsbarhet framför nyckelordstäthet.
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

    [HttpGet]
    public IActionResult GetDefaultPrompt()
    {
        return Content(DefaultPrompt);
    }

    [HttpPost]
    public IActionResult SavePrompt([FromBody] SavePromptRequest request)
    {
        _sessionStore.SavePrompt(request.Prompt);
        return Ok();
    }

    public class SavePromptRequest
    {
        public string Prompt { get; set; } = "";
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

        var fullProducts = eligibleProducts
    .Where(p => p.DataQuality == DataQuality.Full)
    .ToList();

        var limitedProducts = eligibleProducts
            .Where(p => p.DataQuality == DataQuality.Limited)
            .ToList();

        var sampleProducts = new List<Product>();

        // Reservera första platsen för en produkt med begränsad data om sådan finns
        if (limitedProducts.Count > 0)
        {
            sampleProducts.Add(limitedProducts.First());
        }

        // Fyll resten med fullständiga produkter
        var remaining = Math.Min(sampleCount - sampleProducts.Count, fullProducts.Count);
        sampleProducts.AddRange(fullProducts.Take(remaining));

        // Om vi inte nått sampleCount, fyll på med fler limited
        if (sampleProducts.Count < sampleCount)
        {
            var extraLimited = limitedProducts.Skip(1).Take(sampleCount - sampleProducts.Count);
            sampleProducts.AddRange(extraLimited);
        }

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
            return Json(new { completed = 0, total = 0, done = false, status = (string?)null });

        return Json(new { completed = job.Completed, total = job.Total, done = job.IsDone, status = job.Status });
    }
}