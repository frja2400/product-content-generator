using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;
using ProductContentGenerator.Models;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

public class ExportController : Controller
{
    private readonly SessionStore _sessionStore;
    private readonly BatchJobQueue _batchJobQueue;
    private readonly ExportService _exportService;

    public ExportController(SessionStore sessionStore, BatchJobQueue batchJobQueue, ExportService exportService)
    {
        _sessionStore = sessionStore;
        _batchJobQueue = batchJobQueue;
        _exportService = exportService;
    }

    public IActionResult Index()
    {
        var job = _batchJobQueue.Peek();

        if (job != null && job.IsDone)
        {
            if (!job.ResultSaved)
            {
                _sessionStore.SaveProducts(job.AllProducts);
                job.ResultSaved = true;
            }

            ViewBag.SelectedVariantIds = _sessionStore.GetSelectedProducts();
            return View(job.AllProducts);
        }

        var products = _sessionStore.GetProducts();

        if (products.Count == 0)
            return RedirectToAction("Index", "Upload");

        ViewBag.SelectedVariantIds = _sessionStore.GetSelectedProducts();
        return View(products);
    }

    [HttpGet]
    public IActionResult Detail(string variantId)
    {
        var products = _sessionStore.GetProducts();
        var product = products.FirstOrDefault(p => p.VariantId == variantId);

        if (product == null)
            return NotFound();

        return PartialView("_ExportDetail", product);
    }

    [HttpGet]
    public IActionResult DownloadExcel()
    {
        var job = _batchJobQueue.Peek();
        List<Product> products;

        if (job != null && job.IsDone)
            products = job.AllProducts;
        else
            products = _sessionStore.GetProducts();

        var selectedVariantIds = _sessionStore.GetSelectedProducts();

        var exportProducts = products
            .Where(p => selectedVariantIds.Contains(p.VariantId ?? "") &&
                        p.DataQuality != ProductContentGenerator.Models.DataQuality.Insufficient)
            .ToList();

        var fileBytes = _exportService.ExportToXlsx(exportProducts);

        var fileName = $"product-content-export-{DateTime.Now:yyyy-MM-dd}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}