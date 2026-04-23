using System.Text;
using System.Text.Json;
using ProductContentGenerator.Models;

namespace ProductContentGenerator.Services;

// Service för att generera produktbeskrivningar via Claude API
public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _deploymentName;

    private const int MaxDescriptionLength = 500;
    private const int MaxTokens = 800;

    public ClaudeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude API key not configured");
        _endpoint = configuration["Claude:Endpoint"] ?? throw new InvalidOperationException("Claude endpoint not configured");
        _deploymentName = configuration["Claude:DeploymentName"] ?? throw new InvalidOperationException("Claude deployment name not configured");
    }

    public async Task<GenerationResult> GenerateDescriptionAsync(Product product, string prompt)
    {
        // Skippa produkter med otillräcklig data
        if (product.DataQuality == DataQuality.Insufficient)
        {
            return new GenerationResult
            {
                VariantId = product.VariantId,
                Success = false,
                ErrorMessage = "Insufficient data – skipped to save tokens",
                UsedFallback = false
            };
        }

        try
        {
            var productContext = BuildProductContext(product);
            var fullPrompt = $"{prompt}\n\nProduct data:\n{productContext}";

            var requestBody = new
            {
                model = _deploymentName,
                max_tokens = MaxTokens,
                messages = new[]
                {
                    new { role = "user", content = fullPrompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new GenerationResult
                {
                    VariantId = product.VariantId,
                    Success = false,
                    ErrorMessage = $"API error: {response.StatusCode} – {responseBody}",
                    UsedFallback = true,
                    GeneratedDescription = product.LongDescription
                };
            }

            var responseJson = JsonDocument.Parse(responseBody);
            var generatedText = responseJson
                .RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return new GenerationResult
            {
                VariantId = product.VariantId,
                GeneratedDescription = generatedText,
                Success = true,
                UsedFallback = false
            };
        }
        catch (Exception ex)
        {
            // Fallback till originaltext vid fel
            return new GenerationResult
            {
                VariantId = product.VariantId,
                Success = false,
                ErrorMessage = ex.Message,
                UsedFallback = true,
                GeneratedDescription = product.LongDescription
            };
        }
    }

    // Bygger en textrepresentation av produktens data som skickas till Claude
    private string BuildProductContext(Product product)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(product.DisplayName))
            sb.AppendLine($"Product name: {product.DisplayName}");
        if (!string.IsNullOrWhiteSpace(product.Brand))
            sb.AppendLine($"Brand: {product.Brand}");
        if (!string.IsNullOrWhiteSpace(product.LongDescription))
            sb.AppendLine($"Description: {Truncate(product.LongDescription)}");
        if (!string.IsNullOrWhiteSpace(product.ShortDescription))
            sb.AppendLine($"Short description: {Truncate(product.ShortDescription)}");
        if (!string.IsNullOrWhiteSpace(product.ContentDescription))
            sb.AppendLine($"Content: {Truncate(product.ContentDescription)}");
        if (!string.IsNullOrWhiteSpace(product.UsageDescription))
            sb.AppendLine($"Usage: {Truncate(product.UsageDescription)}");
        if (!string.IsNullOrWhiteSpace(product.FeatureBullets))
            sb.AppendLine($"Features: {Truncate(product.FeatureBullets)}");
        if (!string.IsNullOrWhiteSpace(product.AffectingSubstances))
            sb.AppendLine($"Substances: {Truncate(product.AffectingSubstances)}");
        if (!string.IsNullOrWhiteSpace(product.Category0))
            sb.AppendLine($"Category: {product.Category0}");
        if (!string.IsNullOrWhiteSpace(product.Category1))
            sb.AppendLine($"Subcategory: {product.Category1}");

        return sb.ToString();
    }

    // Trunkerar långa texter för att minimera token-användning
    private string Truncate(string text) =>
        text.Length > MaxDescriptionLength
            ? text[..MaxDescriptionLength] + "..."
            : text;
}