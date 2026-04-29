using OfficeOpenXml;
using System.Text;
using ProductContentGenerator.Models;

namespace ProductContentGenerator.Services;

// Service för att exportera produktdata till xlsx
public class ExportService
{
    public byte[] ExportToXlsx(List<Product> products)
    {
        ExcelPackage.License.SetNonCommercialPersonal("ProductContentGenerator");

        using var package = new ExcelPackage();

        // Blad 1: Produkter
        var worksheet = package.Workbook.Worksheets.Add("Products");

        // Kolumnrubriker – matchar xlsx-mallen från MEDS
        var headers = new[]
        {
        "GroupId", "VariantId", "Identifiers_EAN", "UrlName", "SearchProductId",
        "SetGroupNameAndUrlFromVariant", "Inventory", "SEO_Group_Title", "SEO_Group_Description",
        "SEO_Group_CanonicalUrl", "SEO_Group_Index", "SEO_Variant_Title", "SEO_Variant_Description",
        "SEO_Variant_CanonicalUrl", "SEO_Variant_Index", "Brand", "Category_SEO", "Category_0",
        "Category_1", "Category_2", "Category_3", "Category_4", "Category_5", "Category_6",
        "Category_7", "Category_8", "Category_9", "Variant 1", "Variant 2", "A_ColorHexCode",
        "A_VariantStorlek", "A_VariantSort", "A_VariantDoft", "A_VariantSmak", "A_Egenskaper",
        "A_Fodertyp", "A_DjurÅlder", "A_Hundras", "A_Kattras", "A_DjurStorlek", "A_DjurKostbehov",
        "A_Alder", "A_BarnVuxen", "A_Beredningsform", "A_Djurslag", "A_Farg", "A_Form", "A_Hartyp",
        "A_Hudtyp", "A_IsExclusiveToBrand", "A_ItemGroup", "A_PackSize", "A_Solskyddsfaktor",
        "A_Strength", "A_Ursprungsland", "ExcludeFromCampaign", "DisplayName", "ExcludeFromDiscount",
        "IsPublished", "ExtraSearchWords", "A_FeatureBullets", "LongDescription",
        "A_EstimatedLifeTimeDays", "GroupLongDescription", "A_ContentDescription", "A_UsageDescription",
        "A_FeedAllowed_Facebook", "A_FeedAllowed_Google", "A_FeedAllowed_Remarketing",
        "A_FeedAllowed_Affiliate", "A_ProductClass", "ShortDescription", "A_AffectingSubstances"
    };

        // Skriv rubriker i rad 1
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // Skriv produktdata från rad 2
        for (int row = 0; row < products.Count; row++)
        {
            var p = products[row];
            var values = new object?[]
            {
            p.GroupId, p.VariantId, p.EAN, p.UrlName, p.SearchProductId,
            p.SetGroupNameAndUrlFromVariant, p.Inventory, p.SeoGroupTitle, p.SeoGroupDescription,
            p.SeoGroupCanonicalUrl, p.SeoGroupIndex, p.SeoVariantTitle, p.SeoVariantDescription,
            p.SeoVariantCanonicalUrl, p.SeoVariantIndex, p.Brand, p.CategorySEO, p.Category0,
            p.Category1, p.Category2, p.Category3, p.Category4, p.Category5, p.Category6,
            p.Category7, p.Category8, p.Category9, p.Variant1, p.Variant2, p.ColorHexCode,
            p.VariantStorlek, p.VariantSort, p.VariantDoft, p.VariantSmak, p.Egenskaper,
            p.Fodertyp, p.DjurAlder, p.Hundras, p.Kattras, p.DjurStorlek, p.DjurKostbehov,
            p.Alder, p.BarnVuxen, p.Beredningsform, p.Djurslag, p.Farg, p.Form, p.Hartyp,
            p.Hudtyp, p.IsExclusiveToBrand, p.ItemGroup, p.PackSize, p.Solskyddsfaktor,
            p.Strength, p.Ursprungsland, p.ExcludeFromCampaign, p.DisplayName, p.ExcludeFromDiscount,
            p.IsPublished, p.ExtraSearchWords, p.FeatureBullets,
            // LongDescription ersätts med GeneratedDescription om den finns
            !string.IsNullOrWhiteSpace(p.GeneratedDescription) ? ConvertToHtml(p.GeneratedDescription) : p.LongDescription,
            p.EstimatedLifeTimeDays, p.GroupLongDescription, p.ContentDescription, p.UsageDescription,
            p.FeedAllowedFacebook, p.FeedAllowedGoogle, p.FeedAllowedRemarketing,
            p.FeedAllowedAffiliate, p.ProductClass, p.ShortDescription, p.AffectingSubstances
            };

            for (int col = 0; col < values.Length; col++)
            {
                worksheet.Cells[row + 2, col + 1].Value = values[col];
            }
        }

        // Blad 2: Originaltexter
        var originalSheet = package.Workbook.Worksheets.Add("Original descriptions");

        originalSheet.Cells[1, 1].Value = "VariantId";
        originalSheet.Cells[1, 2].Value = "DisplayName";
        originalSheet.Cells[1, 3].Value = "LongDescription (original)";

        for (int row = 0; row < products.Count; row++)
        {
            var p = products[row];
            originalSheet.Cells[row + 2, 1].Value = p.VariantId;
            originalSheet.Cells[row + 2, 2].Value = p.DisplayName;
            originalSheet.Cells[row + 2, 3].Value = p.LongDescription;
        }

        return package.GetAsByteArray();
    }

    private string ConvertToHtml(string text)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        var inList = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("•") || trimmed.StartsWith("-"))
            {
                if (!inList)
                {
                    sb.Append("<ul>");
                    inList = true;
                }
                sb.Append($"<li>{trimmed.TrimStart('•', '-').Trim()}</li>");
            }
            else
            {
                if (inList)
                {
                    sb.Append("</ul>");
                    inList = false;
                }
                if (!string.IsNullOrWhiteSpace(trimmed))
                    sb.Append($"<p>{trimmed}</p>");
            }
        }

        if (inList)
            sb.Append("</ul>");

        return sb.ToString();
    }
}