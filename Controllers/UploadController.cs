using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Services;
using ProductContentGenerator.Data;

namespace ProductContentGenerator.Controllers;

public class UploadController : Controller
{
    private readonly ImportService _importService;
    private readonly ClassificationService _classificationService;
    private readonly SessionStore _sessionStore;

    public UploadController(ImportService importService, ClassificationService classificationService, SessionStore sessionStore)
    {
        _importService = importService;
        _classificationService = classificationService;
        _sessionStore = sessionStore;
    }

    public IActionResult Index()
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        return View();
    }

    [HttpPost]
    public IActionResult Upload(IFormFile file)
    {
        // Ingen fil vald
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction("Index");
        }

        // Fel filformat
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (extension != ".xlsx" && extension != ".xml")
        {
            TempData["Error"] = "Invalid file format. Please upload an XML or XLSX file.";
            return RedirectToAction("Index");
        }

        try
        {
            List<ProductContentGenerator.Models.Product> products;

            using var stream = file.OpenReadStream();

            if (extension == ".xlsx")
                products = _importService.ImportFromXlsx(stream);
            else
                products = _importService.ImportFromXml(stream);

            // Inga produkter hittades
            if (products.Count == 0)
            {
                TempData["Error"] = "No products found in the file. Please check the file and try again.";
                return RedirectToAction("Index");
            }

            _classificationService.ClassifyProducts(products);
            _sessionStore.SaveProducts(products);

            return RedirectToAction("Index", "Configure");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Something went wrong while processing the file. Please check the file and try again.";
            Console.WriteLine($"Upload error: {ex.Message}");
            return RedirectToAction("Index");
        }
    }

}
