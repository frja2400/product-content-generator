using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

// Tillfällig controller för att testa uppladdning av filer och import av produkter.
public class UploadController : Controller
{
    private readonly ImportService _importService;

    public UploadController(ImportService importService)
    {
        _importService = importService;
    }

    // Visar uppladdningssidan
    public IActionResult Index()
    {
        return View();
    }

    // Tar emot uppladdad fil och importerar produkter
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

        // Temporärt – visa resultatet direkt i vyn för testning
        ViewBag.Count = products.Count;
        ViewBag.Products = products.Take(5).ToList();

        return View("Index");
    }
}