using Microsoft.EntityFrameworkCore;
using TableViewer.Data;
using TableViewer.Services;
using TableViewer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ViewService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Инициализация БД
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Миграция из JSON если нужно
        var jsonPath = Path.Combine(app.Environment.ContentRootPath, "queries.json");
        if (File.Exists(jsonPath) && !await context.Views.AnyAsync())
        {
            var jsonData = await File.ReadAllTextAsync(jsonPath);
            var views = System.Text.Json.JsonSerializer.Deserialize<List<ViewConfig>>(jsonData);
            if (views != null)
            {
                await context.Views.AddRangeAsync(views);
                await context.SaveChangesAsync();
            }
        }

        // Проверяем, есть ли настройка "header", если нет - создаем
        if (!await context.Settings.AnyAsync(s => s.Name == "header"))
        {
            context.Settings.Add(new Setting { Name = "header", Value = "TableViewer" });
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();