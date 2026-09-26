using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Services;

var builder = WebApplication.CreateBuilder(args);

// Render.com asigna el puerto via la variable de entorno PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Algolia: búsqueda de texto completo (Write Key solo en servidor)
builder.Services.AddSingleton<AlgoliaService>();

// PieSocket: WebSockets en tiempo real (Api Secret solo en servidor)
builder.Services.AddSingleton<PieSocketService>();

// Redis: caché distribuida (con fallback en memoria si no hay conexión)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName  = "PlataformaIncidencias:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// En Render el TLS lo maneja el reverse proxy, no la app
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// Seeder: crea BD y datos iniciales (GUIDs fijos para sobrevivir reinicios de Render)
using (var scope = app.Services.CreateScope())
{
    var services    = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var algolia     = services.GetRequiredService<AlgoliaService>();
    await DataSeeder.SeedDataAsync(services, userManager, algolia);
}

app.Run();
