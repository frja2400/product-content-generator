using ProductContentGenerator.Models;

namespace ProductContentGenerator.Services;

// Service för att klassificera produkter baserat på datakvalitet
public class ClassificationService
{
    public void ClassifyProducts(List<Product> products)
    {
        foreach (var product in products)
        {
            product.DataQuality = Classify(product);
        }
    }

    private DataQuality Classify(Product product)
    {
        // Otillräcklig – saknar produktnamn eller alla beskrivningsfält
        if (string.IsNullOrWhiteSpace(product.DisplayName))
            return DataQuality.Insufficient;

        bool hasRichDescription =
            !string.IsNullOrWhiteSpace(product.LongDescription) ||
            !string.IsNullOrWhiteSpace(product.ContentDescription) ||
            !string.IsNullOrWhiteSpace(product.FeatureBullets) ||
            !string.IsNullOrWhiteSpace(product.AffectingSubstances);

        bool hasThinDescription =
            !string.IsNullOrWhiteSpace(product.ShortDescription) ||
            !string.IsNullOrWhiteSpace(product.UsageDescription);

        // Otillräcklig – har namn men absolut inget beskrivningsinnehåll
        if (!hasRichDescription && !hasThinDescription)
            return DataQuality.Insufficient;

        // Begränsad – har namn men bara tunna beskrivningsfält
        if (!hasRichDescription && hasThinDescription)
            return DataQuality.Limited;

        // Fullständig – har namn och minst ett rikt beskrivningsfält
        return DataQuality.Full;
    }
}