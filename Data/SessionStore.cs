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
}