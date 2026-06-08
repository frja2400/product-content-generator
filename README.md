# Product Content Generator
 
Webbaserad applikation som genererar SEO-optimerade produktbeskrivningar med hjälp av Claude AI, baserat på leverantörernas produktdata. Byggd som ett examensarbetsprojekt i samarbete med MEDS Apotek.
 
*Applikationen är byggd för MEDS Apoteks och använder deras Claude API-integration via Azure AI Foundry. En publik demo finns inte tillgänglig, men en videodemo visas i min portfolio.*
 
## Installation
 
För att installera och köra lokalt:
 
```bash
git clone https://github.com/frja2400/product-content-generator.git
cd product-content-generator
dotnet restore
```
 
Skapa en `appsettings.Development.json` i rotkatalogen och lägg till dina API-uppgifter:
 
```json
{
  "Claude": {
    "ApiKey": "din-nyckel-här",
    "Endpoint": "din-endpoint-här",
    "DeploymentName": "claude-sonnet-..."
  }
}
```
 
Starta utvecklingsservern:
 
```bash
dotnet run
```
 
## Funktioner
 
Applikationen är uppdelad i ett stegvist arbetsflöde med fyra steg.
 
**Steg 1 – Ladda upp**
Import av produktdata via drag and drop eller filväljare. Stöder MEDS interna xlsx-mall och Google Shopping XML RSS-flöden. En gemensam datamodell används oavsett källformat.
 
**Steg 2 – Konfigurera**
Produkterna klassificeras automatiskt i tre kvalitetsnivåer (fullständig, begränsad, otillräcklig) baserat på tillgängliga beskrivningsfält. Filtrering på varumärke och kategori, urval via checkboxar och justerbar AI-prompt innan körning. Sample-körning på valfritt antal produkter för förhandsgranskning innan hela batchen körs.
 
**Steg 3 – Granska**
Genererade beskrivningar visas tillsammans med originaldata. Produkter med begränsad data flaggas med varningstext. Möjlighet att justera prompten och köra om, med föregående generering sparad för jämförelse. Enskilda produkter kan regenereras vid misslyckad generering.
 
**Steg 4 – Exportera**
Samlad översikt med klassificering per produkt. Produkter med otillräcklig data listas separat för manuell hantering. Exporterar en xlsx-fil som matchar MEDS interna mall, med AI-genererade beskrivningar i `LongDescription`-kolumnen och originalbeskrivningarna bevarade i ett separat blad.
 
## Projektstruktur
 
```
Controllers/
├── UploadController.cs       # Steg 1: filuppladdning, validering, PRG-mönster
├── ConfigureController.cs    # Steg 2: urval, prompt, sample- och batch-körning
├── ReviewController.cs       # Steg 3: granskning, retry, iterationsjämförelse
└── ExportController.cs       # Steg 4: exportvy och xlsx-nedladdning
 
Services/
├── ImportService.cs          # Parser för XML och xlsx till gemensam datamodell
├── ClassificationService.cs  # Klassificerar produkter i Full/Limited/Insufficient
├── ClaudeService.cs          # Claude API-integration med retry-logik för rate limiting
├── BatchJobService.cs        # BackgroundService för asynkron batch-körning
├── BatchJobQueue.cs          # In-memory jobbkö med progress-tracking
└── ExportService.cs          # Bygger xlsx-exportfil med EPPlus
 
Data/
└── SessionStore.cs           # Server-side sessionshantering (JSON-serialisering)
 
Models/
├── Product.cs                # Huvudmodell med GeneratedDescription och DataQuality
├── ProductClassification.cs  # Enum: Full | Limited | Insufficient
└── GenerationResult.cs       # Resultatobjekt från API-anrop
```
 
## Tech stack
 
| | |
|---|---|
| **Backend** | ASP.NET Core MVC, C# |
| **AI** | Claude API via Azure AI Foundry |
| **Excel-hantering** | EPPlus |
| **Frontend** | Bootstrap, CSS, vanilla JavaScript |
| **Deployment** | DigitalOcean VPS, nginx, systemd, Certbot |
| **CI/CD** | GitHub Actions |
 
## Tekniska lösningar
 
### Claude API-integration
 
API:et nås via Azure AI Foundry med direkta HTTP-anrop. Prompten placeras i `system`-meddelandet och produktdatan skickas separat i `user`-meddelandet, vilket ger mer konsekvent efterlevnad av instruktioner som ordgränser och formatering.
 
```csharp
var requestBody = new
{
    model = _deploymentName,
    max_tokens = MaxTokens,
    system = prompt,
    messages = new[]
    {
        new { role = "user", content = $"Product data:\n{productContext}" }
    }
};
```
 
### Automatisk retry vid rate limiting
 
Vid HTTP 429 läses `Retry-After`-headern av och applikationen väntar exakt den angivna tiden innan den försöker igen, utan att avbryta pågående batch-körning.
 
```csharp
if ((int)response.StatusCode == 429)
{
    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(1);
    status?.Report($"API-gränsen nådd – återupptar om {(int)retryAfter.TotalMinutes} min {retryAfter.Seconds} sek...");
    await Task.Delay(retryAfter, cancellationToken);
    status?.Report("");
    continue;
}
```
 
### Asynkron batch-körning
 
`BatchJobService` är en `BackgroundService` som kör batch-jobbet oberoende av HTTP-kontexten, vilket förhindrar session-timeout vid långa körningar. Progress exponeras via en polling-endpoint och en lokal nedräkning i webbläsaren uppdateras sekund för sekund utan extra serveranrop.
 
## Publicering
 
Applikationen körs på en VPS hos DigitalOcean med nginx som reverse proxy och HTTPS via Certbot. GitHub Actions bygger och deployar automatiskt vid push till `main`.
 
## Om projektet
 
Examensarbete inom Webbutveckling (15 hp) vid Mittuniversitetet, VT 2026, utvecklat i samarbete med MEDS Apotek.
 
**Författare:** Frida Jansson