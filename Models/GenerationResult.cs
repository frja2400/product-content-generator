namespace ProductContentGenerator.Models;

public class GenerationResult
{
    public string? VariantId { get; set; }
    public string? GeneratedDescription { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool UsedFallback { get; set; }
}