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
        // Otillräcklig – saknar grundläggande identifieringsinformation
        if (string.IsNullOrWhiteSpace(product.DisplayName) &&
            string.IsNullOrWhiteSpace(product.Brand))
        {
            return DataQuality.Insufficient;
        }

        // Kontrollera om minst ett beskrivningsfält är ifyllt
        bool hasDescriptionData =
            !string.IsNullOrWhiteSpace(product.ContentDescription) ||
            !string.IsNullOrWhiteSpace(product.UsageDescription) ||
            !string.IsNullOrWhiteSpace(product.FeatureBullets) ||
            !string.IsNullOrWhiteSpace(product.AffectingSubstances) ||
            !string.IsNullOrWhiteSpace(product.LongDescription);

        // Begränsad – har grundinfo men saknar beskrivningsdata
        if (!hasDescriptionData)
        {
            return DataQuality.Limited;
        }

        // Fullständig – har både grundinfo och beskrivningsdata
        return DataQuality.Full;
    }
}