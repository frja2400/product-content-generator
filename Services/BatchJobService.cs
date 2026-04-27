namespace ProductContentGenerator.Services;

// Bakgrundstjänst som kör batch-jobb
public class BatchJobService : BackgroundService
{
    private readonly BatchJobQueue _queue;
    private readonly ClaudeService _claudeService;
    private readonly IServiceScopeFactory _scopeFactory;

    public BatchJobService(BatchJobQueue queue, ClaudeService claudeService, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _claudeService = claudeService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = _queue.DequeueForProcessing();

            if (job != null && !job.IsRunning && !job.IsDone)
            {
                Console.WriteLine($"Starting batch job with {job.Total} products");
                job.IsRunning = true;

                for (int i = 0; i < job.Products.Count; i++)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var product = job.Products[i];
                    var result = await _claudeService.GenerateDescriptionAsync(product, job.Prompt);

                    var productInAll = job.AllProducts.FirstOrDefault(p => p.VariantId == product.VariantId);
                    if (productInAll != null)
                    {
                        productInAll.GeneratedDescription = result.Success
                            ? result.GeneratedDescription
                            : product.LongDescription;
                        productInAll.GenerationFailed = !result.Success;
                    }

                    job.Completed = i + 1;
                    Console.WriteLine($"Completed {job.Completed} of {job.Total}");
                }

                job.IsRunning = false;
                job.IsDone = true;

                using var scope = _scopeFactory.CreateScope();
                var sessionStore = scope.ServiceProvider.GetRequiredService<ProductContentGenerator.Data.SessionStore>();
                sessionStore.SaveProducts(job.AllProducts);

                Console.WriteLine("Batch job done!");
            }

            await Task.Delay(500, stoppingToken);
        }
    }
}