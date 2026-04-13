using OfficeOpenXml;
using System.Xml.Linq;
using ProductContentGenerator.Models;

namespace ProductContentGenerator.Services;

// Service för att importera produktdata från xlsx och XML (Google Shopping-feed)
public class ImportService
{
    // Importerar produktdata från en xlsx-fil
    public List<Product> ImportFromXlsx(Stream fileStream)
    {
        ExcelPackage.License.SetNonCommercialPersonal("ProductContentGenerator");
        var products = new List<Product>();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets[0];

        if (worksheet == null) return products;

        // Läs kolumnrubriker från rad 1
        var headers = new Dictionary<string, int>();
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        // Läs produkter från rad 2 och nedåt
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string Cell(string name) =>
                headers.TryGetValue(name, out int col)
                    ? worksheet.Cells[row, col].Text?.Trim() ?? ""
                    : "";

            var product = new Product
            {
                // Identifiering
                GroupId = Cell("GroupId"),
                VariantId = Cell("VariantId"),
                EAN = Cell("Identifiers_EAN"),
                SearchProductId = Cell("SearchProductId"),
                UrlName = Cell("UrlName"),
                SetGroupNameAndUrlFromVariant = Cell("SetGroupNameAndUrlFromVariant"),

                // Grundinfo
                DisplayName = Cell("DisplayName"),
                Brand = Cell("Brand"),
                Inventory = Cell("Inventory"),
                IsPublished = Cell("IsPublished"),
                ExcludeFromCampaign = Cell("ExcludeFromCampaign"),
                ExcludeFromDiscount = Cell("ExcludeFromDiscount"),
                ExtraSearchWords = Cell("ExtraSearchWords"),

                // Beskrivningar
                ShortDescription = Cell("ShortDescription"),
                LongDescription = Cell("LongDescription"),
                GroupLongDescription = Cell("GroupLongDescription"),
                ContentDescription = Cell("A_ContentDescription"),
                UsageDescription = Cell("A_UsageDescription"),
                FeatureBullets = Cell("A_FeatureBullets"),
                AffectingSubstances = Cell("A_AffectingSubstances"),

                // Kategorisering
                CategorySEO = Cell("Category_SEO"),
                Category0 = Cell("Category_0"),
                Category1 = Cell("Category_1"),
                Category2 = Cell("Category_2"),
                Category3 = Cell("Category_3"),
                Category4 = Cell("Category_4"),
                Category5 = Cell("Category_5"),
                Category6 = Cell("Category_6"),
                Category7 = Cell("Category_7"),
                Category8 = Cell("Category_8"),
                Category9 = Cell("Category_9"),
                ProductClass = Cell("A_ProductClass"),

                // SEO
                SeoGroupTitle = Cell("SEO_Group_Title"),
                SeoGroupDescription = Cell("SEO_Group_Description"),
                SeoGroupCanonicalUrl = Cell("SEO_Group_CanonicalUrl"),
                SeoGroupIndex = Cell("SEO_Group_Index"),
                SeoVariantTitle = Cell("SEO_Variant_Title"),
                SeoVariantDescription = Cell("SEO_Variant_Description"),
                SeoVariantCanonicalUrl = Cell("SEO_Variant_CanonicalUrl"),
                SeoVariantIndex = Cell("SEO_Variant_Index"),

                // Varianter
                Variant1 = Cell("Variant 1"),
                Variant2 = Cell("Variant 2"),
                VariantStorlek = Cell("A_VariantStorlek"),
                VariantSort = Cell("A_VariantSort"),
                VariantDoft = Cell("A_VariantDoft"),
                VariantSmak = Cell("A_VariantSmak"),
                ColorHexCode = Cell("A_ColorHexCode"),

                // Attribut – produkt
                Strength = Cell("A_Strength"),
                Form = Cell("A_Form"),
                PackSize = Cell("A_PackSize"),
                Solskyddsfaktor = Cell("A_Solskyddsfaktor"),
                Ursprungsland = Cell("A_Ursprungsland"),
                Egenskaper = Cell("A_Egenskaper"),
                Beredningsform = Cell("A_Beredningsform"),
                ItemGroup = Cell("A_ItemGroup"),
                IsExclusiveToBrand = Cell("A_IsExclusiveToBrand"),
                Alder = Cell("A_Alder"),
                BarnVuxen = Cell("A_BarnVuxen"),
                Hudtyp = Cell("A_Hudtyp"),
                Hartyp = Cell("A_Hartyp"),
                Farg = Cell("A_Farg"),
                EstimatedLifeTimeDays = Cell("A_EstimatedLifeTimeDays"),

                // Attribut – djur
                Fodertyp = Cell("A_Fodertyp"),
                DjurAlder = Cell("A_DjurÅlder"),
                Hundras = Cell("A_Hundras"),
                Kattras = Cell("A_Kattras"),
                DjurStorlek = Cell("A_DjurStorlek"),
                DjurKostbehov = Cell("A_DjurKostbehov"),
                Djurslag = Cell("A_Djurslag"),

                // Feeds
                FeedAllowedFacebook = Cell("A_FeedAllowed_Facebook"),
                FeedAllowedGoogle = Cell("A_FeedAllowed_Google"),
                FeedAllowedRemarketing = Cell("A_FeedAllowed_Remarketing"),
                FeedAllowedAffiliate = Cell("A_FeedAllowed_Affiliate"),

                // Internt
                ImportSource = "xlsx"
            };

            // Hoppa över helt tomma rader
            if (string.IsNullOrEmpty(product.VariantId) && string.IsNullOrEmpty(product.DisplayName))
                continue;

            products.Add(product);
        }

