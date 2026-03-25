using Musicly.Components;
using Musicly.Data;
using Musicly.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// PostgreSQL + EF Core
// Render.com provides DATABASE_URL; fallback to appsettings for local dev
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Render provides: postgres://user:pass@host:port/dbname
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString), ServiceLifetime.Scoped);

// Auth
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

// App services
builder.Services.AddScoped<MusicPlayerService>();
builder.Services.AddScoped<ListeningStatsService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<SongRequestService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<TrackUploadService>();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            Console.WriteLine($"Applying {pending.Count()} pending migration(s)...");
            await db.Database.MigrateAsync();
            Console.WriteLine("Migrations applied successfully.");
        }
        else
        {
            Console.WriteLine("Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Database migration check failed: {ex.Message}");
        Console.WriteLine("Attempting EnsureCreated as fallback...");
        try { await db.Database.EnsureCreatedAsync(); }
        catch { /* DB may already exist, continue */ }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
