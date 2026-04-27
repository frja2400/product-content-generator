using System.Text.Json;
using ProductContentGenerator.Models;

namespace ProductContentGenerator.Data;

// Hanterar temporär lagring av produkter i sessionen mellan stegen
public class SessionStore
{
    private const string ProductsKey = "products";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Sparar produktlistan i sessionen
    public void SaveProducts(List<Product> products)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var json = JsonSerializer.Serialize(products);
        session?.SetString(ProductsKey, json);
    }

    // Hämtar produktlistan från sessionen
    public List<Product> GetProducts()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var json = session?.GetString(ProductsKey);
        if (string.IsNullOrEmpty(json)) return new List<Product>();
        return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
    }

    // Rensar produktlistan från sessionen
    public void Clear()
    {
        _httpContextAccessor.HttpContext?.Session.Remove(ProductsKey);
    }

    private const string PromptKey = "prompt";

    // Sparar prompten i sessionen
    public void SavePrompt(string prompt)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.SetString(PromptKey, prompt);
    }

    // Hämtar prompten från sessionen
    public string GetPrompt()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        return session?.GetString(PromptKey) ?? string.Empty;
    }

    private const string ProgressKey = "progress";

    public void SaveProgress(int completed, int total)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var progress = new { completed, total };
        session?.SetString(ProgressKey, JsonSerializer.Serialize(progress));
    }

    public (int completed, int total) GetProgress()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var json = session?.GetString(ProgressKey);
        if (string.IsNullOrEmpty(json)) return (0, 0);

        var progress = JsonSerializer.Deserialize<JsonElement>(json);
        return (progress.GetProperty("completed").GetInt32(),
                progress.GetProperty("total").GetInt32());
    }

    public void ClearProgress()
    {
        _httpContextAccessor.HttpContext?.Session.Remove(ProgressKey);
    }

    private const string SelectedKey = "selected";

    // Sparar valda produkter i sessionen
    public void SaveSelectedProducts(List<string> variantIds)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.SetString(SelectedKey, JsonSerializer.Serialize(variantIds));
    }

    // Hämtar valda produkter från sessionen
    public List<string> GetSelectedProducts()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var json = session?.GetString(SelectedKey);
        if (string.IsNullOrEmpty(json)) return new List<string>();
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }
}