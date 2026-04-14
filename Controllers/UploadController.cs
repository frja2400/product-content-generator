using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

// Tillfällig controller för att testa uppladdning av filer och import av produkter.
public class UploadController : Controller
{
    private readonly ImportService _importService;
    private readonly ClassificationService _classificationService;

    public UploadController(ImportService importService, ClassificationService classificationService)
    {
        _importService = importService;
        _classificationService = classificationService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return View("Index");

        var extension = Path.GetExtension(file.FileName).ToLower();
        List<ProductContentGenerator.Models.Product> products;

        using var stream = file.OpenReadStream();

        if (extension == ".xlsx")
            products = _importService.ImportFromXlsx(stream);
        else if (extension == ".xml")
            products = _importService.ImportFromXml(stream);
        else
            return View("Index");

        // Klassificera produkterna
        _classificationService.ClassifyProducts(products);

        // Räkna per kvalitetsnivå
        ViewBag.Count = products.Count;
        ViewBag.FullCount = products.Count(p => p.DataQuality == ProductContentGenerator.Models.DataQuality.Full);
        ViewBag.LimitedCount = products.Count(p => p.DataQuality == ProductContentGenerator.Models.DataQuality.Limited);
        ViewBag.InsufficientCount = products.Count(p => p.DataQuality == ProductContentGenerator.Models.DataQuality.Insufficient);
        ViewBag.Products = products.Take(10).ToList();

        return View("Index");
    }
}