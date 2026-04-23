namespace ProductContentGenerator.Models;

public class Product
{
    // Identifiering
    public string? GroupId { get; set; }
    public string? VariantId { get; set; }
    public string? EAN { get; set; }
    public string? SearchProductId { get; set; }
    public string? UrlName { get; set; }
    public string? SetGroupNameAndUrlFromVariant { get; set; }

    // Grundinfo
    public string? DisplayName { get; set; }
    public string? Brand { get; set; }
    public string? IsPublished { get; set; }
    public string? ExcludeFromCampaign { get; set; }
    public string? ExcludeFromDiscount { get; set; }
    public string? ExtraSearchWords { get; set; }
    public string? Inventory { get; set; }

    // Beskrivningar
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? GroupLongDescription { get; set; }
    public string? ContentDescription { get; set; }
    public string? UsageDescription { get; set; }
    public string? FeatureBullets { get; set; }
    public string? AffectingSubstances { get; set; }

    // Kategorisering
    public string? CategorySEO { get; set; }
    public string? Category0 { get; set; }
    public string? Category1 { get; set; }
    public string? Category2 { get; set; }
    public string? Category3 { get; set; }
    public string? Category4 { get; set; }
    public string? Category5 { get; set; }
    public string? Category6 { get; set; }
    public string? Category7 { get; set; }
    public string? Category8 { get; set; }
    public string? Category9 { get; set; }
    public string? ProductClass { get; set; }

    // SEO
    public string? SeoGroupTitle { get; set; }
    public string? SeoGroupDescription { get; set; }
    public string? SeoGroupCanonicalUrl { get; set; }
    public string? SeoGroupIndex { get; set; }
    public string? SeoVariantTitle { get; set; }
    public string? SeoVariantDescription { get; set; }
    public string? SeoVariantCanonicalUrl { get; set; }
    public string? SeoVariantIndex { get; set; }

    // Varianter
    public string? Variant1 { get; set; }
    public string? Variant2 { get; set; }
    public string? VariantStorlek { get; set; }
    public string? VariantSort { get; set; }
    public string? VariantDoft { get; set; }
    public string? VariantSmak { get; set; }
    public string? ColorHexCode { get; set; }

    // Attribut – produkt
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? PackSize { get; set; }
    public string? Solskyddsfaktor { get; set; }
    public string? Ursprungsland { get; set; }
    public string? Egenskaper { get; set; }
    public string? Beredningsform { get; set; }
    public string? ItemGroup { get; set; }
    public string? IsExclusiveToBrand { get; set; }
    public string? Alder { get; set; }
    public string? BarnVuxen { get; set; }
    public string? Hudtyp { get; set; }
    public string? Hartyp { get; set; }
    public string? Farg { get; set; }
    public string? EstimatedLifeTimeDays { get; set; }

    // Attribut – djur
    public string? Fodertyp { get; set; }
    public string? DjurAlder { get; set; }
    public string? Hundras { get; set; }
    public string? Kattras { get; set; }
    public string? DjurStorlek { get; set; }
    public string? DjurKostbehov { get; set; }
    public string? Djurslag { get; set; }

    // Feeds
    public string? FeedAllowedFacebook { get; set; }
    public string? FeedAllowedGoogle { get; set; }
    public string? FeedAllowedRemarketing { get; set; }
    public string? FeedAllowedAffiliate { get; set; }

    // Genererat innehåll
    public string? GeneratedDescription { get; set; }

    // Klassificering (internt, exporteras ej)
    public DataQuality DataQuality { get; set; }
    public string? ImportSource { get; set; }

    // Internt – om genereringen misslyckades och fallback användes
    public bool GenerationFailed { get; set; }
}