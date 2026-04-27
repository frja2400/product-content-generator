using Microsoft.AspNetCore.Mvc;
using ProductContentGenerator.Data;
using ProductContentGenerator.Services;

namespace ProductContentGenerator.Controllers;

public class ExportController : Controller
{
    private readonly SessionStore _sessionStore;
    private readonly BatchJobQueue _batchJobQueue;

    public ExportController(SessionStore sessionStore, BatchJobQueue batchJobQueue)
    {
        _sessionStore = sessionStore;
        _batchJobQueue = batchJobQueue;
    }

    public IActionResult Index()
    {
        var job = _batchJobQueue.Peek();

        if (job != null && job.IsDone)
        {
            // Använd alltid batch-jobbets resultat om det finns
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
}