using ProductContentGenerator.Services;
using ProductContentGenerator.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session-lagring i minnet
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registrera services
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<ClassificationService>();
builder.Services.AddHttpClient<ClaudeService>();
builder.Services.AddScoped<SessionStore>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddSingleton<BatchJobQueue>();
builder.Services.AddHostedService<BatchJobService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Upload}/{action=Index}/{id?}");

app.Run();