        return products;
    }

    // Importerar produktdata från en XML-fil (Google Shopping-feed)
    public List<Product> ImportFromXml(Stream fileStream)
    {
        var products = new List<Product>();

        // Läs filen som text och fixa ogiltiga & innan XML-parsning
        using var reader = new StreamReader(fileStream);
        var xmlContent = reader.ReadToEnd();

        // Ersätt & som inte redan är encodade
        xmlContent = System.Text.RegularExpressions.Regex.Replace(
            xmlContent,
            @"&(?!amp;|lt;|gt;|quot;|apos;|#)",
            "&amp;"
        );

        var doc = XDocument.Parse(xmlContent);
        XNamespace g = "http://base.google.com/ns/1.0";

        var items = doc.Descendants("item");

        foreach (var item in items)
        {
            var product = new Product
            {
                VariantId = item.Element(g + "id")?.Value?.Trim(),
                DisplayName = item.Element(g + "title")?.Value?.Trim(),
                LongDescription = item.Element(g + "description")?.Value?.Trim(),
                Brand = item.Element(g + "brand")?.Value?.Trim(),
                EAN = item.Element(g + "gtin")?.Value?.Trim(),
                Farg = item.Element(g + "color")?.Value?.Trim(),
                UrlName = item.Element(g + "link")?.Value?.Trim(),

                // Internt
                ImportSource = "xml"
            };

            // Mappa product_type till Category0–Category9
            MapProductType(product, item.Element(g + "product_type")?.Value);

            // Hoppa över helt tomma rader
            if (string.IsNullOrEmpty(product.VariantId) && string.IsNullOrEmpty(product.DisplayName))
                continue;

            products.Add(product);
        }

        return products;
    }

    // Delar upp product_type-strängen (ex: "Hud > Deodorant > Veganskt") i Category0–Category9
    private void MapProductType(Product product, string? productType)
    {
        if (string.IsNullOrEmpty(productType)) return;

        var segments = productType.Split('>');
        var categoryProperties = new[]
        {
            nameof(Product.Category0), nameof(Product.Category1), nameof(Product.Category2),
            nameof(Product.Category3), nameof(Product.Category4), nameof(Product.Category5),
            nameof(Product.Category6), nameof(Product.Category7), nameof(Product.Category8),
            nameof(Product.Category9)
        };

        for (int i = 0; i < segments.Length && i < categoryProperties.Length; i++)
        {
            typeof(Product).GetProperty(categoryProperties[i])
                ?.SetValue(product, segments[i].Trim());
        }
    }
}