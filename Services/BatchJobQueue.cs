using ProductContentGenerator.Models;

namespace ProductContentGenerator.Services;

// Håller koll på batch-jobb som ska köras
public class BatchJobQueue
{
    private BatchJob? _currentJob;
    private readonly object _lock = new();

    public void Enqueue(BatchJob job)
    {
        lock (_lock)
        {
            _currentJob = job;
        }
    }

    // Hämtar jobbet utan att ta bort det
    public BatchJob? Peek()
    {
        lock (_lock)
        {
            return _currentJob;
        }
    }

    // Hämtar jobbet för bearbetning utan att ta bort det från kön
    public BatchJob? DequeueForProcessing()
    {
        lock (_lock)
        {
            return _currentJob;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _currentJob = null;
        }
    }
}

public class BatchJob
{
    public List<Product> Products { get; set; } = new();
    public List<Product> AllProducts { get; set; } = new();
    public string Prompt { get; set; } = "";
    public int Completed { get; set; }
    public int Total { get; set; }
    public bool IsRunning { get; set; }
    public bool IsDone { get; set; }
    public bool ResultSaved { get; set; }
}